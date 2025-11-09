using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentGroup : MonoBehaviour
{
    public List<FragmentController> members = new List<FragmentController>();
    public Transform originalSlot;


    public void Add(FragmentController frag)
    {
        members.Add(frag);
        frag.transform.SetParent(transform, true);
    }

    public void Remove(FragmentController frag)
    {
        members.Remove(frag);

    }

    public bool Contains(FragmentController frag) => members.Contains(frag);
}
