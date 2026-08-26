using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DarkTreeFPS.MCPBridge
{
    /// <summary>
    /// HTTP сервер для приема команд от MCP Server
    /// Запускается автоматически при загрузке Unity Editor
    /// </summary>
    [InitializeOnLoad]
    public class MCPBridgeServer
    {
        private static HttpListener listener;
        private static Thread listenerThread;
        private static bool isRunning = false;
        private static int port = 7777;

        // Статический конструктор - запускается при загрузке Unity Editor
        static MCPBridgeServer()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.quitting += OnEditorQuitting;

            // Автозапуск если включен в настройках
            if (MCPBridgeSettings.AutoStartEnabled)
            {
                StartServer();
            }
        }

        private static void OnEditorUpdate()
        {
            // Проверка один раз при инициализации
            EditorApplication.update -= OnEditorUpdate;
        }

        private static void OnEditorQuitting()
        {
            StopServer();
        }

        [MenuItem("Window/MCP Bridge/Start Server")]
        public static void StartServer()
        {
            if (isRunning)
            {
                Debug.LogWarning("[MCPBridge] Server is already running");
                return;
            }

            port = MCPBridgeSettings.Port;

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/");
                listener.Start();
                isRunning = true;

                listenerThread = new Thread(ListenerLoop)
                {
                    IsBackground = true,
                    Name = "MCPBridge Server"
                };
                listenerThread.Start();

                Debug.Log($"[MCPBridge] Server started on port {port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCPBridge] Failed to start server: {e.Message}");
                isRunning = false;
            }
        }

        [MenuItem("Window/MCP Bridge/Stop Server")]
        public static void StopServer()
        {
            if (!isRunning)
            {
                Debug.LogWarning("[MCPBridge] Server is not running");
                return;
            }

            isRunning = false;

            try
            {
                listener?.Stop();
                listener?.Close();
                listenerThread?.Join(1000);
                Debug.Log("[MCPBridge] Server stopped");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCPBridge] Error stopping server: {e.Message}");
            }
        }

        [MenuItem("Window/MCP Bridge/Restart Server")]
        public static void RestartServer()
        {
            StopServer();
            Thread.Sleep(500);
            StartServer();
        }

        [MenuItem("Window/MCP Bridge/Settings")]
        public static void OpenSettings()
        {
            MCPBridgeSettingsWindow.ShowWindow();
        }

        private static void ListenerLoop()
        {
            try
            {
                while (isRunning)
                {
                    try
                    {
                        if (listener != null && listener.IsListening)
                        {
                            var context = listener.GetContext();
                            ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        // Unity останавливает поток при перекомпиляции - это нормально
                        Debug.Log("[MCPBridge] Listener thread stopped (compilation or editor closing)");
                        Thread.ResetAbort();
                        break;
                    }
                    catch (HttpListenerException)
                    {
                        // Сервер остановлен
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Listener был disposed - нормальная остановка
                        break;
                    }
                    catch (Exception e)
                    {
                        if (!isRunning) break;
                        Debug.LogError($"[MCPBridge] Listener error: {e.Message}");
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // Финальная обработка ThreadAbortException на уровне всего метода
                Debug.Log("[MCPBridge] Listener thread aborted gracefully");
                Thread.ResetAbort();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCPBridge] Fatal listener error: {e.Message}");
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // CORS headers
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            try
            {
                string responseString = "";

                switch (request.Url.AbsolutePath)
                {
                    case "/health":
                        responseString = HandleHealthCheck();
                        break;

                    case "/execute":
                        responseString = HandleExecuteCommand(request);
                        break;

                    case "/scene":
                        responseString = HandleGetSceneInfo();
                        break;

                    case "/import":
                        responseString = HandleImportAsset(request);
                        break;

                    case "/animation/retarget":
                        responseString = HandleAnimationRetarget(request);
                        break;

                    default:
                        response.StatusCode = 404;
                        responseString = JsonConvert.SerializeObject(new
                        {
                            error = "Endpoint not found",
                            path = request.Url.AbsolutePath
                        });
                        break;
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                response.StatusCode = 500;
                string errorResponse = JsonConvert.SerializeObject(new
                {
                    error = e.Message,
                    stackTrace = e.StackTrace
                });
                byte[] buffer = Encoding.UTF8.GetBytes(errorResponse);
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        private static string HandleHealthCheck()
        {
            return JsonConvert.SerializeObject(new
            {
                status = "ok",
                unityVersion = Application.unityVersion,
                projectName = Application.productName,
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        private static string HandleExecuteCommand(HttpListenerRequest request)
        {
            string body = ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            string command = data["command"].ToString();
            var args = data.ContainsKey("args") ? data["args"] as Newtonsoft.Json.Linq.JObject : null;

            // Выполнить команду в главном потоке Unity
            object result = null;
            Exception error = null;

            EditorApplication.delayCall += () =>
            {
                try
                {
                    result = UnityCommandExecutor.Execute(command, args);
                }
                catch (Exception e)
                {
                    error = e;
                }
            };

            // Ждем выполнения (максимум 10 секунд)
            int timeout = 100; // 10 секунд
            while (result == null && error == null && timeout > 0)
            {
                Thread.Sleep(100);
                timeout--;
            }

            if (error != null)
            {
                throw error;
            }

            return JsonConvert.SerializeObject(new
            {
                success = true,
                command,
                result
            });
        }

        private static string HandleGetSceneInfo()
        {
            string result = "";
            EditorApplication.delayCall += () =>
            {
                result = UnityCommandExecutor.GetSceneInfo();
            };

            int timeout = 50;
            while (string.IsNullOrEmpty(result) && timeout > 0)
            {
                Thread.Sleep(100);
                timeout--;
            }

            return result;
        }

        private static string HandleImportAsset(HttpListenerRequest request)
        {
            string body = ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            string path = data["path"].ToString();
            var options = data.ContainsKey("options") ? data["options"] as Newtonsoft.Json.Linq.JObject : null;

            string result = "";
            EditorApplication.delayCall += () =>
            {
                result = UnityCommandExecutor.ImportAsset(path, options);
            };

            int timeout = 100;
            while (string.IsNullOrEmpty(result) && timeout > 0)
            {
                Thread.Sleep(100);
                timeout--;
            }

            return result;
        }

        private static string HandleAnimationRetarget(HttpListenerRequest request)
        {
            string body = ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            string result = "";
            EditorApplication.delayCall += () =>
            {
                result = AnimationRetargeting.RetargetAnimation(data);
            };

            int timeout = 200; // Анимации могут требовать больше времени
            while (string.IsNullOrEmpty(result) && timeout > 0)
            {
                Thread.Sleep(100);
                timeout--;
            }

            return result;
        }

        private static string ReadRequestBody(HttpListenerRequest request)
        {
            using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        public static bool IsRunning => isRunning;
        public static int Port => port;
    }
}
