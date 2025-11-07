using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FragmentData
{
    public Mesh mesh;
    public Material material;
}

[CreateAssetMenu(fileName = "New Artefact", menuName = "Artefact")]
public class Fragment : ScriptableObject
{
    public Mesh finalModel;
    public List<FragmentData> fragmentMeshes;
    public string artifactName;
    public string history;
}
