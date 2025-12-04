using System.Collections.Generic;
using UnityEngine;

public class GameLoader : MonoBehaviour
{
    [Header("Artefact To Load")]
    public Artefact artefactData;

    [System.Serializable]
    public struct ArtefactByObjectType
    {
        public ObjectType objectType;
        public Artefact artefact;
    }

    [Header("Auto Select From ObjectType")]
    [SerializeField] private bool autoSelectByObjectType = true;
    [SerializeField] private ArtefactByObjectType[] artefactMappings;
    [SerializeField] private Artefact fallbackArtefact;
    private readonly List<GameObject> spawnedFragments = new List<GameObject>();

    private void Start()
    {
        StartCoroutine(InitializeSession());
    }

    private System.Collections.IEnumerator InitializeSession()
    {
        // Debug incoming context so we can spot why a default Coin might be used.
        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log($"[GameLoader] STM present. Incoming ObjectType: {SceneTransitionManager.Instance.GetCurrentObjectType()}, Chapter: {SceneTransitionManager.Instance.GetCurrentChapterType()}");
        }
        else
        {
            Debug.LogWarning("[GameLoader] SceneTransitionManager missing on load - auto-select may fallback to default artefact.");
        }

        // Auto-pick artefact based on the ObjectType coming from menu (via SceneTransitionManager)
        if (autoSelectByObjectType && SceneTransitionManager.Instance != null)
        {
            ObjectType targetType = SceneTransitionManager.Instance.GetCurrentObjectType();
            var mapped = GetArtefactForType(targetType);

            if (mapped != null)
            {
                artefactData = mapped;
                Debug.Log($"[GameLoader] Auto-selected artefact for {targetType}: {artefactData.name}");
            }
            else if (fallbackArtefact != null)
            {
                artefactData = fallbackArtefact;
                Debug.LogWarning($"[GameLoader] No mapping found for {targetType}. Using fallback artefact: {fallbackArtefact.name}");
            }
            else
            {
                Debug.LogWarning($"[GameLoader] No mapping found for {targetType}. Using pre-assigned artefact: {(artefactData != null ? artefactData.name : "NULL")}");
            }
        }

        // Pastikan seluruh manager tersedia sebelum reset (mencegah progress UI tertinggal)
        yield return new WaitUntil(() =>
            UIManager.Instance != null &&
            CleanManager.Instance != null &&
            AssembleManager.Instance != null &&
            GamePlayManager.Instance != null &&
            TouchManager.Instance != null
        );

        // Fresh session reset so assemble state doesn't carry over between runs
        AssembleManager.Instance?.ResetForNewSession();
        CleanManager.Instance?.ResetForNewSession();
        ResetUIProgress();
        CleanupPreviousFragments();

        // Reset gameplay session state
        GamePlayManager.Instance?.ResetSession();

        // Spawn artefact fragments
        LoadArtefact();

        // Wait a frame so spawned objects can run Awake/Start
        yield return null;

        // Rebuild tracking after all fragments register to avoid empty progress that instantly finishes
        CleanManager.Instance?.RebuildFromScene();
        AssembleManager.Instance?.RebuildFromScene();

        Debug.Log($"[GameLoader] Spawned fragments: {spawnedFragments.Count}, assembly targets: {AssembleManager.Instance?.assemblyTargets.Count}");
    }

    public void LoadArtefact()
    {
        if (artefactData == null)
        {
            Debug.LogError("No Artefact assigned to GameLoader!");
            return;
        }

        AssembleManager.Instance.assemblyTargets.Clear();
        foreach (var fragData in artefactData.artefacts)
        {
            if (fragData.correctPosition == null)
            {
                Debug.LogWarning("Fragment " + fragData.fragment.name + " has no correct position assigned.");
                GameObject spawned = Instantiate(fragData.fragment);
                spawnedFragments.Add(spawned);
            }
            else
            {
                Debug.Log("Spawning Fragment: " + fragData.fragment.name);
                GameObject spawned = Instantiate(fragData.fragment);
                spawnedFragments.Add(spawned);

                FragmentStateMachine fragmentSM = spawned.GetComponent<FragmentStateMachine>();

                AssemblyTarget newTarget = new AssemblyTarget(fragmentSM, fragData.correctPosition);

                AssembleManager.Instance.assemblyTargets.Add(newTarget);
            }
        }

        UIManager.Instance.SetArtefactName(artefactData.artifactName);

        Vector3 eulerAngles = artefactData.finishRotation;
        Vector3 position = artefactData.finishPosition;

        Quaternion rotation = Quaternion.Euler(eulerAngles);
        GamePlayManager.Instance.SetFinishedRotationAndRotation(rotation, position);

        AssembleManager.Instance.ShowingAssembleProgress();
    }

    private Artefact GetArtefactForType(ObjectType type)
    {
        foreach (var mapping in artefactMappings)
        {
            if (mapping.objectType == type)
            {
                return mapping.artefact;
            }
        }
        return null;
    }

    private void CleanupPreviousFragments()
    {
        for (int i = spawnedFragments.Count - 1; i >= 0; i--)
        {
            if (spawnedFragments[i] != null)
            {
                Destroy(spawnedFragments[i]);
            }
        }
        spawnedFragments.Clear();
    }

    private void ResetUIProgress()
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.ResetProgressBars();
    }
}
