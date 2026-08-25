using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
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

                case "SaveScene":
                    return SaveScene();

                case "RefreshAssetDatabase":
                    AssetDatabase.Refresh();
                    return new { success = true, message = "Asset Database refreshed" };

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

        private static object GetConsoleLogs(JObject args)
        {
            int count = args?["count"]?.ToObject<int>() ?? 50;
            string filter = args?["filter"]?.ToString()?.ToLower() ?? "all";

            // Unity не предоставляет прямого API для Console логов
            // Нужно использовать рефлексию или собственную систему логирования

            return new
            {
                message = "Console logs API is not directly accessible. Consider implementing custom logging system.",
                count = 0,
                logs = new List<object>()
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
