using UnityEngine;
using DG.Tweening;

public partial class JarAutoAssembly : MonoBehaviour
{
    public static void DisassembleLastPiece()
    {
        if (assemblyOrder.Count <= 1)
        {
            Debug.Log("Hanya tersisa satu bagian, tidak bisa dibongkar lebih lanjut dengan cara ini.");
            return;
        }

        JarAutoAssembly pieceToDisassemble = assemblyOrder[assemblyOrder.Count - 1];
        assemblyOrder.RemoveAt(assemblyOrder.Count - 1);

        Debug.Log($"Membongkar bagian: {pieceToDisassemble.pieceType}. Sisa dalam urutan: {assemblyOrder.Count}");

        if (pieceToDisassemble != null)
        {
            pieceToDisassemble.InitiateDisassembly();
        }

        // Jika setelah pembongkaran hanya tersisa satu bagian, otomatis masuk ke mode inspeksi.
        if (assemblyOrder.Count == 1)
        {
            JarAutoAssembly lastRemainingPiece = assemblyOrder[0];
            if (lastRemainingPiece != null)
            {
                Debug.Log($"Hanya satu bagian tersisa ({lastRemainingPiece.pieceType}), otomatis inspeksi.");

                // Tandai sebagai tidak terpasang
                lastRemainingPiece.isAssembled = false;

                // Panggil inspeksi otomatis
                lastRemainingPiece.TryInspection();
            }
        }
    }

    public void InitiateDisassembly()
    {
        Debug.Log($"♻️ Reassembly triggered for {pieceType} - returning to original position");

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && closeUpManager.HasObjectInCloseUp)
        {
            Transform currentObject = closeUpManager.CurrentCloseUpObject;
            if (currentObject == transform ||
                (JarAutoAssembly.currentPartialAssemblyParent != null && currentObject == JarAutoAssembly.currentPartialAssemblyParent.transform) ||
                (jarFullyAssembled && currentObject == assembledJarRoot))
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
            // NEW: Detach one piece instead of resetting all
            DetachSinglePieceFromAssembly();
        }
        else if (assembledJarCollider != null)
        {
            assembledJarCollider.enabled = false;
        }

        // This piece is now unassembled
        isAssembled = false;
        isDragging = false;
        isHoldingForDrag = false;

        transform.DOKill();

        // Animate the piece back to its original position
        PlayReassemblyReturnAnimation();

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

    /// <summary>
    /// Detaches a single piece from a fully assembled jar.
    /// </summary>
    void DetachSinglePieceFromAssembly()
    {
        Debug.Log($"🔧 Detaching {pieceType} from fully assembled jar.");

        // Jar is no longer fully assembled
        jarFullyAssembled = false;

        // Stop the assembled jar from rotating
        ResetJarCompletionState();

        // Re-enable interaction for the other pieces
        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece == null || piece == this)
                continue;

            // Re-enable the inspectable component for the other pieces
            if (piece.inspectableComponent != null && piece.allowReassemblyAfterCompletion)
            {
                piece.inspectableComponent.SetInspectable(piece.initialCanBeInspected);
            }

            // Ensure the piece's script is enabled
            if (!piece.enabled)
            {
                piece.enabled = true;
            }
            
            // Ensure piece colliders are on so they can be selected
            if (piece.pieceCollider != null)
            {
                piece.pieceCollider.enabled = true;
            }
        }
        
        // This piece is no longer assembled
        isAssembled = false;
        
        // Move this piece out of the assembledJarRoot hierarchy
        transform.SetParent(originalParent, true);
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
        // The animation was in world space. We just need to snap the final world values.
        // The parent was already set correctly at the start of the animation.
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;
        targetPosition = originalPosition;
    }
}
