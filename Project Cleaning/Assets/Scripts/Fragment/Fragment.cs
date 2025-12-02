// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// [System.Serializable]
// public struct FragmentData
// {
//     public Mesh mesh;
//     public Material material;
//     public Vector3 correctPosition;
// }

// [System.Serializable]
// public struct SurfaceData
// {
//     public Texture2D mask;
//     public Material mat;
//     public int type;
// }

// [CreateAssetMenu(fileName = "New Artefact", menuName = "Artefact")]
// public class Fragment : ScriptableObject
// {
//     [Header("Information")]
//     public string artifactName;
//     public string history;

//     [Header("Mesh Model")]
//     public Mesh finalModel;
//     public List<FragmentData> fragmentMeshes;

//     [Header("Clean Mask")]
//     public Texture2D mask;
//     public Material mat;
//     public int type;
// }
