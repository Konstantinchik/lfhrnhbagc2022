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

        // Очередь задач на главный поток Unity (обрабатывается в EditorApplication.update)
        private class MainThreadJob
        {
            public Func<object> work;
            public object result;
            public Exception error;
            public readonly ManualResetEventSlim done = new ManualResetEventSlim(false);
        }

        private static readonly System.Collections.Concurrent.BlockingCollection<MainThreadJob> mainThreadQueue =
            new System.Collections.Concurrent.BlockingCollection<MainThreadJob>();

        // Статический конструктор - запускается при загрузке Unity Editor
        static MCPBridgeServer()
        {
            EditorApplication.update += ProcessMainThreadQueue;
            EditorApplication.quitting += OnEditorQuitting;

            // Автозапуск если включен в настройках
            if (MCPBridgeSettings.AutoStartEnabled)
            {
                StartServer();
            }
        }

        private static void ProcessMainThreadQueue()
        {
            while (mainThreadQueue.TryTake(out var job))
            {
                try
                {
                    job.result = job.work();
                }
                catch (Exception e)
                {
                    job.error = e;
                }
                finally
                {
                    job.done.Set();
                }
            }
        }

        /// <summary>
        /// Выполнить работу на главном потоке Unity и дождаться результата
        /// </summary>
        private static object RunOnMainThread(Func<object> work, int timeoutMs, string jobName)
        {
            var job = new MainThreadJob { work = work };
            mainThreadQueue.Add(job);

            if (!job.done.Wait(timeoutMs))
            {
                throw new TimeoutException($"Unity command timed out: {jobName}");
            }

            if (job.error != null)
            {
                throw job.error;
            }

            return job.result;
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
                response.ContentEncoding = Encoding.UTF8;
                response.ContentType = "application/json; charset=utf-8";
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
                response.ContentEncoding = Encoding.UTF8;
                response.ContentType = "application/json; charset=utf-8";
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
                bridge = "DarkTreeFPS MCP Bridge",
                port,
                isRunning,
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        private static string HandleExecuteCommand(HttpListenerRequest request)
        {
            string body = ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            string command = data["command"].ToString();
            var args = data.ContainsKey("args") ? data["args"] as Newtonsoft.Json.Linq.JObject : null;

            if (command == "DebugLog")
            {
                object directResult = UnityCommandExecutor.Execute(command, args);
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    command,
                    result = directResult
                });
            }

            // Выполнить команду в главном потоке Unity через очередь задач
            object result = RunOnMainThread(
                () => UnityCommandExecutor.Execute(command, args),
                30000, command);

            return JsonConvert.SerializeObject(new
            {
                success = true,
                command,
                result
            });
        }
        private static string HandleGetSceneInfo()
        {
            return (string)RunOnMainThread(
                () => UnityCommandExecutor.GetSceneInfo(),
                30000, "GetSceneInfo");
        }

        private static string HandleImportAsset(HttpListenerRequest request)
        {
            string body = ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            string path = data["path"].ToString();
            var options = data.ContainsKey("options") ? data["options"] as Newtonsoft.Json.Linq.JObject : null;

            return (string)RunOnMainThread(
                () => UnityCommandExecutor.ImportAsset(path, options),
                30000, "ImportAsset");
        }

        private static string HandleAnimationRetarget(HttpListenerRequest request)
        {
            string body = ReadRequestBody(request);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(body);

            return (string)RunOnMainThread(
                () => AnimationRetargeting.RetargetAnimation(data),
                120000, "AnimationRetarget"); // Анимации могут требовать больше времени
        }

        private static string ReadRequestBody(HttpListenerRequest request)
        {
            // JSON requests from MCP clients are UTF-8. Use it explicitly instead of relying
            // on HttpListener's inferred/default request encoding (often code page 1252).
            using (var reader = new System.IO.StreamReader(
                request.InputStream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true))
            {
                return reader.ReadToEnd();
            }
        }

        public static bool IsRunning => isRunning;
        public static int Port => port;
    }
}
