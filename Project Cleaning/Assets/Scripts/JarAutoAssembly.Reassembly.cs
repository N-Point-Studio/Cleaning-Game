using UnityEngine;
using DG.Tweening;

public partial class JarAutoAssembly : MonoBehaviour
{
    void TriggerReassemblyFromHold()
    {
        Debug.Log($"♻️ Reassembly triggered for {pieceType} - returning to original position");

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
        {
            Transform currentObject = closeUpManager.CurrentCloseUpObject;
            if (currentObject == transform ||
                (currentPartialAssemblyParent != null && currentObject == currentPartialAssemblyParent.transform))
            {
                closeUpManager.PauseRotation();
                closeUpManager.AnimationHandler?.StopCurrentAnimation();
                closeUpManager.ClearCurrentObject();
            }
            else
            {
                closeUpManager.PauseRotation();
            }
        }

        CleanupTemporaryAssemblyParent();

        bool wasFullyAssembled = jarFullyAssembled;

        if (wasFullyAssembled)
        {
            jarFullyAssembled = false;
            ResetJarCompletionState();
        }
        else if (assembledJarCollider != null)
        {
            assembledJarCollider.enabled = false;
        }

        isAssembled = false;
        isDragging = false;

        transform.DOKill();

        if (!wasFullyAssembled)
        {
            PlayReassemblyReturnAnimation();
        }

        inspectionOffset = Vector3.zero;
        inspectionRotationOffset = Quaternion.identity;
        localOffsetsComputed = false;

        if (inspectableComponent != null)
        {
            inspectableComponent.SetInspectable(initialCanBeInspected);
            if (inspectableComponent.IsBeingInspected())
            {
                inspectableComponent.OnInspectionEnd();
            }
        }

        enabled = true;
        reassemblyHoldTriggered = false;

    }

    void ResetJarCompletionState()
    {
        if (assembledJarCollider != null)
        {
            assembledJarCollider.enabled = false;
        }

        SmoothObjectRotator rotator = FindObjectOfType<SmoothObjectRotator>();
        rotator?.StopRotating();

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.CurrentCloseUpObject == assembledJarRoot)
        {
            closeUpManager.ExitCloseUp(true);
        }

        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece == null)
                continue;

            piece.transform.DOKill();

            if (piece.inspectableComponent != null && piece.allowReassemblyAfterCompletion)
            {
                piece.inspectableComponent.SetInspectable(piece.initialCanBeInspected);
            }

            if (!piece.enabled)
            {
                piece.enabled = true;
            }

            piece.awaitingReassemblyDecision = false;
            piece.reassemblyHoldTriggered = false;
            piece.isHoldingForDrag = false;
            piece.isDragging = false;
            piece.isAssembled = false;
            piece.targetPosition = piece.originalPosition;
            piece.inspectionOffset = Vector3.zero;
            piece.inspectionRotationOffset = Quaternion.identity;
            piece.localOffsetsComputed = false;

            piece.PlayReassemblyReturnAnimation();
        }
    }

    void PlayReassemblyReturnAnimation()
    {
        transform.DOKill();

        // Preserve current world transform while restoring hierarchy
        transform.SetParent(originalParent, true);
        transform.localScale = originalScale;

        if (pieceCollider != null)
        {
            pieceCollider.enabled = false;
        }

        float duration = Mathf.Max(0f, reassemblyReturnDuration);
        targetPosition = originalPosition;

        if (duration <= 0f)
        {
            SnapBackToOriginalTransform();

            if (pieceCollider != null)
            {
                pieceCollider.enabled = true;
            }

            return;
        }

        Sequence returnSequence = DOTween.Sequence();
        returnSequence.SetLink(gameObject);

        returnSequence.Append(transform.DOMove(originalPosition, duration).SetEase(reassemblyEase));
        returnSequence.Join(transform.DORotateQuaternion(originalRotation, duration).SetEase(reassemblyEase));
        returnSequence.OnComplete(() =>
        {
            SnapBackToOriginalTransform();

            if (pieceCollider != null)
            {
                pieceCollider.enabled = true;
            }
        });

        returnSequence.Play();
    }

    void SnapBackToOriginalTransform()
    {
        transform.SetParent(originalParent, false);
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        transform.localScale = originalScale;
        targetPosition = originalPosition;
    }
}
