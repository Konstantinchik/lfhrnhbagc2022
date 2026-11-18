#if UNITY_EDITOR
using DarkTreeFPS.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class AttachRadioactiveZoneTestToDummy
{
    private const string TargetName = "Dummy";
    private static bool attached;

    static AttachRadioactiveZoneTestToDummy()
    {
        EditorApplication.delayCall += TryAttach;
        EditorApplication.hierarchyChanged += TryAttach;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += TryAttach;
    }

    private static void TryAttach()
    {
        if (attached || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
            return;

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        var dummy = FindInRoots(scene.GetRootGameObjects());
        if (dummy == null)
            return;

        attached = true;
        bool sceneChanged = false;
        if (dummy.GetComponent<RadioactiveZoneDebugTest>() == null)
        {
            Undo.AddComponent<RadioactiveZoneDebugTest>(dummy);
            sceneChanged = true;
        }

        if (dummy.GetComponent<AnomalyZoneDebugTest>() == null)
        {
            Undo.AddComponent<AnomalyZoneDebugTest>(dummy);
            sceneChanged = true;
        }

        if (sceneChanged)
            EditorSceneManager.MarkSceneDirty(scene);

        Selection.activeGameObject = dummy;
        Debug.Log("[Gameplay tests] Attached persistent test harnesses to active scene GameObject 'Dummy'. The scene has not been saved.", dummy);
        EditorApplication.hierarchyChanged -= TryAttach;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
    }

    private static GameObject FindInRoots(GameObject[] roots)
    {
        foreach (var root in roots)
        {
            if (root.name == TargetName)
                return root;

            var nested = FindInChildren(root.transform);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static GameObject FindInChildren(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == TargetName)
                return child.gameObject;

            var nested = FindInChildren(child);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
#endif
