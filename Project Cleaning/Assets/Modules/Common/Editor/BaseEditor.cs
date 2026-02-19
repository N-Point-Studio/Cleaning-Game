using UnityEditor;
using UnityEngine;

namespace Modules
{
    public abstract class BaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnDrawCustomInspector();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnDrawCustomInspector()
        {

        }

        protected virtual bool DrawToggle(string label, bool state)
        {
            return EditorGUILayout.Toggle(label, state);
        }

        protected virtual bool DrawPropertyField(string fieldName)
        {
            return EditorGUILayout.PropertyField(serializedObject.FindProperty(fieldName), true);
        }

        protected virtual bool DrawButton(string buttonName)
        {
            return GUILayout.Button(buttonName);
        }
    }
}