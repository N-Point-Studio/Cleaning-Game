using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Modules;


#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = nameof(ArtefactGroupData), menuName = "App/Data/Artefact Group Data")]
public class ArtefactGroupData : ScriptableObject
{
    [SerializeField] private BaseData baseData;
    public BaseData BaseData => baseData;

    // [SerializeField] private ArtefactGroupItemUI pagePrefab;
    // public ArtefactGroupItemUI PagePrefab => pagePrefab;

    [SerializeField] private ArtefactOrigin artefactOrigin;
    public ArtefactOrigin ArtefactOrigin => artefactOrigin;

    [SerializeField] private List<ArtefactData> artefactDatas = new List<ArtefactData>();
    public List<ArtefactData> ArtefactDatas => artefactDatas;

    public ArtefactData GetArtefactData(string artefactId)
    {
        return artefactDatas.FirstOrDefault(x => x.BaseData.Id == artefactId);
    }

#if UNITY_EDITOR
    [Header("Auto Setup Config")]
    [SerializeField] private string[] searchFolders;

    public void Setup()
    {
        artefactDatas.Clear();

        string[] guids = AssetDatabase.FindAssets("t:ArtefactData", searchFolders);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ArtefactData item = AssetDatabase.LoadAssetAtPath<ArtefactData>(path);

            if (item == null) continue;
            if (item.BaseData.Exclude) continue;
            if (artefactDatas.Contains(item)) continue;
            if (item.ArtefactOrigin != artefactOrigin) continue;

            artefactDatas.Add(item);
        }

        Sort();

        EditorUtility.SetDirty(this);
        AppLogger.Log($"[ArtefactGroupData] Setup '{name}' Complete. Found {artefactDatas.Count} items.");
    }

    private void Sort()
    {
        artefactDatas.Sort((a, b) => string.Compare(a.name, b.name));
    }

    private void OnValidate()
    {
        if (baseData != null) baseData.SetId(this.name);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(ArtefactGroupData))]
public class ArtefactGroupDataEditor : Editor
{
    private ArtefactGroupData script;

    private void OnEnable()
    {
        script = (ArtefactGroupData)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        EditorGUILayout.Space();
        if (GUILayout.Button("Setup Artefact Group", GUILayout.Height(30)))
        {
            script.Setup();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

