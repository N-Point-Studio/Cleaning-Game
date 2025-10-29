using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;    // since you use DOTween

public class JarInsertionController : MonoBehaviour
{
    public Transform jarTarget;
    public Transform lidTransform;      // optional
    public float insertDuration = 0.5f;
    public float lidAnimDuration = 0.3f;
    [Range(0.1f, 3.0f)]
    public float snapThreshold = 1.0f; // increased for easier snapping

    private bool isInserted = false;

    public void TryInsert(Transform piece)
    {
        if (isInserted) return;

        // Debug: Check if jarTarget is assigned
        if (jarTarget == null)
        {
            Debug.LogError("JarInsertionController: jarTarget is not assigned!");
            return;
        }

        float dist = Vector3.Distance(piece.position, jarTarget.position);
        Debug.Log($"Jar distance to target: {dist:F2} (threshold: {snapThreshold})");

        if (dist < snapThreshold)
        {
            Debug.Log("Jar close enough - triggering insertion!");
            // Good: close enough → trigger insertion
            TriggerInsert(piece);
        }
        else
        {
            Debug.Log("Jar too far - returning to original position");
            // Not close enough → animate back to origin
            piece.DOMove(piece.GetComponent<JarDragController>().originalPosition, 0.3f).SetEase(Ease.OutQuad);
        }
    }

    private void TriggerInsert(Transform piece)
    {
        isInserted = true;
        Sequence seq = DOTween.Sequence();

        if (lidTransform != null)
        {
            seq.Append(lidTransform.DOLocalRotate(new Vector3(-30f, 0f, 0f), lidAnimDuration).SetEase(Ease.OutBack));
        }

        seq.Append(piece.DOMove(jarTarget.position, insertDuration).SetEase(Ease.OutCubic));
        // Force jar to be upright when inserted
        seq.Join(piece.DORotate(Vector3.zero, insertDuration).SetEase(Ease.OutCubic));
        seq.AppendInterval(0.1f);

        if (lidTransform != null)
        {
            seq.Append(lidTransform.DOLocalRotate(Vector3.zero, lidAnimDuration).SetEase(Ease.InBack));
        }

        seq.OnComplete(() =>
        {
            // Post insertion logic here
        });
    }
}
