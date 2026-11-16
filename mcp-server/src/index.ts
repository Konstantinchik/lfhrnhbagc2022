#!/usr/bin/env node
import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';
import axios, { AxiosInstance } from 'axios';
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

  async isConnected(): Promise<boolean> {
    try {
      const response = await this.http.get(`/health`, { timeout: HEALTH_TIMEOUT_MS });
      return response.status === 200;
    } catch {
      return false;
    }
  }

  async executeCommand(command: string, args?: Record<string, any>): Promise<any> {
    const response = await this.http.post(`/execute`, {
      command,
      args: args || {}
    });
    return response.data;
  }

  async getSceneInfo(): Promise<any> {
    const response = await this.http.get(`/scene`);
    return response.data;
  }

  async importAsset(path: string, options?: Record<string, any>): Promise<any> {
    const response = await this.http.post(`/import`, {
      path,
      options: options || {}
    });
    return response.data;
  }

  async retargetAnimation(options: {
    sourceAnimationPath: string;
    sourceSkeletonType: string;
    targetSkeletonType: string;
    outputPath: string;
  }): Promise<any> {
    const response = await this.http.post(`/animation/retarget`, options, { timeout: ANIMATION_TIMEOUT_MS });
    return response.data;
  }
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
    description: 'Get recent Unity Console logs for debugging',
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
    description: 'Check Unity script compilation status and get recent compile errors',
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
        const connected = await unityClient.isConnected();
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify({
                connected,
                status: connected ? 'Unity Editor MCP Bridge is active' : 'Unity Editor not responding',
                bridge_url: process.env.UNITY_EDITOR_HOST || 'localhost',
                port: process.env.UNITY_EDITOR_PORT || '7777',
              }, null, 2),
            },
          ],
        };
      }

      case 'unity_execute_command': {
        const { command, args: cmdArgs } = args as { command: string; args?: Record<string, any> };
        const result = await unityClient.executeCommand(command, cmdArgs);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case 'unity_get_scene_info': {
        const sceneInfo = await unityClient.getSceneInfo();
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(sceneInfo, null, 2),
            },
          ],
        };
      }

      case 'unity_debug_log': {
        const { message, level } = args as { message: string; level?: 'log' | 'warning' | 'error' };
        const result = await unityClient.executeCommand('DebugLog', {
          message,
          level: level || 'log',
        });
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case 'unity_create_gameobject': {
        const { name: goName, type, parent } = args as { name: string; type?: string; parent?: string };
        const result = await unityClient.executeCommand('CreateGameObject', {
          name: goName,
          type: type || 'Empty',
          parent,
        });
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case 'unity_import_asset': {
        const { path, destination } = args as { path: string; destination?: string };
        const result = await unityClient.importAsset(path, { destination });
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case 'unity_animation_retarget': {
        const retargetArgs = args as {
          sourceAnimationPath: string;
          sourceSkeletonType: string;
          targetSkeletonType: string;
          outputPath: string;
        };
        const result = await unityClient.retargetAnimation(retargetArgs);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case 'unity_get_project_structure': {
        const { depth } = args as { depth?: number };
        const result = await unityClient.executeCommand('GetProjectStructure', { depth: depth || 2 });
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case 'unity_console_logs': {
        const { count, filter } = args as { count?: number; filter?: string };
        const result = await unityClient.executeCommand('GetConsoleLogs', {
          count: count || 50,
          filter: filter || 'all',
        });
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case 'unity_play_mode': {
        const { action } = args as { action: 'enter' | 'exit' | 'status' };
        const result = await unityClient.executeCommand(
          action === 'enter' ? 'EnterPlayMode'
          : action === 'exit' ? 'ExitPlayMode'
          : 'IsPlayMode'
        );
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      }

      case 'unity_compilation_check': {
        const result = await unityClient.executeCommand('GetCompilationErrors');
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
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
