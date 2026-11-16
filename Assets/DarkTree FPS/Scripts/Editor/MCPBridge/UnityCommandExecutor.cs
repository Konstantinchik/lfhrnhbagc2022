using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DarkTreeFPS.MCPBridge
{
    /// <summary>
    /// Выполняет команды Unity Editor API по запросу от MCP сервера
    /// </summary>
    public static class UnityCommandExecutor
    {
        public static object Execute(string command, JObject args)
        {
            switch (command)
            {
                case "CreateGameObject":
                    return CreateGameObject(args);

                case "GetProjectStructure":
                    return GetProjectStructure(args);

                case "GetConsoleLogs":
                    return GetConsoleLogs(args);

                case "DebugLog":
                    return DebugLog(args);

                case "SaveScene":
                    return SaveScene();

                case "RefreshAssetDatabase":
                    AssetDatabase.Refresh();
                    return new { success = true, message = "Asset Database refreshed" };

                case "EnterPlayMode":
                    if (!EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = true;
                    }
                    return new
                    {
                        success = true,
                        isPlaying = EditorApplication.isPlaying,
                        message = "Entering Play Mode"
                    };

                case "ExitPlayMode":
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = false;
                    }
                    return new
                    {
                        success = true,
                        isPlaying = EditorApplication.isPlaying,
                        message = "Exiting Play Mode"
                    };

                case "IsPlayMode":
                    return new
                    {
                        success = true,
                        isPlaying = EditorApplication.isPlaying,
                        isPaused = EditorApplication.isPaused,
                        isCompiling = EditorApplication.isCompiling,
                        isUpdating = EditorApplication.isUpdating
                    };

                case "GetCompilationErrors":
                    return GetCompilationErrors();

                default:
                    throw new ArgumentException($"Unknown command: {command}");
            }
        }

        private static object CreateGameObject(JObject args)
        {
            string name = args?["name"]?.ToString() ?? "GameObject";
            string type = args?["type"]?.ToString() ?? "Empty";
            string parent = args?["parent"]?.ToString();

            GameObject go = null;

            switch (type.ToLower())
            {
                case "empty":
                    go = new GameObject(name);
                    break;
                case "cube":
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = name;
                    break;
                case "sphere":
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = name;
                    break;
                case "capsule":
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.name = name;
                    break;
                case "cylinder":
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.name = name;
                    break;
                case "plane":
                    go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    go.name = name;
                    break;
                default:
                    go = new GameObject(name);
                    break;
            }

            if (!string.IsNullOrEmpty(parent))
            {
                GameObject parentObj = GameObject.Find(parent);
                if (parentObj != null)
                {
                    go.transform.SetParent(parentObj.transform);
                }
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            Selection.activeGameObject = go;

            return new
            {
                success = true,
                gameObjectName = go.name,
                instanceId = go.GetInstanceID(),
                message = $"Created GameObject: {name}"
            };
        }

        public static string GetSceneInfo()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

            var rootObjects = scene.GetRootGameObjects();
            var gameObjects = new List<object>();

            foreach (var root in rootObjects)
            {
                gameObjects.Add(GetGameObjectInfo(root));
            }

            var result = new
            {
                sceneName = scene.name,
                scenePath = scene.path,
                isDirty = scene.isDirty,
                isLoaded = scene.isLoaded,
                rootCount = scene.rootCount,
                gameObjects = gameObjects
            };

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }

        private static object GetGameObjectInfo(GameObject go)
        {
            var components = go.GetComponents<Component>();
            var componentNames = new List<string>();
            foreach (var comp in components)
            {
                if (comp != null)
                    componentNames.Add(comp.GetType().Name);
            }

            var children = new List<object>();
            for (int i = 0; i < go.transform.childCount; i++)
            {
                children.Add(GetGameObjectInfo(go.transform.GetChild(i).gameObject));
            }

            return new
            {
                name = go.name,
                active = go.activeSelf,
                tag = go.tag,
                layer = LayerMask.LayerToName(go.layer),
                components = componentNames,
                childCount = go.transform.childCount,
                children = children.Count > 0 ? children : null
            };
        }

        private static object GetProjectStructure(JObject args)
        {
            int depth = args?["depth"]?.ToObject<int>() ?? 2;

            string assetsPath = Application.dataPath;
            var structure = ScanDirectory("Assets", depth, 0);

            return new
            {
                projectPath = assetsPath,
                structure = structure
            };
        }

        private static object ScanDirectory(string path, int maxDepth, int currentDepth)
        {
            if (currentDepth >= maxDepth)
                return null;

            var dirs = AssetDatabase.GetSubFolders(path);
            var files = System.IO.Directory.GetFiles(path);

            var directories = new List<object>();
            var fileList = new List<string>();

            foreach (var dir in dirs)
            {
                string dirName = System.IO.Path.GetFileName(dir);
                var subStructure = ScanDirectory(dir, maxDepth, currentDepth + 1);
                directories.Add(new
                {
                    name = dirName,
                    path = dir,
                    children = subStructure
                });
            }

            foreach (var file in files)
            {
                string fileName = System.IO.Path.GetFileName(file);
                if (!fileName.EndsWith(".meta"))
                {
                    fileList.Add(fileName);
                }
            }

            return new
            {
                directories = directories,
                files = fileList
            };
        }

        private static object DebugLog(JObject args)
        {
            string message = args?["message"]?.ToString() ?? string.Empty;
            string level = args?["level"]?.ToString()?.ToLowerInvariant() ?? "log";

            switch (level)
            {
                case "warning":
                case "warn":
                    Debug.LogWarning(message);
                    level = "warning";
                    break;

                case "error":
                    Debug.LogError(message);
                    break;

                default:
                    Debug.Log(message);
                    level = "log";
                    break;
            }

            return new
            {
                success = true,
                command = "DebugLog",
                level,
                message
            };
        }

        private static object GetConsoleLogs(JObject args)
        {
            int count = args?["count"]?.ToObject<int>() ?? 50;
            string filter = args?["filter"]?.ToString()?.ToLower() ?? "all";

            var logs = new List<object>();
            int errorCount = 0, warningCount = 0, logCount = 0;

            try
            {
                Assembly editorAssembly = typeof(EditorWindow).Assembly;
                Type logEntriesType = editorAssembly.GetType("UnityEditor.LogEntries");
                Type logEntryType = editorAssembly.GetType("UnityEditor.LogEntry");

                if (logEntriesType == null || logEntryType == null)
                {
                    return new
                    {
                        success = false,
                        error = "LogEntries reflection not available in this Unity version",
                        count = 0,
                        logs = logs
                    };
                }

                // Надёжные счётчики (после StartGettingEntries)
                MethodInfo getCountByMode = logEntriesType.GetMethod("GetCountByMode");
                MethodInfo getCount = logEntriesType.GetMethod("GetCount");

                // Чтение записей: GetEntryCount + GetEntryInternal (Unity 2022+)
                MethodInfo getEntryCount = logEntriesType.GetMethod("GetEntryCount");
                MethodInfo getEntryInternal = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo startGetting = logEntriesType.GetMethod("StartGettingEntries");
                MethodInfo endGetting = logEntriesType.GetMethod("EndGettingEntries");

                if (getEntryCount == null || getEntryInternal == null || startGetting == null || endGetting == null)
                {
                    return new
                    {
                        success = true,
                        error = errorCount,
                        warning = warningCount,
                        info = logCount,
                        message = "Console entry API not available in this Unity version",
                        count = 0,
                        logs = logs
                    };
                }

                FieldInfo messageField = logEntryType.GetField("message") ?? logEntryType.GetField("condition");
                FieldInfo modeField = logEntryType.GetField("mode");

                // Счётчики ДО StartGettingEntries (после старта сессии они кидают исключения)
                if (getCountByMode != null)
                {
                    try { errorCount = (int)getCountByMode.Invoke(null, new object[] { 0 }); } catch { }
                    try { warningCount = (int)getCountByMode.Invoke(null, new object[] { 2 }); } catch { }
                    try { logCount = (int)getCountByMode.Invoke(null, new object[] { 3 }); } catch { }
                }

                MethodInfo getRows = logEntriesType.GetMethod("GetRows");
                List<int> rows = new List<int>();

                // Всего записей: сумма по классам 0..2 (error/assert/warning/log)
                int totalEntries = 0;
                for (int m = 0; m <= 2; m++)
                {
                    try { totalEntries += (int)getEntryCount.Invoke(null, new object[] { m }); } catch { }
                }

                startGetting.Invoke(null, null);
                try
                {
                    // Сначала пробуем GetRows, но fallback - прямые индексы 0..total-1
                    if (getRows != null)
                    {
                        try { getRows.Invoke(null, new object[] { rows }); } catch { }
                    }
                    if (rows.Count != totalEntries)
                    {
                        rows.Clear();
                        for (int i = 0; i < totalEntries; i++) rows.Add(i);
                    }

                    // Зондирование: счётчики по режимам 0..3 через GetEntryCount
                    if (args?["debug"]?.ToObject<bool>() == true)
                    {
                        var probe = new Dictionary<string, object>();
                        probe["GetRows"] = rows.Count;
                        for (int m = 0; m <= 3; m++)
                        {
                            try { probe["GetEntryCount(" + m + ")"] = (int)getEntryCount.Invoke(null, new object[] { m }); } catch (Exception pe) { probe["GetEntryCount(" + m + ")"] = pe.Message; }
                        }
                        logs.Add(new { level = "probe", probe });
                    }

                    // Новые записи в конце списка - идём с конца по валидным строкам
                    for (int r = rows.Count - 1; r >= 0 && logs.Count < count; r--)
                    {
                        object entry = Activator.CreateInstance(logEntryType);
                        object[] entryArgs = new object[] { rows[r], entry };
                        getEntryInternal.Invoke(null, entryArgs);
                        entry = entryArgs[1];

                        string condition = messageField?.GetValue(entry) as string ?? string.Empty;
                        int mode = (int)(modeField?.GetValue(entry) ?? 0);

                        // Биты уровня в LogEntry.mode (определены эмпирически для Unity 2022):
                        // error = 1<<23, warning = 1<<21, остальное - info
                        string level;
                        if ((mode & (1 << 23)) != 0) level = "error";
                        else if ((mode & (1 << 21)) != 0) level = "warning";
                        else level = "info";

                        object logItem;
                        if (args?["debug"]?.ToObject<bool>() == true)
                        {
                            var fields = new Dictionary<string, object>();
                            foreach (var f in logEntryType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            {
                                fields[f.Name] = f.GetValue(entry);
                            }
                            var props = new Dictionary<string, object>();
                            foreach (var p in logEntryType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            {
                                try { props[p.Name] = p.GetValue(entry); } catch { }
                            }
                            logItem = new { level, message = condition, mode, fields, properties = props };
                        }
                        else
                        {
                            logItem = new { level, message = condition };
                        }

                        if (filter == "all" || filter == level)
                        {
                            logs.Add(logItem);
                        }
                    }
                }
                finally
                {
                    endGetting.Invoke(null, null);
                }
            }
            catch (Exception e)
            {
                Exception inner = (e is TargetInvocationException tie && tie.InnerException != null) ? tie.InnerException : e;
                return new
                {
                    success = false,
                    error = inner.Message,
                    stackTrace = inner.StackTrace,
                    signatures = GetLogMethodSignatures(),
                    count = logs.Count,
                    logs = logs
                };
            }

            return new
            {
                success = true,
                error = errorCount,
                warning = warningCount,
                info = logCount,
                count = logs.Count,
                logs = logs
            };
        }

        private static List<string> GetLogMethodSignatures()
        {
            var signatures = new List<string>();
            try
            {
                Type logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                if (logEntriesType == null) return signatures;

                foreach (var name in new[] { "GetEntryInternal", "GetEntryCount", "StartGettingEntries", "GetCountsByType" })
                {
                    foreach (var m in logEntriesType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                    {
                        if (m.Name != name) continue;
                        var ps = m.GetParameters();
                        var sig = name + "(" + string.Join(", ", System.Array.ConvertAll(ps, p => (p.ParameterType.IsByRef ? "ref " : "") + p.ParameterType.Name)) + ")";
                        signatures.Add(sig);
                    }
                }
            }
            catch { }

            return signatures;
        }

        private static object GetCompilationErrors()
        {
            int errorCount = 0, warningCount = 0;

            try
            {
                Type logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                MethodInfo getCountByMode = logEntriesType?.GetMethod("GetCountByMode");
                if (getCountByMode != null)
                {
                    errorCount = (int)getCountByMode.Invoke(null, new object[] { 0 });
                    warningCount = (int)getCountByMode.Invoke(null, new object[] { 2 });
                }
            }
            catch (Exception)
            {
                // счётчики недоступны - вернём только статус компиляции
            }

            return new
            {
                success = true,
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                isPlaying = EditorApplication.isPlaying,
                errorCount = errorCount,
                warningCount = warningCount,
                message = EditorApplication.isCompiling
                    ? "Compilation in progress"
                    : (errorCount > 0 ? $"{errorCount} compile error(s) in console" : "No compilation errors")
            };
        }

        public static string ImportAsset(string path, JObject options)
        {
            string destination = options?["destination"]?.ToString();

            if (string.IsNullOrEmpty(destination))
            {
                destination = "Assets/Imported/";
            }

            try
            {
                // Проверить существование файла
                if (!System.IO.File.Exists(path))
                {
                    throw new System.IO.FileNotFoundException($"File not found: {path}");
                }

                // Убедиться что destination находится в Assets
                if (!destination.StartsWith("Assets/"))
                {
                    destination = "Assets/" + destination;
                }

                // Создать папку если не существует
                string destinationDir = System.IO.Path.GetDirectoryName(destination);
                if (!AssetDatabase.IsValidFolder(destinationDir))
                {
                    string[] folders = destinationDir.Split('/');
                    string currentPath = "";
                    foreach (string folder in folders)
                    {
                        if (string.IsNullOrEmpty(folder)) continue;

                        string newPath = string.IsNullOrEmpty(currentPath) ? folder : currentPath + "/" + folder;
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            string parentPath = string.IsNullOrEmpty(currentPath) ? "" : currentPath;
                            AssetDatabase.CreateFolder(parentPath, folder);
                        }
                        currentPath = newPath;
                    }
                }

                // Копировать файл
                string fileName = System.IO.Path.GetFileName(path);
                string fullDestination = destination.EndsWith("/") ? destination + fileName : destination;

                System.IO.File.Copy(path, fullDestination, true);
                AssetDatabase.ImportAsset(fullDestination);
                AssetDatabase.Refresh();

                var result = new
                {
                    success = true,
                    sourcePath = path,
                    destinationPath = fullDestination,
                    message = $"Asset imported successfully: {fileName}"
                };

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception e)
            {
                var result = new
                {
                    success = false,
                    error = e.Message,
                    sourcePath = path
                };

                return JsonConvert.SerializeObject(result);
            }
        }

        private static object SaveScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            return new
            {
                success = saved,
                sceneName = scene.name,
                scenePath = scene.path,
                message = saved ? "Scene saved successfully" : "Failed to save scene"
            };
        }
    }
}
