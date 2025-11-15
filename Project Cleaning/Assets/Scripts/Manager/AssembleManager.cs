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
    public List<ClusterStateMachine> clusters = new List<ClusterStateMachine>();

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

    private void LateUpdate()
    {
        clusters.RemoveAll(item => item == null);
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

    public void RegisterCluster(ClusterStateMachine cluster, bool isRegister)
    {
        if (!clusters.Contains(cluster))
        {
            if (isRegister)
            {
                clusters.Add(cluster);
            }
            else
            {
                clusters.Remove(cluster);
            }
        }
    }
}
