using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentGroup : MonoBehaviour
{
    public List<FragmentController> members = new List<FragmentController>();

    public void Add(FragmentController frag)
    {
        members.Add(frag);
        frag.transform.SetParent(transform, true);
    }

    public void Remove(FragmentController frag)
    {
        members.Remove(frag);
        frag.transform.SetParent(null);

        if (members.Count <= 1)
        {
            if (members.Count == 1)
                members[0].transform.SetParent(null);

            Destroy(gameObject);
        }
    }

    public bool Contains(FragmentController frag) => members.Contains(frag);
}
