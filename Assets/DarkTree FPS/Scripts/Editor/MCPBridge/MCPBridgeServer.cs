using UnityEngine;
using UnityEditor;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        private static volatile bool isRunning = false;
        private static int port = 7777;

        // Состояние редактора, обновляемое на главном потоке. Читается из потоков HTTP,
        // потому что EditorApplication нельзя опрашивать вне главного потока.
        private static long lastMainThreadTickTicks;
        private static volatile bool cachedIsCompiling;
        private static volatile bool cachedIsUpdating;
        private static volatile bool cachedIsPlaying;

        // Очередь задач на главный поток Unity (обрабатывается в EditorApplication.update)
        private class MainThreadJob
        {
            public Func<object> work;
            public object result;
            public Exception error;
            // 0 - ждёт, 1 - взята главным потоком, 2 - отменена (клиент не дождался)
            public int state;
            public readonly ManualResetEventSlim done = new ManualResetEventSlim(false);
        }

        /// <summary>
        /// Редактор не успел выполнить команду: занят компиляцией, импортом или перезагрузкой скриптов
        /// </summary>
        private class EditorBusyException : Exception
        {
            public EditorBusyException(string message) : base(message) { }
        }

        /// <summary>
        /// Некорректный запрос клиента
        /// </summary>
        private class BadRequestException : Exception
        {
            public BadRequestException(string message) : base(message) { }
        }

        private static readonly System.Collections.Concurrent.ConcurrentQueue<MainThreadJob> mainThreadQueue =
            new System.Collections.Concurrent.ConcurrentQueue<MainThreadJob>();

        // Статический конструктор - запускается при загрузке Unity Editor
        static MCPBridgeServer()
        {
            EditorApplication.update += ProcessMainThreadQueue;
            EditorApplication.quitting += OnEditorQuitting;
            // Перед перезагрузкой скриптов сервер нужно остановить самим: иначе Unity обрывает
            // поток слушателя, а порт может остаться занятым до сборки мусора.
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

            UpdateEditorState();

            // Автозапуск если включен в настройках
            if (MCPBridgeSettings.AutoStartEnabled)
            {
                StartServer();
            }
        }

        private static void UpdateEditorState()
        {
            Interlocked.Exchange(ref lastMainThreadTickTicks, DateTime.UtcNow.Ticks);
            cachedIsCompiling = EditorApplication.isCompiling;
            cachedIsUpdating = EditorApplication.isUpdating;
            cachedIsPlaying = EditorApplication.isPlaying;
        }

        private static void ProcessMainThreadQueue()
        {
            UpdateEditorState();

            while (mainThreadQueue.TryDequeue(out var job))
            {
                // Клиент уже получил таймаут - выполнять команду задним числом нельзя
                if (Interlocked.CompareExchange(ref job.state, 1, 0) != 0)
                    continue;

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
            if (!isRunning)
                throw new EditorBusyException("MCP Bridge is stopping (script reload or editor shutdown). Retry in a few seconds.");

            var job = new MainThreadJob { work = work };
            mainThreadQueue.Enqueue(job);

            if (!job.done.Wait(timeoutMs))
            {
                // Отменяем, если главный поток ещё не взял задачу. Если уже взял - дожидаемся результата.
                if (Interlocked.CompareExchange(ref job.state, 2, 0) == 0)
                {
                    throw new EditorBusyException(
                        $"Unity main thread did not run '{jobName}' within {timeoutMs} ms. " +
                        DescribeEditorState() + " The command was cancelled and will not run later.");
                }

                job.done.Wait();
            }

            if (job.error != null)
            {
                throw job.error;
            }

            return job.result;
        }

        private static double MillisecondsSinceMainThreadTick()
        {
            long ticks = Interlocked.Read(ref lastMainThreadTickTicks);
            return (DateTime.UtcNow.Ticks - ticks) / (double)TimeSpan.TicksPerMillisecond;
        }

        private static string DescribeEditorState()
        {
            if (cachedIsCompiling) return "The editor is compiling scripts.";
            if (cachedIsUpdating) return "The editor is importing assets.";
            return $"The editor main thread has been busy for {MillisecondsSinceMainThreadTick():F0} ms.";
        }

        private static void OnEditorQuitting()
        {
            StopServerInternal(false);
        }

        private static void OnBeforeAssemblyReload()
        {
            StopServerInternal(false);
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
                // Оба адреса: часть клиентов (Node 18+) разрешает localhost в IPv6 и затем идёт на 127.0.0.1
                listener.Prefixes.Add($"http://localhost:{port}/");
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                listener.Start();
                isRunning = true;

                listenerThread = new Thread(ListenerLoop)
                {
                    IsBackground = true,
                    Name = "MCPBridge Server"
                };
                listenerThread.Start(listener);

                Debug.Log($"[MCPBridge] Server started on port {port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCPBridge] Failed to start server on port {port}: {e.Message}. " +
                    "The port may be used by another Unity Editor; change it in Window/MCP Bridge/Settings.");
                isRunning = false;
                try { listener?.Close(); } catch { }
                listener = null;
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

            StopServerInternal(true);
        }

        private static void StopServerInternal(bool log)
        {
            if (!isRunning && listener == null)
                return;

            // С этого момента новые команды сразу получают 503 "повторите позже"
            isRunning = false;

            // Сначала отвечаем ожидающим запросам, и только потом закрываем listener:
            // иначе клиент увидит обрыв соединения и не узнает, выполнилась ли команда
            bool hadPendingJobs = FailPendingJobs();
            if (hadPendingJobs)
                Thread.Sleep(150);

            try
            {
                // Close освобождает порт и прерывает ожидание GetContext в потоке слушателя
                listener?.Close();
            }
            catch (Exception e)
            {
                if (log) Debug.LogError($"[MCPBridge] Error stopping server: {e.Message}");
            }

            listener = null;

            try
            {
                if (listenerThread != null && listenerThread.IsAlive)
                    listenerThread.Join(1000);
            }
            catch { }

            listenerThread = null;

            FailPendingJobs();

            if (log) Debug.Log("[MCPBridge] Server stopped");
        }

        /// <summary>
        /// Завершает ошибкой "редактор занят" команды, которые главный поток уже не выполнит
        /// </summary>
        private static bool FailPendingJobs()
        {
            bool any = false;

            while (mainThreadQueue.TryDequeue(out var job))
            {
                if (Interlocked.CompareExchange(ref job.state, 1, 0) != 0)
                    continue;

                job.error = new EditorBusyException("MCP Bridge stopped before the command could run (script reload or editor shutdown). Retry in a few seconds.");
                job.done.Set();
                any = true;
            }

            return any;
        }

        [MenuItem("Window/MCP Bridge/Restart Server")]
        public static void RestartServer()
        {
            StopServerInternal(true);
            StartServer();
        }

        [MenuItem("Window/MCP Bridge/Settings")]
        public static void OpenSettings()
        {
            MCPBridgeSettingsWindow.ShowWindow();
        }

        private static void ListenerLoop(object state)
        {
            // Работаем со своим экземпляром: статическое поле может смениться при перезапуске сервера
            var activeListener = (HttpListener)state;

            while (isRunning)
            {
                try
                {
                    var context = activeListener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (ThreadAbortException)
                {
                    // Unity выгружает домен. Прерывание нельзя отменять - просто выходим.
                    return;
                }
                catch (HttpListenerException)
                {
                    // Сервер остановлен
                    return;
                }
                catch (ObjectDisposedException)
                {
                    // Listener был disposed - нормальная остановка
                    return;
                }
                catch (InvalidOperationException)
                {
                    // Listener остановлен между проверкой и GetContext
                    return;
                }
                catch (Exception e)
                {
                    if (!isRunning) return;
                    Debug.LogError($"[MCPBridge] Listener error: {e.Message}");
                    Thread.Sleep(100);
                }
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            // Метод выполняется в пуле потоков: наружу не должно выйти ни одно исключение
            try
            {
                var request = context.Request;
                var response = context.Response;

                // CORS headers
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                int statusCode = 200;
                string responseString = "";

                if (request.HttpMethod != "OPTIONS")
                {
                    try
                    {
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
                                statusCode = 404;
                                responseString = JsonConvert.SerializeObject(new
                                {
                                    success = false,
                                    error = "Endpoint not found",
                                    path = request.Url.AbsolutePath
                                });
                                break;
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        // 400 - ошибка в запросе, 503 - редактор занят (можно повторить), 500 - сбой команды
                        bool busy = e is EditorBusyException;
                        bool badRequest = e is BadRequestException || e is ArgumentException || e is JsonException;
                        statusCode = busy ? 503 : (badRequest ? 400 : 500);
                        if (busy) response.AddHeader("Retry-After", "3");

                        responseString = JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = e.Message,
                            errorType = busy ? "editor_busy" : (badRequest ? "bad_request" : "command_failed"),
                            retryable = busy,
                            stackTrace = (busy || badRequest) ? null : e.StackTrace
                        });
                    }
                }

                WriteResponse(response, statusCode, responseString);
            }
            catch (ThreadAbortException)
            {
                // Домен выгружается - ответ отправить уже нельзя
            }
            catch (Exception)
            {
                // Клиент отключился или listener закрыт - сообщать некому
            }
        }

        private static void WriteResponse(HttpListenerResponse response, int statusCode, string body)
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(body ?? string.Empty);
                response.StatusCode = statusCode;
                response.ContentEncoding = Encoding.UTF8;
                response.ContentType = "application/json; charset=utf-8";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        private static string HandleHealthCheck()
        {
            // Отвечает без главного потока, поэтому работает и во время компиляции/импорта
            double sinceTick = MillisecondsSinceMainThreadTick();

            return JsonConvert.SerializeObject(new
            {
                status = "ok",
                bridge = "DarkTreeFPS MCP Bridge",
                port,
                isRunning,
                isCompiling = cachedIsCompiling,
                isUpdating = cachedIsUpdating,
                isPlaying = cachedIsPlaying,
                mainThreadIdleMs = Math.Round(sinceTick),
                mainThreadResponsive = sinceTick < 2000,
                projectPath = projectPath,
                unityVersion = unityVersion,
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        // Application.* нельзя читать вне главного потока - запоминаем при загрузке
        private static readonly string projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);
        private static readonly string unityVersion = Application.unityVersion;

        private static JObject ParseJsonBody(HttpListenerRequest request)
        {
            string body = ReadRequestBody(request);
            if (string.IsNullOrWhiteSpace(body))
                throw new BadRequestException("Request body is empty; expected a JSON object.");

            try
            {
                return JObject.Parse(body);
            }
            catch (JsonException e)
            {
                throw new BadRequestException("Request body is not a valid JSON object: " + e.Message);
            }
        }

        private static string RequireString(JObject data, string key)
        {
            var token = data[key];
            string value = token != null && token.Type != JTokenType.Null ? token.ToString() : null;
            if (string.IsNullOrEmpty(value))
                throw new BadRequestException($"Missing required field '{key}'.");
            return value;
        }

        private static string HandleExecuteCommand(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request);

            string command = RequireString(data, "command");
            var args = data["args"] as JObject;

            if (command == "DebugLog")
            {
                // Debug.Log потокобезопасен, поэтому работает и при занятом главном потоке
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
            var data = ParseJsonBody(request);

            string path = RequireString(data, "path");
            var options = data["options"] as JObject;

            return (string)RunOnMainThread(
                () => UnityCommandExecutor.ImportAsset(path, options),
                30000, "ImportAsset");
        }

        private static string HandleAnimationRetarget(HttpListenerRequest request)
        {
            var data = ParseJsonBody(request).ToObject<Dictionary<string, object>>();

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
