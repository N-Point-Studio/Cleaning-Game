using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Modules
{
    public static class FPSDisplayEditor
    {
        [MenuItem("Tools/App/FPS Display", false, 1)]
        private static void CreateObject()
        {
            var obj = new GameObject(nameof(FPSDisplay)).AddComponent<FPSDisplay>();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.SetActiveObjectWithContext(obj, obj);
        }
    }
}