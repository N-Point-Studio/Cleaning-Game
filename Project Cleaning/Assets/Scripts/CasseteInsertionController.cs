using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;    // since you use DOTween

public class CasseteInsertionController : MonoBehaviour
{
    public Transform cassetteTarget;
    public Transform lidTransform;      // optional
    public float insertDuration = 0.5f;
    public float lidAnimDuration = 0.3f;
    public float snapThreshold = 0.2f; // adjust as needed

    private bool isInserted = false;

    public void TryInsert(Transform piece)
    {
        if (isInserted) return;

        float dist = Vector3.Distance(piece.position, cassetteTarget.position);
        if (dist < snapThreshold)
        {
            // Good: close enough → trigger insertion
            TriggerInsert(piece);
        }
        else
        {
            // Not close enough → animate back to origin
            piece.DOMove(piece.GetComponent<CassetteDrag>().originalPosition, 0.3f).SetEase(Ease.OutQuad);
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

        seq.Append(piece.DOMove(cassetteTarget.position, insertDuration).SetEase(Ease.OutCubic));
        seq.Join(piece.DORotate(cassetteTarget.rotation.eulerAngles, insertDuration).SetEase(Ease.OutCubic));
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
