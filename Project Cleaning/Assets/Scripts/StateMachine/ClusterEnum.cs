using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ClusterState
{
    Created,
    Idle,
    MoveToInspect,
    Inspect,
    Return,
    Assemble,
    Unassembled,
    Finished
}
