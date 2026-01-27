using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanMesh : MonoBehaviour
{
    void Start()
    {
        CleanManager.Instance.RegisterMud(this, false);
    }

    public bool DestroyMesh()
    {
        CleanManager.Instance.RegisterMud(this, true);
        Destroy(gameObject);
        return true;
    }
}
