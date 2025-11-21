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
    public int TotalFragments = 0;

    [Header("Progress")]
    [Range(0, 1f)]
    [SerializeField] private float progressAttach = 0f;

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

    private void Update()
    {
        AssembleProgress();
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

    private void AssembleProgress()
    {
        float progressAttachment = 0;
        foreach (var cluster in clusters)
        {
            if (cluster == null) continue;

            FragmentStateMachine[] fragments = cluster.GetComponentsInChildren<FragmentStateMachine>();

            if (fragments.Length <= 1) continue;

            foreach (var fragment in fragments)
            {
                if (fragment == null) continue;

                if (fragment.CurrentStatus == "Attached")
                {
                    progressAttachment += 1;
                    Debug.Log("persentase naik");
                }
            }
        }

        var overallProgress = progressAttachment / TotalFragments;
        Debug.Log($"Progress attach ({progressAttachment}/{TotalFragments}): {overallProgress}");
        progressAttach = overallProgress;
    }

    public float GetAttachProgress()
    {
        return progressAttach;
    }

    public void ShowingAssembleProgress()
    {
        Debug.Log("Showing Assemble Progress " + assemblyTargets.Count);
        TotalFragments = assemblyTargets.Count;
        if (assemblyTargets.Count == 0)
        {
            UIManager.Instance.ShowProgress(UIManager.ProgressType.Assemble, false);
        }
    }
}
