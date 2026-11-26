#!/usr/bin/env node
import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';
import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';
import dotenv from 'dotenv';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Resolve configuration beside the MCP server, not from the client's working directory.
const serverDirectory = path.dirname(fileURLToPath(import.meta.url));
dotenv.config({ path: path.resolve(serverDirectory, '..', '.env') });

// Timeouts (ms): health check is cheap, editor commands may take longer
const HEALTH_TIMEOUT_MS = 2000;
const COMMAND_TIMEOUT_MS = 30000;
const ANIMATION_TIMEOUT_MS = 120000;
// How long to keep retrying while Unity reloads scripts and the bridge restarts
const RETRY_WINDOW_MS = 45000;
const RETRY_DELAY_MS = 1000;

// Unity Bridge client
class UnityBridgeClient {
  private baseUrl: string;
  private http: AxiosInstance;

  constructor() {
    const host = process.env.UNITY_EDITOR_HOST || 'localhost';
    const port = process.env.UNITY_EDITOR_PORT || '7777';
    this.baseUrl = `http://${host}:${port}`;
    this.http = axios.create({
      baseURL: this.baseUrl,
      timeout: COMMAND_TIMEOUT_MS,
      validateStatus: null,
      headers: {
        'Content-Type': 'application/json; charset=utf-8',
        'Accept': 'application/json',
      },
    });
  }

  async health(): Promise<any | null> {
    try {
      const response = await this.http.get(`/health`, { timeout: HEALTH_TIMEOUT_MS });
      return response.status === 200 ? response.data : null;
    } catch {
      return null;
    }
  }

  /**
   * Sends a request and retries while the bridge is restarting.
   *
   * Unity stops the bridge for a few seconds on every script reload (compilation, entering
   * Play Mode). Two cases are safe to retry because the command is known not to have run:
   * the connection was refused, or the bridge answered 503 (it cancels the queued command).
   * A connection dropped mid-request is not retried: the command may already have executed.
   */
  private async request(config: AxiosRequestConfig): Promise<UnityResponse> {
    const deadline = Date.now() + RETRY_WINDOW_MS;

    for (;;) {
      try {
        const response = await this.http.request(config);
        if (response.status === 503 && Date.now() < deadline) {
          await delay(RETRY_DELAY_MS);
          continue;
        }
        return { status: response.status, data: response.data };
      } catch (error) {
        const code = axios.isAxiosError(error) ? error.code : undefined;
        if (code === 'ECONNREFUSED' && Date.now() < deadline) {
          await delay(RETRY_DELAY_MS);
          continue;
        }
        if (code === 'ECONNREFUSED') {
          throw new Error(
            `Unity MCP Bridge is not reachable at ${this.baseUrl}. ` +
            'Open the project in Unity Editor and run Window -> MCP Bridge -> Start Server.'
          );
        }
        if (code === 'ECONNRESET' || code === 'ECONNABORTED') {
          throw new Error(
            `Connection to Unity MCP Bridge was lost during the request (${code}). ` +
            'Unity may be reloading scripts; the command may or may not have run. Check the editor state before retrying.'
          );
        }
        throw error;
      }
    }
  }

  executeCommand(command: string, args?: Record<string, any>): Promise<UnityResponse> {
    return this.request({ method: 'post', url: `/execute`, data: { command, args: args || {} } });
  }

  getSceneInfo(): Promise<UnityResponse> {
    return this.request({ method: 'get', url: `/scene` });
  }

  importAsset(path: string, options?: Record<string, any>): Promise<UnityResponse> {
    return this.request({ method: 'post', url: `/import`, data: { path, options: options || {} } });
  }

  retargetAnimation(options: {
    sourceAnimationPath: string;
    sourceSkeletonType: string;
    targetSkeletonType: string;
    outputPath: string;
  }): Promise<UnityResponse> {
    return this.request({
      method: 'post',
      url: `/animation/retarget`,
      data: options,
      timeout: ANIMATION_TIMEOUT_MS,
    });
  }
}

