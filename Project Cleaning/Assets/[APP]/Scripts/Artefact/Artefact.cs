using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ArtefactFragmentData
{
    public GameObject fragment;
    public Transform correctPosition;
}

[CreateAssetMenu(fileName = "Artefact Data", menuName = "Artefact")]
public class Artefact : ScriptableObject
{
    [Header("Information")]
    public string artifactName;
    public string code;
    public string history;
    public List<ArtefactFragmentData> artefacts;
    public Vector3 finishRotation;
    public Vector3 finishPosition;
    public bool isSmallSized;
}
