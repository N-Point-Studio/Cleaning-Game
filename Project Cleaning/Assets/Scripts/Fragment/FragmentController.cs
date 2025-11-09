using UnityEngine;
using System.Collections;

[RequireComponent(typeof(InspectableJar))]
public class FragmentController : MonoBehaviour
{
    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;

    private Coroutine moveRoutine;
    private InspectableJar inspect;

    void Awake()
    {
        originalParent = transform.parent;
        inspect = GetComponent<InspectableJar>();
    }

    private void OnEnable()
    {
        TouchManager.OnHoldPerformed += TryReturnToSlot;
    }

    private void OnDisable()
    {
        TouchManager.OnHoldPerformed -= TryReturnToSlot;
    }

    public void SaveInitialTransform()
    {
        originalLocalPos = transform.localPosition;
        originalLocalRot = transform.localRotation;
    }

    public void SetMoveStateInspect()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        GetComponent<InspectableJar>().enabled = true;
        moveRoutine = StartCoroutine(MoveToInspect());
    }

    public void SetMoveStateReset()
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        GetComponent<InspectableJar>().enabled = false;

        AssembleManager.Instance.ResetInspect();
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

    public void SetMoveStateInspectToGroup(Transform group)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveToGroup(group));
    }

    IEnumerator MoveToGroup(Transform group)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 endPos = group.position;
        Quaternion endRot = group.rotation;

        float t = 0f;
        float duration = 0.35f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.SetParent(group, true);
    }

    void TryReturnToSlot()
    {
        if (inspect.isRotating && inspect.fingerOnObject) return;
        Ray ray = Camera.main.ScreenPointToRay(TouchManager.Instance.tapPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform)
            {
                if (transform.parent.GetComponent<FragmentGroup>() != null)
                    transform.parent.GetComponent<FragmentGroup>().Remove(this);
                SetMoveStateReset();
            }
        }
    }

}
