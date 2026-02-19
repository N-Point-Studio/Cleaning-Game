using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Modules
{
    public abstract class BaseDatabase<T> : ScriptableObject where T : ScriptableObject
    {
        public abstract T GetItem(int index);
        public abstract T GetItem(string id);
        public abstract T[] GetAllItems();
        public abstract T GetRandom();

#if UNITY_EDITOR
        [Header("Search Items")]
        [SerializeField] protected string[] searchFolders;

        protected abstract void Clear();
        protected abstract void Add(T value);
        protected abstract void Sort();

        public virtual void Setup()
        {
            Clear();

            string[] guids = AssetDatabase.FindAssets("t:scriptableobject", searchFolders);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T item = AssetDatabase.LoadAssetAtPath<T>(path);

                if (item == null)
                    continue;

                Add(item);
            }

            Sort();
        }
#endif
    }

    [System.Serializable]
    public abstract class DatabaseItemPair<T> where T : ScriptableObject
    {
        [SerializeField] protected string key;
        public string Key => key;

        [SerializeField, SerializeReference] protected T value;
        public T Value => value;

        public virtual int CompareTo(string key)
        {
            return this.key.CompareTo(key);
        }
    }
}