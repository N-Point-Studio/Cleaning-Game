using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            Destroy(Instance.gameObject);
        }
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshInspectPosition();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshInspectPosition();
    }

    private void RefreshInspectPosition()
    {
        if (InspectPosition != null) return;

        var foundByTag = GameObject.FindGameObjectWithTag("InspectPosition");
        if (foundByTag != null)
        {
            InspectPosition = foundByTag.transform;
            Debug.Log($"[AssembleManager] InspectPosition assigned via tag: {InspectPosition.name}");
            return;
        }

        var foundByName = GameObject.Find("InspectPosition");
        if (foundByName != null)
        {
            InspectPosition = foundByName.transform;
            Debug.Log($"[AssembleManager] InspectPosition assigned via name: {InspectPosition.name}");
        }
        else
        {
            Debug.LogWarning("[AssembleManager] InspectPosition not found in scene. Assign it via tag 'InspectPosition' or name.");
        }
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

        /// <summary>
        /// Reset all assemble state for a new gameplay session.
        /// Call this before spawning new artefact fragments.
        /// </summary>
        public void ResetForNewSession()
        {
            assemblyTargets.Clear();
            clusters.Clear();
            CurrentFragmentInspected = null;
            CurrentClusterInspected = null;
            TotalFragments = 0;
            progressAttach = 0f;
            RefreshInspectPosition();
            Debug.Log("[AssembleManager] Reset state for new session.");
        }

        /// <summary>
        /// Rebuild cluster tracking from current scene objects (call after fragments/clusters spawned).
        /// </summary>
        public void RebuildFromScene()
        {
            clusters.Clear();
            clusters.AddRange(FindObjectsOfType<ClusterStateMachine>());
            CurrentFragmentInspected = null;
            CurrentClusterInspected = null;
            Debug.Log($"[AssembleManager] Rebuilt clusters from scene. Count={clusters.Count}");
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
                    //Debug.Log("persentase naik");
                }
            }
        }

        var overallProgress = progressAttachment / TotalFragments;
        //Debug.Log($"Progress attach ({progressAttachment}/{TotalFragments}): {overallProgress}");
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
