using System.Collections.Generic;
using UnityEngine;

public class GameLoader : MonoBehaviour
{
    [Header("Artefact To Load")]
    public Artefact artefactData;

    private void Start()
    {
        LoadArtefact();
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
            GameObject spawned = Instantiate(fragData.fragment);

            FragmentStateMachine fragmentSM = spawned.GetComponent<FragmentStateMachine>();

            AssemblyTarget newTarget = new AssemblyTarget
            {
                targetFragment = fragmentSM,
                correctPosition = fragData.correctPosition
            };

            AssembleManager.Instance.assemblyTargets.Add(newTarget);
        }
    }
}
