using System.Collections.Generic;
using UnityEngine;

public class FragmentsManager : MonoBehaviour
{
    public static FragmentsManager Instance { get; private set; }

    [Header("All Fragments in Scene")]
    public List<FragmentStateMachine> allFragments = new List<FragmentStateMachine>();

    [Header("Progress")]
    [Range(0f, 100f)]
    public float progressPercentage = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (allFragments.Count == 0)
        {
            FragmentStateMachine[] fragments = FindObjectsOfType<FragmentStateMachine>();
            allFragments.AddRange(fragments);
        }

        // UpdateProgress();
    }

    public void AddFragment(FragmentStateMachine frag)
    {
        if (!allFragments.Contains(frag))
        {
            allFragments.Add(frag);
        }

        // UpdateProgress();
    }

    public void RemoveFragment(FragmentStateMachine frag)
    {
        if (allFragments.Contains(frag))
        {
            allFragments.Remove(frag);
        }

        // UpdateProgress();
    }

    public bool IsContained(FragmentStateMachine frag)
    {
        return allFragments.Contains(frag);
    }

    // public void UpdateProgress()
    // {
    //     int total = allFragments.Count;
    //     if (total == 0)
    //     {
    //         progressPercentage = 0f;
    //         return;
    //     }

    //     int assembledCount = 0;

    //     foreach (var frag in allFragments)
    //     {
    //         if (frag.clusterRoot != null)
    //         {
    //             assembledCount++;
    //         }
    //     }

    //     progressPercentage = (float)assembledCount / total * 100f;
    //     Debug.Log("Progress assemble: " + progressPercentage);
    // }
}
