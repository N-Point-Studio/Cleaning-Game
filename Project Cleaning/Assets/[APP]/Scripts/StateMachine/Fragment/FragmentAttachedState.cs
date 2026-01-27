using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentAttachedState : FragmentBaseState
{
    private Transform CorrectPosition;
    private Coroutine moveRoutine;
    private float moveSpeed = 6f;
    private float rotationSpeed = 10f;

    public FragmentAttachedState(FragmentStateMachine stateMachine, Transform correctPos) : base(stateMachine)
    {
        CorrectPosition = correctPos;
        stateMachine.StartCoroutine(MoveToCorrectPosition());
    }

    public override void Enter()
    {
        stateMachine.CurrentStatus = "Attached";
        stateMachine.Interaction.isHoldAvailable = true;
        stateMachine.Interaction.isTapAvailable = false;
        stateMachine.Interaction.isDragAvailable = false;
        moveRoutine = stateMachine.StartCoroutine(MoveToCorrectPosition());

        // --- TAMBAHAN TUTORIAL ---
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CompleteStep(4); // Index 4 = Assemble
        }
        // -------------------------

        moveRoutine = stateMachine.StartCoroutine(MoveToCorrectPosition());
    }

    public override void Tick(float deltaTime)
    {
        if (stateMachine.Interaction.isHolding && !TouchManager.Instance.isInteracting)
        {
            Holding();
        }
    }

    public override void Exit()
    {
        stateMachine.Interaction.DisableAllInteraction();
    }

    private void Holding()
    {
        if (AssembleManager.Instance.CurrentClusterInspected == null) return;
        AssembleManager.Instance.CurrentClusterInspected.RemovingFragment(stateMachine);
    }

    private IEnumerator MoveToCorrectPosition()
    {
        Vector3 startPos = stateMachine.transform.localPosition;
        Quaternion startRot = stateMachine.transform.localRotation;

        Vector3 targetPos = CorrectPosition.localPosition;
        Quaternion targetRot = CorrectPosition.localRotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            stateMachine.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            stateMachine.transform.localRotation = Quaternion.Slerp(startRot, targetRot, t * rotationSpeed / moveSpeed);
            yield return null;
        }

        stateMachine.transform.localPosition = targetPos;
        stateMachine.transform.localRotation = targetRot;
        if (stateMachine.TryGetComponent(out Collider col))
            col.enabled = true;
    }
}
