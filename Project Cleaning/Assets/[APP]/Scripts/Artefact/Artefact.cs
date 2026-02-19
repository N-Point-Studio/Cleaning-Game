
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(ArtefactData), menuName = "App/Data/Artefact Data")]
public class ArtefactData : ScriptableObject
{
    [SerializeField] private BaseData baseData;
    public BaseData BaseData => baseData;

    [SerializeField] private Sprite completedIcon;
    public Sprite CompletedIcon => completedIcon;

    [SerializeField] private bool unlockedByDefault;
    public bool UnlockedByDefault => unlockedByDefault;

    [SerializeField] private ArtefactOrigin artefactOrigin;
    public ArtefactOrigin ArtefactOrigin => artefactOrigin;

    // [SerializeField] private ToolType allowedToolTypes;
    // public ToolType AllowedToolTypes => allowedToolTypes;

    [SerializeField] private List<ArtefactFragmentData> artefactFragmentDatas;
    public List<ArtefactFragmentData> ArtefactFragmentDatas => artefactFragmentDatas;

    [SerializeField] private CustomTransform finishTransform;
    public CustomTransform FinishTransform => finishTransform;

    private string artefactGroupId;
    public string ArtefactGroupId => artefactGroupId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        baseData.SetId(this.name);
    }
#endif
}

[System.Serializable]
public struct ArtefactFragmentData
{
    [SerializeField] private GameObject prefab;
    public GameObject Prefab => prefab;

    [SerializeField] private CustomTransform correctTransform;
    public CustomTransform CorrectTransform => correctTransform;
}

[System.Serializable]
public struct CustomTransform
{
    [SerializeField] private Vector3 rotation;
    public Vector3 Rotation => rotation;

    [SerializeField] private Vector3 position;
    public Vector3 Position => position;
}

public enum ArtefactOrigin
{
    China,
    Indonesia,
    Egypt
}