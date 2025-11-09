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

            fragObj.AddComponent<FragmentStateMachine>();


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

        if (currentGroup.members.Count != 0)
        {

        }

        currentInspectTarget.SetMoveStateReset();

        currentInspectTarget = newTarget;
        newTarget.SetMoveStateInspect();
    }

    public void ResetInspect()
    {
        currentInspectTarget = null;
    }

    public void TryAssembleFragment(FragmentController newTarget)
    {
        // Kalau belum ada group, buat group baru
        if (currentGroup == null)
        {
            currentGroup = new GameObject("FragmentGroup").AddComponent<FragmentGroup>();
            currentGroup.transform.position = inspectCenter.position;
            currentGroup.transform.rotation = inspectCenter.rotation;
            currentGroup.transform.SetParent(inspectCenter, true);

            // Simpan di slot fragment yang sedang di-inspect
            currentGroup.originalSlot = currentInspectTarget.transform.parent;
        }


        // Pastikan fragment yang currently di-inspect ikut masuk group
        if (currentInspectTarget != null && !currentGroup.Contains(currentInspectTarget))
        {
            Debug.Log("HARUSNYA MASUK");
            currentGroup.Add(currentInspectTarget);
            currentInspectTarget.SetMoveStateInspectToGroup(currentGroup.transform);
        }

        // Kalau newTarget sudah ada di dalam group, tidak perlu diproses
        if (currentGroup.Contains(newTarget))
        {
            Debug.Log("Group already contains " + newTarget.name);
            return;
        }

        // Masukkan fragment baru ke group
        currentGroup.Add(newTarget);
        newTarget.SetMoveStateInspectToGroup(currentGroup.transform);
    }


}
