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

                case "CaptureScreenshot":
                    return CaptureScreenshot();

                case "DumpUI":
                    return DumpUI(args);

                case "DebugInventory":
                    return DebugInventory(args);

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

        // Биты LogEntry.mode (UnityEditor.ConsoleWindow.Mode, Unity 2022)
        private const int ModeError = 1 << 0;
        private const int ModeAssert = 1 << 1;
        private const int ModeFatal = 1 << 4;
        private const int ModeAssetImportError = 1 << 6;
        private const int ModeAssetImportWarning = 1 << 7;
        private const int ModeScriptingError = 1 << 8;
        private const int ModeScriptingWarning = 1 << 9;
        private const int ModeScriptCompileError = 1 << 11;
        private const int ModeScriptCompileWarning = 1 << 12;
        private const int ModeScriptingException = 1 << 17;
        private const int ModeGraphCompileError = 1 << 20;
        private const int ModeScriptingAssertion = 1 << 21;

        private const int ErrorModeMask = ModeError | ModeAssert | ModeFatal | ModeAssetImportError |
            ModeScriptingError | ModeScriptCompileError | ModeScriptingException | ModeGraphCompileError | ModeScriptingAssertion;
        private const int WarningModeMask = ModeAssetImportWarning | ModeScriptingWarning | ModeScriptCompileWarning;

        private static string LevelFromMode(int mode)
        {
            if ((mode & ErrorModeMask) != 0) return "error";
            if ((mode & WarningModeMask) != 0) return "warning";
            return "info";
        }

        private class ConsoleEntry
        {
            public string level;
            public string message;
            public string stackTrace;
            public string file;
            public int line;
            public int mode;
        }

        private class ConsoleSnapshot
        {
            public bool available;
            public string unavailableReason;
            public int total;
            public int errorCount, warningCount, infoCount;
            public readonly List<ConsoleEntry> entries = new List<ConsoleEntry>();
        }

        /// <summary>
        /// Читает записи Console от новых к старым через внутренний UnityEditor.LogEntries.
        /// Список соответствует тому, что показывает окно Console (с учётом его фильтров и Collapse).
        /// </summary>
        private static ConsoleSnapshot ReadConsole(int maxEntries, Func<ConsoleEntry, bool> predicate)
        {
            var snapshot = new ConsoleSnapshot();

            Assembly editorAssembly = typeof(EditorWindow).Assembly;
            Type logEntriesType = editorAssembly.GetType("UnityEditor.LogEntries");
            Type logEntryType = editorAssembly.GetType("UnityEditor.LogEntry");
            const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            MethodInfo startGetting = logEntriesType?.GetMethod("StartGettingEntries", StaticFlags);
            MethodInfo endGetting = logEntriesType?.GetMethod("EndGettingEntries", StaticFlags);
            MethodInfo getEntryInternal = logEntriesType?.GetMethod("GetEntryInternal", StaticFlags);
            MethodInfo getCount = logEntriesType?.GetMethod("GetCount", StaticFlags);
            MethodInfo getCountsByType = logEntriesType?.GetMethod("GetCountsByType", StaticFlags);

            if (logEntryType == null || startGetting == null || endGetting == null || getEntryInternal == null)
            {
                snapshot.unavailableReason = "Console entry API (UnityEditor.LogEntries) is not available in this Unity version";
                return snapshot;
            }

            snapshot.available = true;

            FieldInfo messageField = logEntryType.GetField("message", InstanceFlags) ?? logEntryType.GetField("condition", InstanceFlags);
            FieldInfo modeField = logEntryType.GetField("mode", InstanceFlags);
            FieldInfo fileField = logEntryType.GetField("file", InstanceFlags);
            FieldInfo lineField = logEntryType.GetField("line", InstanceFlags);
            FieldInfo callstackStartField = logEntryType.GetField("callstackTextStartUTF16", InstanceFlags);

            if (getCountsByType != null)
            {
                try
                {
                    object[] counts = { 0, 0, 0 };
                    getCountsByType.Invoke(null, counts);
                    snapshot.errorCount = (int)counts[0];
                    snapshot.warningCount = (int)counts[1];
                    snapshot.infoCount = (int)counts[2];
                }
                catch { }
            }

            // StartGettingEntries возвращает число строк; GetCount - запасной вариант
            int total = 0;
            object started = startGetting.Invoke(null, null);
            try
            {
                if (started is int startedCount)
                    total = startedCount;
                else if (getCount != null)
                    total = (int)getCount.Invoke(null, null);

                snapshot.total = total;

                // Новые записи в конце списка - идём с конца
                for (int row = total - 1; row >= 0 && snapshot.entries.Count < maxEntries; row--)
                {
                    object entry = Activator.CreateInstance(logEntryType);
                    object[] entryArgs = { row, entry };
                    object got = getEntryInternal.Invoke(null, entryArgs);
                    if (got is bool ok && !ok)
                        continue;

                    entry = entryArgs[1];

                    string text = messageField?.GetValue(entry) as string ?? string.Empty;
                    int mode = modeField != null ? Convert.ToInt32(modeField.GetValue(entry)) : 0;

                    // Текст записи: сообщение, затем стек вызовов
                    int split = callstackStartField != null ? Convert.ToInt32(callstackStartField.GetValue(entry)) : 0;
                    if (split <= 0 || split > text.Length)
                    {
                        split = text.IndexOf('\n');
                        if (split < 0) split = text.Length;
                    }

                    var item = new ConsoleEntry
                    {
                        level = LevelFromMode(mode),
                        message = text.Substring(0, split).TrimEnd('\r', '\n'),
                        stackTrace = text.Substring(split).Trim('\r', '\n'),
                        file = fileField?.GetValue(entry) as string,
                        line = lineField != null ? Convert.ToInt32(lineField.GetValue(entry)) : 0,
                        mode = mode
                    };

                    if (predicate == null || predicate(item))
                        snapshot.entries.Add(item);
                }
            }
            finally
            {
                endGetting.Invoke(null, null);
            }

            return snapshot;
        }

        private static object GetConsoleLogs(JObject args)
        {
            int count = Mathf.Clamp(args?["count"]?.ToObject<int>() ?? 50, 1, 1000);
            string filter = args?["filter"]?.ToString()?.ToLowerInvariant() ?? "all";
            if (filter == "log") filter = "info";
            bool includeStackTrace = args?["stackTrace"]?.ToObject<bool>() == true;
            bool debug = args?["debug"]?.ToObject<bool>() == true;

            try
            {
                var snapshot = ReadConsole(count, e => filter == "all" || filter == e.level);

                if (!snapshot.available)
                {
                    return new
                    {
                        success = false,
                        error = snapshot.unavailableReason,
                        count = 0,
                        logs = new List<object>()
                    };
                }

                var logs = new List<object>();
                foreach (var e in snapshot.entries)
                {
                    var item = new Dictionary<string, object>
                    {
                        ["level"] = e.level,
                        ["message"] = e.message
                    };

                    if (!string.IsNullOrEmpty(e.file))
                    {
                        item["file"] = e.file;
                        item["line"] = e.line;
                    }

                    if (includeStackTrace && !string.IsNullOrEmpty(e.stackTrace))
                        item["stackTrace"] = e.stackTrace;

                    if (debug)
                        item["mode"] = e.mode;

                    logs.Add(item);
                }

                return new
                {
                    success = true,
                    // Счётчики и total относятся ко всей консоли, count - к возвращённым записям (сначала новые)
                    error = snapshot.errorCount,
                    warning = snapshot.warningCount,
                    info = snapshot.infoCount,
                    total = snapshot.total,
                    count = logs.Count,
                    logs = logs
                };
            }
            catch (Exception e)
            {
                Exception inner = (e is TargetInvocationException tie && tie.InnerException != null) ? tie.InnerException : e;
                return new
                {
                    success = false,
                    error = inner.Message,
                    stackTrace = inner.StackTrace,
                    count = 0,
                    logs = new List<object>()
                };
            }
        }

        private static object GetCompilationErrors()
        {
            var errors = new List<object>();
            int compileErrorCount = 0, compileWarningCount = 0;
            string readError = null;

            try
            {
                // Ошибки компиляции остаются в консоли, пока не исправлены, поэтому читаем всю консоль
                var snapshot = ReadConsole(int.MaxValue, e =>
                    (e.mode & (ModeScriptCompileError | ModeScriptCompileWarning)) != 0);

                if (!snapshot.available)
                    readError = snapshot.unavailableReason;

                foreach (var e in snapshot.entries)
                {
                    if ((e.mode & ModeScriptCompileError) != 0)
                    {
                        compileErrorCount++;
                        if (errors.Count < 50)
                            errors.Add(new { message = e.message, file = e.file, line = e.line });
                    }
                    else
                    {
                        compileWarningCount++;
                    }
                }
            }
            catch (Exception e)
            {
                Exception inner = (e is TargetInvocationException tie && tie.InnerException != null) ? tie.InnerException : e;
                readError = inner.Message;
            }

            // Надёжный признак от самого редактора: последняя компиляция завершилась с ошибками
            bool compilationFailed = EditorUtility.scriptCompilationFailed;
            bool isCompiling = EditorApplication.isCompiling;

            string message;
            if (isCompiling) message = "Compilation in progress";
            else if (compilationFailed || compileErrorCount > 0) message = $"Compilation failed: {compileErrorCount} error(s) in console";
            else message = "No compilation errors";

            return new
            {
                success = true,
                isCompiling,
                isUpdating = EditorApplication.isUpdating,
                isPlaying = EditorApplication.isPlaying,
                compilationFailed,
                errorCount = compileErrorCount,
                warningCount = compileWarningCount,
                errors,
                consoleReadError = readError,
                message
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

        /// <summary>
        /// Сохраняет снимок Game view в Temp/UnityTestLogs. Работает только в Play Mode;
        /// файл появляется в конце текущего кадра.
        /// </summary>
        private static object CaptureScreenshot()
        {
            if (!EditorApplication.isPlaying)
            {
                return new
                {
                    success = false,
                    error = "CaptureScreenshot works only in Play Mode: the Game view is not rendered otherwise."
                };
            }

            string directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Temp/UnityTestLogs"));
            System.IO.Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, "screenshot-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".png");

            ScreenCapture.CaptureScreenshot(path);

            return new
            {
                success = true,
                path,
                width = Screen.width,
                height = Screen.height,
                message = "Screenshot is written at the end of the current frame"
            };
        }

        /// <summary>
        /// Выгружает дерево UI под объектом с заданным именем: размеры, спрайты, цвета, порядок.
        /// В отличие от /scene видит и объекты из DontDestroyOnLoad (игрок в Play Mode).
        /// args: root (имя объекта), depth (по умолчанию 3), maxChildren (по умолчанию 40)
        /// </summary>
        private static object DumpUI(JObject args)
        {
            string rootName = args?["root"]?.ToString();
            int depth = args?["depth"]?.ToObject<int>() ?? 3;
            int maxChildren = args?["maxChildren"]?.ToObject<int>() ?? 40;

            if (string.IsNullOrEmpty(rootName))
                throw new ArgumentException("DumpUI requires 'root': the name of the GameObject to dump.");

            var roots = new List<object>();
            foreach (var rect in UnityEngine.Object.FindObjectsOfType<RectTransform>(true))
            {
                if (rect.name == rootName)
                    roots.Add(DumpUINode(rect, depth, maxChildren));
            }

            return new { success = true, root = rootName, found = roots.Count, nodes = roots };
        }

        private static object DumpUINode(RectTransform rect, int depth, int maxChildren)
        {
            var node = new Dictionary<string, object>
            {
                ["name"] = rect.name,
                ["active"] = rect.gameObject.activeInHierarchy,
                ["pos"] = $"{rect.anchoredPosition.x:F0},{rect.anchoredPosition.y:F0}",
                ["size"] = $"{rect.rect.width:F0}x{rect.rect.height:F0}",
                ["scale"] = $"{rect.localScale.x:F2},{rect.localScale.y:F2},{rect.localScale.z:F2}"
            };

            var image = rect.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                node["image"] = (image.enabled ? "" : "(disabled) ") +
                    (image.sprite != null ? image.sprite.name : "no sprite") + " " + image.type +
                    $" rgba({image.color.r:F2},{image.color.g:F2},{image.color.b:F2},{image.color.a:F2})";
            }

            var others = new List<string>();
            foreach (var component in rect.GetComponents<Component>())
            {
                if (component == null || component is RectTransform || component is CanvasRenderer || component is UnityEngine.UI.Image)
                    continue;
                others.Add(component.GetType().Name);
            }
            if (others.Count > 0)
                node["components"] = string.Join(",", others);

            var inventory = rect.GetComponent<DTInventory.DTInventory>();
            if (inventory != null)
            {
                node["cellColors"] = $"normal={inventory.normalCellColor} hovered={inventory.hoveredCellColor} blocked={inventory.blockedCellColor}";
                node["slots"] = inventory.slots != null ? inventory.slots.Count : 0;
                node["items"] = inventory.inventoryItems != null ? inventory.inventoryItems.Count : 0;
            }

            var slot = rect.GetComponent<DTInventory.GridSlot>();
            if (slot != null)
            {
                node["slot"] = $"{slot.x},{slot.y} free={slot.free} ownImage={(slot.image != null && slot.image.gameObject == rect.gameObject)}";
            }

            node["childCount"] = rect.childCount;

            if (depth > 0 && rect.childCount > 0)
            {
                var children = new List<object>();
                for (int i = 0; i < rect.childCount && i < maxChildren; i++)
                {
                    if (rect.GetChild(i) is RectTransform child)
                        children.Add(DumpUINode(child, depth - 1, maxChildren));
                }
                node["children"] = children;
            }

            return node;
        }

        /// <summary>
        /// Отладка инвентаря в Play Mode без ввода с клавиатуры.
        /// args: open (bool) - открыть/закрыть окно; addItem (string) - название предмета из AssetsDatabase,
        /// который нужно создать и положить в инвентарь тем же путём, что и при подборе.
        /// </summary>
        private static object DebugInventory(JObject args)
        {
            if (!EditorApplication.isPlaying)
                return new { success = false, error = "DebugInventory works only in Play Mode." };

            var inventory = UnityEngine.Object.FindObjectOfType<DTInventory.DTInventory>();
            if (inventory == null)
                return new { success = false, error = "DTInventory not found." };

            string added = null;
            bool? addResult = null;
            string title = args?["addItem"]?.ToString();

            if (!string.IsNullOrEmpty(title))
            {
                var database = UnityEngine.Object.FindObjectOfType<DTInventory.AssetsDatabase>();
                var prefab = database != null ? database.FindItem(title) : null;
                if (prefab == null)
                    return new { success = false, error = "Item '" + title + "' not found in AssetsDatabase." };

                var item = UnityEngine.Object.Instantiate(prefab);
                addResult = inventory.AddItem(item);
                added = item.title;
            }

            if (args?["open"] != null)
                DTInventory.InventoryManager.showInventory = args["open"].ToObject<bool>();

            // drag (название предмета) + dropOnPanel (индекс слота экипировки): проводит предмет через
            // настоящие обработчики OnBeginDrag/OnEndDrag. dragFromPanel выбирает экземпляр в слоте,
            // иначе берётся экземпляр из сетки.
            string dragResult = null;
            string dragTitle = args?["drag"]?.ToString();
            if (!string.IsNullOrEmpty(dragTitle) && args?["dropOnPanel"] != null)
            {
                int dropIndex = args["dropOnPanel"].ToObject<int>();
                int? fromIndex = args["dragFromPanel"] != null ? args["dragFromPanel"].ToObject<int>() : (int?)null;

                if (dropIndex < 0 || dropIndex >= inventory.equipmentPanels.Count)
                    return new { success = false, error = "dropOnPanel is out of range." };

                var targetPanel = inventory.equipmentPanels[dropIndex];
                DTInventory.InventoryItem source = null;
                DTInventory.InventoryItem occupant = null;

                foreach (var candidate in inventory.inventoryItems)
                {
                    if (candidate == null || candidate.item == null)
                        continue;

                    var candidateSlot = inventory.FindSlotByIndex(candidate.x, candidate.y);
                    var candidatePanel = candidateSlot != null ? candidateSlot.equipmentPanel : null;

                    if (candidatePanel == targetPanel)
                        occupant = candidate;

                    if (source == null && candidate.item.title == dragTitle)
                    {
                        bool matches = fromIndex.HasValue
                            ? candidatePanel == inventory.equipmentPanels[fromIndex.Value]
                            : candidatePanel == null;
                        if (matches)
                            source = candidate;
                    }
                }

                if (source == null)
                    return new { success = false, error = "No '" + dragTitle + "' found at the requested place." };

                GameObject dropTarget = occupant != null && occupant != source
                    ? occupant.gameObject
                    : targetPanel.mainSlot.gameObject;

                source.OnBeginDrag(null);
                var eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                {
                    pointerCurrentRaycast = new UnityEngine.EventSystems.RaycastResult { gameObject = dropTarget }
                };
                source.OnEndDrag(eventData);
                dragResult = "dropped " + dragTitle + " on " + dropTarget.name;
            }

            // Итоговая раскладка: что в каждом слоте и где лежит каждый предмет
            var panels = new List<string>();
            for (int i = 0; i < inventory.equipmentPanels.Count; i++)
            {
                var panel = inventory.equipmentPanels[i];
                panels.Add(i + ":" + panel.allowedItemType + "=" + (panel.equipedItem != null ? panel.equipedItem.title : "-"));
            }

            var placement = new List<string>();
            int occupiedCells = 0;
            foreach (var slot in inventory.slots)
            {
                if (slot != null && !slot.free)
                    occupiedCells++;
            }
            int expectedCells = 0;
            foreach (var inventoryItem in inventory.inventoryItems)
            {
                if (inventoryItem == null || inventoryItem.item == null)
                    continue;
                expectedCells += inventoryItem.width * inventoryItem.height;
                placement.Add(inventoryItem.item.title + "@" + inventoryItem.x + "," + inventoryItem.y);
            }

            var weaponManager = UnityEngine.Object.FindObjectOfType<DarkTreeFPS.WeaponManager>();

            return new
            {
                success = true,
                added,
                addResult,
                dragResult,
                open = DTInventory.InventoryManager.showInventory,
                items = inventory.inventoryItems.Count,
                panels,
                placement,
                occupiedCells,
                expectedCells,
                activeWeapon = weaponManager != null && weaponManager.activeWeapon != null ? weaponManager.activeWeapon.weaponName : null
            };
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
