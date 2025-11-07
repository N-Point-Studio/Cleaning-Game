using UnityEngine;
using System.Collections;

public class FragmentController : MonoBehaviour
{
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    private Coroutine moveRoutine;

    void Awake()
    {
        originalParent = transform.parent;
    }

    public void SaveInitialTransform()
    {
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
    }

    public void SetMoveStateInspect()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveToInspect());
    }

    public void SetMoveStateReset()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveBackToSlot());
    }

    IEnumerator MoveToInspect()
    {
        Transform inspectCenter = AssembleManager.Instance.inspectCenter;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = inspectCenter.position;
        Quaternion endRot = inspectCenter.rotation;

        float t = 0f;
        float duration = 0.35f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.SetParent(inspectCenter, true);
    }

    IEnumerator MoveBackToSlot()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = originalParent.TransformPoint(originalLocalPos);
        Quaternion endRot = originalParent.rotation * originalLocalRot;

        float t = 0f;
        float duration = 0.35f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.SetParent(originalParent);
        transform.localPosition = originalLocalPos;
        transform.localRotation = originalLocalRot;
    }
}
