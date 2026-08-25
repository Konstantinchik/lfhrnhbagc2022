using UnityEngine;
using UnityEditor;

namespace DarkTreeFPS.MCPBridge
{
    /// <summary>
    /// Настройки MCP Bridge сохраняются в EditorPrefs
    /// </summary>
    public static class MCPBridgeSettings
    {
        private const string KEY_PORT = "MCPBridge_Port";
        private const string KEY_AUTO_START = "MCPBridge_AutoStart";

        private const int DEFAULT_PORT = 7777;
        private const bool DEFAULT_AUTO_START = true;

        public static int Port
        {
            get => EditorPrefs.GetInt(KEY_PORT, DEFAULT_PORT);
            set => EditorPrefs.SetInt(KEY_PORT, value);
        }

        public static bool AutoStartEnabled
        {
            get => EditorPrefs.GetBool(KEY_AUTO_START, DEFAULT_AUTO_START);
            set => EditorPrefs.SetBool(KEY_AUTO_START, value);
        }
    }

    /// <summary>
    /// Окно настроек MCP Bridge
    /// </summary>
    public class MCPBridgeSettingsWindow : EditorWindow
    {
        private int port;
        private bool autoStart;

        public static void ShowWindow()
        {
            var window = GetWindow<MCPBridgeSettingsWindow>("MCP Bridge Settings");
            window.minSize = new Vector2(400, 250);
            window.Show();
        }

        private void OnEnable()
        {
            port = MCPBridgeSettings.Port;
            autoStart = MCPBridgeSettings.AutoStartEnabled;
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            EditorGUILayout.LabelField("MCP Bridge Configuration", EditorStyles.boldLabel);

            GUILayout.Space(10);

            // Статус сервера
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Server Status", EditorStyles.boldLabel);

            string status = MCPBridgeServer.IsRunning ? "Running" : "Stopped";
            Color statusColor = MCPBridgeServer.IsRunning ? Color.green : Color.red;

            var previousColor = GUI.contentColor;
            GUI.contentColor = statusColor;
            EditorGUILayout.LabelField("Status:", status);
            GUI.contentColor = previousColor;

            if (MCPBridgeServer.IsRunning)
            {
                EditorGUILayout.LabelField("Port:", MCPBridgeServer.Port.ToString());
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Настройки
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            port = EditorGUILayout.IntField("Port:", port);
            if (port < 1024 || port > 65535)
            {
                EditorGUILayout.HelpBox("Port должен быть между 1024 и 65535", MessageType.Warning);
            }

            autoStart = EditorGUILayout.Toggle("Auto-start on Editor load:", autoStart);

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Кнопки управления
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Settings", GUILayout.Height(30)))
            {
                MCPBridgeSettings.Port = port;
                MCPBridgeSettings.AutoStartEnabled = autoStart;
                Debug.Log("[MCPBridge] Settings saved");
            }

            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                port = 7777;
                autoStart = true;
                MCPBridgeSettings.Port = port;
                MCPBridgeSettings.AutoStartEnabled = autoStart;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Управление сервером
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Server Control", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !MCPBridgeServer.IsRunning;
            if (GUILayout.Button("Start Server", GUILayout.Height(35)))
            {
                MCPBridgeServer.StartServer();
            }

            GUI.enabled = MCPBridgeServer.IsRunning;
            if (GUILayout.Button("Stop Server", GUILayout.Height(35)))
            {
                MCPBridgeServer.StopServer();
            }

            GUI.enabled = true;
            if (GUILayout.Button("Restart Server", GUILayout.Height(35)))
            {
                MCPBridgeServer.RestartServer();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Информация
            EditorGUILayout.HelpBox(
                "MCP Bridge позволяет AI агентам (Claude) взаимодействовать с Unity Editor через HTTP API.\n\n" +
                "Убедитесь, что порт не занят другими приложениями.\n" +
                "MCP Server должен быть запущен отдельно: npm run dev в папке mcp-server/",
                MessageType.Info
            );
        }
    }
}
