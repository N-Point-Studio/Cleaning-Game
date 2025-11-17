// using System.Collections.Generic;
// using UnityEngine;

// public class FragmentCluster : MonoBehaviour
// {
//     [Header("Cluster Info")]
//     public List<FragmentStateMachine> fragments = new List<FragmentStateMachine>();
//     public Vector3 initialPosition;
//     public Quaternion initialRotation;

//     private void Awake()
//     {
//         // Simpan posisi awal cluster
//         initialPosition = transform.position;
//         initialRotation = transform.rotation;
//     }

//     public void AddFragment(FragmentStateMachine frag)
//     {
//         if (!fragments.Contains(frag))
//         {
//             fragments.Add(frag);
//             frag.clusterRoot = transform;
//             frag.transform.SetParent(transform);
//         }
//     }

//     public void RemoveFragment(FragmentStateMachine frag)
//     {
//         if (fragments.Contains(frag))
//         {
//             fragments.Remove(frag);
//             frag.clusterRoot = null;
//             frag.transform.SetParent(null);
//         }

//         // // Kalau cluster tinggal 1, lepaskan juga dan hancurkan cluster
//         // if (fragments.Count <= 1)
//         // {
//         //     if (fragments.Count == 1)
//         //     {
//         //         fragments[0].clusterRoot = null;
//         //         fragments[0].transform.SetParent(null);
//         //     }

//         //     Destroy(gameObject);
//         // }
//     }

//     public void ReturnToInitial(float moveSpeed = 6f)
//     {
//         transform.position = Vector3.Lerp(transform.position, initialPosition, Time.deltaTime * moveSpeed);
//         transform.rotation = Quaternion.Lerp(transform.rotation, initialRotation, Time.deltaTime * moveSpeed);
//     }

//     public bool Contains(FragmentStateMachine frag)
//     {
//         return fragments.Contains(frag);
//     }
// }
