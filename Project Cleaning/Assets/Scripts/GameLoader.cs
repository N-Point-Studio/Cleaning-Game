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
            if (fragData.correctPosition == null)
            {
                Debug.LogWarning("Fragment " + fragData.fragment.name + " has no correct position assigned.");
                GameObject spawned = Instantiate(fragData.fragment);
            }
            else
            {
                Debug.Log("Spawning Fragment: " + fragData.fragment.name);
                GameObject spawned = Instantiate(fragData.fragment);

                FragmentStateMachine fragmentSM = spawned.GetComponent<FragmentStateMachine>();

                AssemblyTarget newTarget = new AssemblyTarget(fragmentSM, fragData.correctPosition);

                AssembleManager.Instance.assemblyTargets.Add(newTarget);
            }
        }

        AssembleManager.Instance.ShowingAssembleProgress();
    }
}
