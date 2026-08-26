using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace DarkTreeFPS.EditorTools
{
    /// <summary>
    /// Утилита для автоматического исправления Convex MeshColliders на статичных объектах
    /// </summary>
    public class FixRockColliders : EditorWindow
    {
        private List<GameObject> foundObjects = new List<GameObject>();
        private Vector2 scrollPosition;

        [MenuItem("Tools/Fix Rock Colliders")]
        public static void ShowWindow()
        {
            var window = GetWindow<FixRockColliders>("Fix Rock Colliders");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Fix Convex MeshColliders on Rocks", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Этот инструмент найдёт все объекты Rock с Convex MeshCollider и отключит Convex.\n" +
                "Это решит проблему 'polygon limit (256)'.",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Найти объекты с проблемой", GUILayout.Height(40)))
            {
                FindProblematicObjects();
            }

            GUILayout.Space(10);

            if (foundObjects.Count > 0)
            {
                EditorGUILayout.LabelField($"Найдено объектов: {foundObjects.Count}", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                foreach (var obj in foundObjects)
                {
                    if (obj != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndScrollView();

                GUILayout.Space(10);

                if (GUILayout.Button("Исправить все найденные объекты", GUILayout.Height(40)))
                {
                    FixAllObjects();
                }
            }
        }

        private void FindProblematicObjects()
        {
            foundObjects.Clear();

            // Поиск в сцене
            MeshCollider[] colliders = FindObjectsOfType<MeshCollider>();
            
            foreach (var collider in colliders)
            {
                // Проверяем, содержит ли имя объекта "Rock" и включен ли Convex
                if (collider.gameObject.name.Contains("Rock") && collider.convex)
                {
                    foundObjects.Add(collider.gameObject);
                }
            }

            // Поиск в префабах
            string[] guids = AssetDatabase.FindAssets("t:Prefab Rock");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    MeshCollider[] prefabColliders = prefab.GetComponentsInChildren<MeshCollider>(true);
                    foreach (var collider in prefabColliders)
                    {
                        if (collider.convex && !foundObjects.Contains(prefab))
                        {
                            foundObjects.Add(prefab);
                        }
                    }
                }
            }

            Debug.Log($"[FixRockColliders] Найдено {foundObjects.Count} объектов с Convex MeshCollider");
        }

        private void FixAllObjects()
        {
            if (foundObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("Ошибка", "Нет объектов для исправления", "OK");
                return;
            }

            int fixedCount = 0;
            Undo.RecordObjects(foundObjects.ToArray(), "Fix Rock Colliders");

            foreach (var obj in foundObjects)
            {
                if (obj != null)
                {
                    MeshCollider[] colliders = obj.GetComponentsInChildren<MeshCollider>(true);
                    
                    foreach (var collider in colliders)
                    {
                        if (collider.convex)
                        {
                            collider.convex = false;
                            
                            // Если это префаб, пометить как изменённый
                            if (PrefabUtility.IsPartOfPrefabAsset(obj))
                            {
                                EditorUtility.SetDirty(obj);
                            }
                            
                            fixedCount++;
                            Debug.Log($"[FixRockColliders] Отключен Convex на: {obj.name} -> {collider.gameObject.name}");
                        }
                    }
                }
            }

            // Сохранить изменения в префабах
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Готово!", 
                $"Исправлено {fixedCount} MeshCollider(s) на {foundObjects.Count} объекте(ах).\n\nConvex отключен, теперь используются точные mesh colliders.", 
                "OK"
            );

            foundObjects.Clear();
        }
    }
}
