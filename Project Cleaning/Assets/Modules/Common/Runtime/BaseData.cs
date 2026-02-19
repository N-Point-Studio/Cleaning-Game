using Modules;
using UnityEngine;

[System.Serializable]
public class BaseData
{
    [SerializeField, ReadOnly] protected string id;
    public string Id => id;

    [SerializeField] protected string itemName;
    public string ItemName => itemName;

    [SerializeField, TextArea] protected string itemDescription;
    public string ItemDescription => itemDescription;

    [SerializeField] protected Sprite itemIcon;
    public Sprite ItemIcon => itemIcon;

    [SerializeField] protected bool exclude;
    public bool Exclude => exclude;

    public void SetId(string newId)
    {
        id = newId;
    }
}