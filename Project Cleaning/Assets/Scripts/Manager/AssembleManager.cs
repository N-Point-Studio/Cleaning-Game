using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssembleManager : MonoBehaviour
{
    public static AssembleManager Instance { get; private set; }

    public Transform InspectPosition;
    public FragmentStateMachine CurrentFragmentInspected;
    public ClusterStateMachine CurrentClusterInspected;
    public List<AssemblyTarget> assemblyTargets = new();

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

    public bool IsInspectingAvailable()
    {
        return CurrentFragmentInspected == null && CurrentClusterInspected == null;
    }

    public void SetCurrentInspectFragment(FragmentStateMachine stateMachine)
    {
        CurrentFragmentInspected = stateMachine;
    }

    public void SetCurrentInspectCluster(ClusterStateMachine stateMachine)
    {
        CurrentClusterInspected = stateMachine;
    }

    public bool TryGetAssemblePosition(FragmentStateMachine other, out Transform correctPos)
    {
        foreach (var target in assemblyTargets)
        {
            if (target.targetFragment == other)
            {
                correctPos = target.correctPosition;
                return true;
            }
        }
        correctPos = null;
        return false;
    }
}
