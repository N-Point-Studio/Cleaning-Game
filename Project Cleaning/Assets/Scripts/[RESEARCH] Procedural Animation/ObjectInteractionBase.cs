using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ObjectInteractionBase : MonoBehaviour
{
    // public abstract void EnterState();
    // public abstract void ExitState();
    // public abstract void UpdateState();
    public abstract void OnTriggerEnter(Collider collider);
    public abstract void OnTriggerStay(Collider collider);
    public abstract void OnTriggerExit(Collider collider);
}
