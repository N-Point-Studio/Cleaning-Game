using System.Collections.Generic;
using UnityEngine;

public class AssembleManager : MonoBehaviour
{
    [Header("Artifact Data (ScriptableObject)")]
    public Fragment artefactData;

    [Header("Slots untuk fragment (urutan harus sesuai dengan ScriptableObject)")]
    public List<Transform> fragmentSlots = new List<Transform>();
    // Drag Fragment_position_1, Fragment_position_2, ... ke sini

    [Header("Inspect Center Position")]
    public Transform inspectCenter;

    [Header("Attach Settings")]
    public float snapDistance = 0.25f;

    private List<FragmentController> fragments = new List<FragmentController>();
    private FragmentController currentInspectTarget = null;

    public static AssembleManager Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        LoadFragmentsIntoSlots();
    }

    void LoadFragmentsIntoSlots()
    {
        if (artefactData == null)
        {
            Debug.LogError("[AssembleManager] Missing artefactData!");
            return;
        }

        if (fragmentSlots.Count < artefactData.fragmentMeshes.Count)
        {
            Debug.LogError("[AssembleManager] Slot count tidak cukup untuk fragment data!");
            return;
        }

        fragments.Clear();

        for (int i = 0; i < artefactData.fragmentMeshes.Count; i++)
        {
            Transform slot = fragmentSlots[i];

            FragmentData data = artefactData.fragmentMeshes[i];

            // Create fragment object
            GameObject fragObj = new GameObject("Fragment_" + (i + 1));
            fragObj.transform.SetParent(slot);
            fragObj.transform.localPosition = Vector3.zero;
            fragObj.transform.localRotation = Quaternion.identity;

            // Mesh and material
            MeshFilter mf = fragObj.AddComponent<MeshFilter>();
            mf.sharedMesh = data.mesh;

            MeshRenderer mr = fragObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = data.material;

            // Add logic components
            FragmentController fc = fragObj.AddComponent<FragmentController>();
            fragObj.AddComponent<FragmentDraggable>();
            MeshCollider mc = fragObj.AddComponent<MeshCollider>();

            // Auto attach point
            GameObject attach = new GameObject("AttachPoint");
            attach.transform.SetParent(fragObj.transform);
            attach.transform.localPosition = Vector3.zero;
            fc.attachPoint = attach.transform;

            fc.SaveInitialTransform();
            fragments.Add(fc);
        }
    }

    public void Inspect(FragmentController selected)
    {
        currentInspectTarget = selected;
        selected.transform.position = inspectCenter.position;
    }

    public void TryAttach(FragmentController dragged)
    {
        if (currentInspectTarget == null || dragged == currentInspectTarget)
            return;

        float dist = Vector3.Distance(
            dragged.transform.position,
            currentInspectTarget.attachPoint.position
        );

        if (dist <= snapDistance)
        {
            dragged.transform.position = currentInspectTarget.attachPoint.position;
            dragged.transform.rotation = currentInspectTarget.attachPoint.rotation;
            dragged.isAttached = true;

            // Set next reference for chain attach
            currentInspectTarget = dragged;
            CheckCompletion();
        }
    }

    public void ResetFragment(FragmentController fc)
    {
        fc.ResetFragment();
        if (currentInspectTarget == fc)
            currentInspectTarget = null;
    }

    void CheckCompletion()
    {
        foreach (var frag in fragments)
        {
            if (!frag.isAttached)
                return;
        }
    }

    void OnDrawGizmos()
    {
        if (inspectCenter == null || fragmentSlots == null)
            return;

        Gizmos.color = Color.yellow;

        foreach (var slot in fragmentSlots)
        {
            if (slot == null) continue;
            Gizmos.DrawLine(slot.position, inspectCenter.position);
            Gizmos.DrawSphere(inspectCenter.position, 0.02f);
        }
    }

}
