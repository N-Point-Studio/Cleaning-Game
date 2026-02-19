using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Modules;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = nameof(ArtefactDatabase), menuName = "App/Database/Artefact Database")]
public class ArtefactDatabase : BaseDatabase<ArtefactGroupData>
{
    [System.Serializable]
    public class ArtefactGroupPair : DatabaseItemPair<ArtefactGroupData>
    {
        public ArtefactGroupPair(string key, ArtefactGroupData value)
        {
            this.key = key;
            this.value = value;
        }
    }

    [Header("Artefacts")]
    [SerializeField] private List<ArtefactGroupPair> items = new List<ArtefactGroupPair>();

    private readonly Dictionary<string, ArtefactGroupData> lookup = new Dictionary<string, ArtefactGroupData>();

#if UNITY_EDITOR
    public override void Setup()
    {
        Clear();

        string[] guids = AssetDatabase.FindAssets("t:scriptableobject", searchFolders);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ArtefactGroupData item = AssetDatabase.LoadAssetAtPath<ArtefactGroupData>(path);

            if (item == null || item.BaseData.Exclude)
                continue;

            Add(item);
        }

        Sort();
    }
#endif

    public override ArtefactGroupData[] GetAllItems()
    {
        ArtefactGroupData[] arr = new ArtefactGroupData[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            arr[i] = items[i]?.Value;
        }
        return arr;
    }

    public override ArtefactGroupData GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;
        return items[index]?.Value;
    }

    public override ArtefactGroupData GetItem(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        EnsureIndex();
        if (lookup.TryGetValue(id, out var val))
            return val;

        return null;
    }

    public override ArtefactGroupData GetRandom()
    {
        if (items == null || items.Count == 0)
            return null;
        int idx = UnityEngine.Random.Range(0, items.Count);
        return items[idx]?.Value;
    }

    public ArtefactGroupData GetArtefactGroup(string groupId)
    {
        return GetItem(groupId);
    }

    public ArtefactData GetArtefactData(string artefactId)
    {
        foreach (var group in GetAllItems())
        {
            if (group == null) continue;

            var item = group.ArtefactDatas.FirstOrDefault(x => x.BaseData.Id == artefactId);
            if (item != null)
                return item;
        }
        return null;
    }

    public List<ArtefactData> GetAllArtefactDatas()
    {
        var allDatas = new List<ArtefactData>();
        foreach (var group in GetAllItems())
        {
            if (group != null && group.ArtefactDatas != null)
            {
                allDatas.AddRange(group.ArtefactDatas);
            }
        }
        return allDatas;
    }

    public List<ArtefactGroupData> GetArtefactGroupsByAge(ArtefactOrigin artefactAge)
    {
        return GetAllItems().Where(a => a.ArtefactOrigin == artefactAge).ToList();
    }

#if UNITY_EDITOR
    protected override void Add(ArtefactGroupData value)
    {
        if (value == null)
            return;

        var key = GetKey(value);
        if (string.IsNullOrEmpty(key))
            return;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].Key == key)
                return;
        }

        items.Add(new ArtefactGroupPair(key, value));
        if (!lookup.ContainsKey(key))
            lookup.Add(key, value);
    }

    protected override void Clear()
    {
        items.Clear();
        lookup.Clear();
    }

    protected override void Sort()
    {
        items.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return string.Compare(a.Key, b.Key, System.StringComparison.Ordinal);
        });

        RebuildIndex();
    }
#endif

    private static string GetKey(ArtefactGroupData data)
    {
        return data != null ? data.BaseData.Id : null;
    }

    private void EnsureIndex()
    {
        if (lookup.Count == items.Count)
            return;
        RebuildIndex();
    }

    private void RebuildIndex()
    {
        lookup.Clear();
        for (int i = 0; i < items.Count; i++)
        {
            var p = items[i];
            if (p == null || p.Value == null)
                continue;
            var key = p.Key;
            if (string.IsNullOrEmpty(key))
                key = GetKey(p.Value);
            if (string.IsNullOrEmpty(key))
                continue;
            if (!lookup.ContainsKey(key))
                lookup.Add(key, p.Value);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ArtefactDatabase))]
public class ArtefactDatabaseEditor : Editor
{
    private ArtefactDatabase script;

    private void OnEnable()
    {
        script = (ArtefactDatabase)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        EditorGUILayout.Space();
        if (GUILayout.Button("Setup Database"))
        {
            script.Setup();
            EditorUtility.SetDirty(script);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif