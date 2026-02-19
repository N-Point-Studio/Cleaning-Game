using UnityEngine;
using UnityEditor;

namespace Modules
{
    public class BaseDatabaseEditor<T1, T2> : Editor where T1 : BaseDatabase<T2> where T2 : ScriptableObject
    {
        protected T1 script;
        protected SerializedProperty items;
        protected SerializedProperty searchFolders;

        protected virtual void OnEnable()
        {
            script = (T1)target;
            items = serializedObject.FindProperty(nameof(items));
            searchFolders = serializedObject.FindProperty(nameof(searchFolders));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(items, true);
            EditorGUILayout.PropertyField(searchFolders, true);

            if (GUILayout.Button("Setup"))
            {
                script.Setup();
                EditorUtility.SetDirty(script);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}