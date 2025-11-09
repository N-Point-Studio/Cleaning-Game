using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FragmentStateMachine : StateMachine
{
    [SerializeField] public DraggableObject draggableObject { get; private set; }
    public Transform MainCameraTransform { get; private set; }

    private void Start()
    {
        MainCameraTransform = Camera.main.transform;
        // SwitchState(new PlayerFreeLookState(this));
    }

    private void OnEnable()
    {
        // Health.OnTakeDamage += HandleTakeDamage;
        // Health.OnDie += HandleDie;
    }

    void OnDisable()
    {
        // Health.OnTakeDamage -= HandleTakeDamage;
        // Health.OnDie -= HandleDie;
    }

    private void HandleTakeDamage()
    {
        // SwitchState(new PlayerImpactState(this));
    }
    private void HandleDie()
    {
        // SwitchState(new PlayerDeadState(this));
    }
}
