using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssembleManager : MonoBehaviour
{
    public static AssembleManager Instance;

    [Header("Artifact Data (ScriptableObject)")]
    public Fragment artefactData;

    [Header("Fragment Slots")]
    public List<Transform> fragmentSlots = new List<Transform>();

    [Header("Inspect Center Position")]
    public Transform inspectCenter;

    private FragmentGroup currentGroup;


    private readonly List<FragmentController> fragments = new List<FragmentController>();
    private FragmentController currentInspectTarget = null;

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
            return;
        }

        if (fragmentSlots.Count < artefactData.fragmentMeshes.Count)
        {
            return;
        }

        fragments.Clear();

        for (int i = 0; i < artefactData.fragmentMeshes.Count; i++)
        {
            Transform slot = fragmentSlots[i];
            FragmentData data = artefactData.fragmentMeshes[i];
            GameObject fragObj = new GameObject("Fragment_" + (i + 1));
            fragObj.transform.SetParent(slot);
            fragObj.transform.localPosition = Vector3.zero;
            fragObj.transform.localRotation = Quaternion.identity;

            fragObj.layer = LayerMask.NameToLayer("Dirts");

            fragObj.AddComponent<MeshFilter>().sharedMesh = data.mesh;
            fragObj.AddComponent<MeshRenderer>().sharedMaterial = data.material;
            fragObj.AddComponent<MeshCollider>();

            FragmentController fc = fragObj.AddComponent<FragmentController>();
            fc.SaveInitialTransform();

            Clean clean = fragObj.AddComponent<Clean>();
            clean._dirtMaskBase = artefactData.mask;
            clean._material = artefactData.mat;

            fragments.Add(fc);
        }
    }

    public void InspectFragment(FragmentController newTarget)
    {
        if (currentInspectTarget == null)
        {
            currentInspectTarget = newTarget;
            currentInspectTarget.SetMoveStateInspect();
            return;
        }

        if (newTarget == currentInspectTarget) return;

        currentInspectTarget.SetMoveStateReset();

        currentInspectTarget = newTarget;
        newTarget.SetMoveStateInspect();
    }

    public void InspectFragment1(FragmentController newTarget)
    {
        // Jika belum ada grup → buat grup baru
        if (currentGroup == null)
        {
            currentGroup = new GameObject("FragmentGroup").AddComponent<FragmentGroup>();
            currentGroup.transform.position = inspectCenter.position;
            currentGroup.transform.rotation = inspectCenter.rotation;

            currentGroup.Add(newTarget);
            newTarget.SetMoveStateInspectToGroup(currentGroup.transform);
            return;
        }

        // Jika fragment sudah dalam grup → cukup fokuskan kamera ke grup
        if (currentGroup.Contains(newTarget)) return;

        // Jika fragment lain di tap → tambahkan fragment itu ke grup
        currentGroup.Add(newTarget);
        newTarget.SetMoveStateInspectToGroup(currentGroup.transform);
    }

    public void TryAssembleFragment(FragmentController newTarget)
    {
        // Jika belum ada grup → buat grup baru
        if (currentGroup == null)
        {
            currentGroup = new GameObject("FragmentGroup").AddComponent<FragmentGroup>();
            currentGroup.transform.position = inspectCenter.position;
            currentGroup.transform.rotation = inspectCenter.rotation;

            currentGroup.Add(newTarget);
            newTarget.SetMoveStateInspectToGroup(currentGroup.transform);
            return;
        }

        // Jika fragment sudah dalam grup → cukup fokuskan kamera ke grup
        if (currentGroup.Contains(newTarget)) return;

        // Jika fragment lain di tap → tambahkan fragment itu ke grup
        currentGroup.Add(newTarget);
        newTarget.SetMoveStateInspectToGroup(currentGroup.transform);
    }

}