interface UnityResponse {
  status: number;
  data: any;
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Formats a bridge response as an MCP tool result; HTTP errors are reported as tool errors.
function toolResult(response: UnityResponse) {
  const failed = response.status >= 400 || response.data?.success === false;
  return {
    content: [
      {
        type: 'text' as const,
        text: JSON.stringify(response.data, null, 2),
      },
    ],
    ...(failed ? { isError: true } : {}),
  };
}

const unityClient = new UnityBridgeClient();

// MCP Server setup
const server = new Server(
  {
    name: 'unity-mcp-server',
    version: '0.1.0',
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Tool definitions
const tools = [
  {
    name: 'unity_check_connection',
    description: 'Check if Unity Editor is running and MCP Bridge is active',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'unity_execute_command',
    description: 'Execute a command in Unity Editor via Editor API',
    inputSchema: {
      type: 'object',
      properties: {
        command: {
          type: 'string',
          description: 'Unity command to execute (e.g., "CreateGameObject", "DebugLog", "SaveScene")',
        },
        args: {
          type: 'object',
          description: 'Command arguments as key-value pairs',
        },
      },
      required: ['command'],
    },
  },
  {
    name: 'unity_get_scene_info',
    description: 'Get information about the currently opened Unity scene',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
  {
    name: 'unity_debug_log',
    description: 'Write a one-off message to the Unity Console via Debug.Log, Debug.LogWarning, or Debug.LogError. Use this for requests like "выведи в Console/Log"; do not create scripts, GameObjects, prefabs, or assets for one-time log messages.',
    inputSchema: {
      type: 'object',
      properties: {
        message: {
          type: 'string',
          description: 'Message to write to the Unity Console',
        },
        level: {
          type: 'string',
          description: 'Log level: "log", "warning", or "error". Defaults to "log".',
          enum: ['log', 'warning', 'error'],
          default: 'log',
        },
      },
      required: ['message'],
    },
  },
  {
    name: 'unity_create_gameobject',
    description: 'Create a new GameObject in the Unity scene',
    inputSchema: {
      type: 'object',
      properties: {
        name: {
          type: 'string',
          description: 'Name of the GameObject',
        },
        type: {
          type: 'string',
          description: 'Type of GameObject (e.g., "Empty", "Cube", "Sphere")',
          default: 'Empty',
        },
        parent: {
          type: 'string',
          description: 'Parent GameObject path (optional)',
        },
      },
      required: ['name'],
    },
  },
  {
    name: 'unity_import_asset',
    description: 'Import an asset into Unity project',
    inputSchema: {
      type: 'object',
      properties: {
        path: {
          type: 'string',
          description: 'Path to the asset file (absolute or relative to project)',
        },
        destination: {
          type: 'string',
          description: 'Destination path in Assets folder',
        },
      },
      required: ['path'],
    },
  },
  {
    name: 'unity_animation_retarget',
    description: 'Retarget animation from one skeleton type to another using AI-powered bone mapping',
    inputSchema: {
      type: 'object',
      properties: {
        sourceAnimationPath: {
          type: 'string',
          description: 'Path to source animation clip in Assets',
        },
        sourceSkeletonType: {
          type: 'string',
          description: 'Source skeleton type (e.g., "Mixamo", "UE4", "Custom")',
        },
        targetSkeletonType: {
          type: 'string',
          description: 'Target skeleton type',
        },
        outputPath: {
          type: 'string',
          description: 'Output path for retargeted animation',
        },
      },
      required: ['sourceAnimationPath', 'sourceSkeletonType', 'targetSkeletonType', 'outputPath'],
    },
  },
  {
    name: 'unity_get_project_structure',
    description: 'Get the structure of Assets folder and key project directories',
    inputSchema: {
      type: 'object',
      properties: {
        depth: {
          type: 'number',
          description: 'Maximum depth to traverse (default: 2)',
          default: 2,
        },
      },
    },
  },
  {
    name: 'unity_console_logs',
    description: 'Get recent Unity Console entries, newest first, with their level (error/warning/info), source file and line. The result also reports total error/warning/info counts for the whole console.',
    inputSchema: {
      type: 'object',
      properties: {
        count: {
          type: 'number',
          description: 'Number of recent log entries to retrieve (default: 50)',
          default: 50,
        },
        filter: {
          type: 'string',
          description: 'Filter logs by type: "all", "error", "warning", "info" (default: "all")',
          default: 'all',
        },
        stackTrace: {
          type: 'boolean',
          description: 'Include the stack trace of each entry (default: false)',
          default: false,
        },
      },
    },
  },
  {
    name: 'unity_play_mode',
    description: 'Enter or exit Play Mode in Unity Editor, or check current status',
    inputSchema: {
      type: 'object',
      properties: {
        action: {
          type: 'string',
          description: 'Action to perform: "enter", "exit" or "status"',
          enum: ['enter', 'exit', 'status'],
        },
      },
      required: ['action'],
    },
  },
  {
    name: 'unity_compilation_check',
    description: 'Check Unity script compilation status. Returns whether the last compilation failed and lists the compile errors currently in the console.',
    inputSchema: {
      type: 'object',
      properties: {},
    },
  },
];

// List tools handler
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return { tools };
});

// Call tool handler
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      case 'unity_check_connection': {
        const health = await unityClient.health();
        const connected = health !== null;
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                connected,
                status: !connected
                  ? 'Unity Editor not responding'
                  : health.mainThreadResponsive === false
                    ? 'Unity Editor MCP Bridge is active, but the editor is busy (compiling, importing or unfocused)'
                    : 'Unity Editor MCP Bridge is active',
                bridge_url: process.env.UNITY_EDITOR_HOST || 'localhost',
                port: process.env.UNITY_EDITOR_PORT || '7777',
                editor: health,
              }, null, 2),
            },
          ],
        };
      }

      case 'unity_execute_command': {
        const { command, args: cmdArgs } = args as { command: string; args?: Record<string, any> };
        const result = await unityClient.executeCommand(command, cmdArgs);
        return toolResult(result);
      }

      case 'unity_get_scene_info': {
        const sceneInfo = await unityClient.getSceneInfo();
        return toolResult(sceneInfo);
      }

      case 'unity_debug_log': {
        const { message, level } = args as { message: string; level?: 'log' | 'warning' | 'error' };
        const result = await unityClient.executeCommand('DebugLog', {
          message,
          level: level || 'log',
        });
        return toolResult(result);
      }

      case 'unity_create_gameobject': {
        const { name: goName, type, parent } = args as { name: string; type?: string; parent?: string };
        const result = await unityClient.executeCommand('CreateGameObject', {
          name: goName,
          type: type || 'Empty',
          parent,
        });
        return toolResult(result);
      }

      case 'unity_import_asset': {
        const { path, destination } = args as { path: string; destination?: string };
        const result = await unityClient.importAsset(path, { destination });
        return toolResult(result);
      }

      case 'unity_animation_retarget': {
        const retargetArgs = args as {
          sourceAnimationPath: string;
          sourceSkeletonType: string;
          targetSkeletonType: string;
          outputPath: string;
        };
        const result = await unityClient.retargetAnimation(retargetArgs);
        return toolResult(result);
      }

      case 'unity_get_project_structure': {
        const { depth } = args as { depth?: number };
        const result = await unityClient.executeCommand('GetProjectStructure', { depth: depth || 2 });
        return toolResult(result);
      }

      case 'unity_console_logs': {
        const { count, filter, stackTrace } = args as { count?: number; filter?: string; stackTrace?: boolean };
        const result = await unityClient.executeCommand('GetConsoleLogs', {
          count: count || 50,
          filter: filter || 'all',
          stackTrace: stackTrace === true,
        });
        return toolResult(result);
      }

      case 'unity_play_mode': {
        const { action } = args as { action: 'enter' | 'exit' | 'status' };
        const result = await unityClient.executeCommand(
          action === 'enter' ? 'EnterPlayMode'
          : action === 'exit' ? 'ExitPlayMode'
          : 'IsPlayMode'
        );
        return toolResult(result);
      }

      case 'unity_compilation_check': {
        const result = await unityClient.executeCommand('GetCompilationErrors');
        return toolResult(result);
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify({
            error: errorMessage,
            tool: name,
            timestamp: new Date().toISOString(),
          }, null, 2),
        },
      ],
      isError: true,
    };
  }
});

// Start server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);

  console.error('Unity MCP Server running on stdio');
  console.error(`Unity Editor Bridge: ${process.env.UNITY_EDITOR_HOST || 'localhost'}:${process.env.UNITY_EDITOR_PORT || '7777'}`);
}

main().catch((error) => {
  console.error('Fatal error:', error);
  process.exit(1);
});
