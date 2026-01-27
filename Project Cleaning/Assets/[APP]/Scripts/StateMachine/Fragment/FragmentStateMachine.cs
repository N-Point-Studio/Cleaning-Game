using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FragmentInteraction))]
[RequireComponent(typeof(AudioSource))]
public class FragmentStateMachine : StateMachine
{
    public List<FragmentStateMachine> StateMachineConnected = new List<FragmentStateMachine>();
    public FragmentInteraction Interaction { get; private set; }
    public Camera MainCamera { get; private set; }
    [field: SerializeField] public Vector3 InitialPosition { get; set; }
    [field: SerializeField] public Quaternion InitialRotation { get; set; }
    public AudioClip putSound;
    public AudioClip assembleSound;
    [SerializeField] public AudioSource audioSource;

    public string CurrentStatus;

    private void Awake()
    {
        MainCamera = Camera.main;
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;
    }

    void OnEnable()
    {
        SettingManager.OnSfxVolumeChanged += SetSFXVolume;
    }

    void OnDisable()
    {
        SettingManager.OnSfxVolumeChanged -= SetSFXVolume;
    }

    private void Start()
    {
        Interaction = GetComponent<FragmentInteraction>();
        audioSource = GetComponent<AudioSource>();
        Interaction.SetInitialPos(InitialPosition);
        SwitchState(new FragmentIdleState(this));
    }

    public void DisableAllInteraction()
    {
        Interaction.isDragAvailable = false;
        Interaction.isTapAvailable = false;
        Interaction.isHoldAvailable = false;
    }

    private void SetSFXVolume(float vol)
    {
        audioSource.volume = vol;
    }
}
