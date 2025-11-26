using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ToolCleaningSurface : MonoBehaviour
{
    public enum CollisionToolsType
    {
        Texture,
        Mesh,
    }

    [Header("Tool Settings")]
    [SerializeField] private CollisionToolsType toolType = CollisionToolsType.Texture;
    [SerializeField] float raycastRange = 5f;
    [SerializeField] private LayerMask dirtsLayerMask;
    [SerializeField] private Texture2D brush;
    [SerializeField] private float brushSize;

    [Header("References")]
    [SerializeField] private SurfaceDetection surface;
    [SerializeField] private ParticleSystem cleaningVFX;

    [Header("Audio Settings")]
    [SerializeField] private float movementThreshold = 0.01f;

    public Vector3 RaycastTipPos { get; private set; }
    public Vector3 RaycastTipNormal { get; private set; }
    private AudioSource cleaningAudioSource;
    private bool isActivelyCleaning = false;
    private Vector3 lastFramePosition;
    private bool isMoving = false;
    private float vfxEmissionRate;


    private bool wasCleaningLastFrame = false;


    void Awake()
    {
        cleaningAudioSource = GetComponent<AudioSource>();
        if (surface == null)
        {
            Debug.LogError("SurfaceDetection belum di-assign di ToolCleaningSurface!");
        }
        if (cleaningVFX == null)
        {
            Debug.LogError("CleaningVFX belum di-assign di Inspector!");
        }
        else
        {
            vfxEmissionRate = cleaningVFX.emission.rateOverTime.constant;
            var emissionModule = cleaningVFX.emission;
            emissionModule.rateOverTime = 0f;
        }
        lastFramePosition = transform.position;
    }

    void OnEnable()
    {
        SettingManager.OnSfxVolumeChanged += SetSFXVolume;
    }

    void OnDisable()
    {
        SettingManager.OnSfxVolumeChanged -= SetSFXVolume;
    }

    void Update()
    {
        if (surface == null) return;

        CheckForMovement();
        RaycastCleaningSurface();
        HandleEffects();
        HandleHaptics();
    }

    private void CheckForMovement()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastFramePosition);
        isMoving = distanceMoved > movementThreshold;
        lastFramePosition = transform.position;
    }

    private void RaycastCleaningSurface()
    {
        isActivelyCleaning = false;

        switch (toolType)
        {
            case CollisionToolsType.Mesh:
                if (surface.MudObject != null)
                {
                    isActivelyCleaning = TryDestroyMesh(surface.MudObject);
                }
                break;
            case CollisionToolsType.Texture:
                if (surface.CleaningSurface != null)
                {
                    isActivelyCleaning = TryClean(surface.CleaningSurface, surface.TextureSurface);
                    Debug.Log("isActivelyCleaning: " + isActivelyCleaning);
                }
                break;
        }
    }

    private bool TryClean(Clean clean, Vector2 textureCoord)
    {
        // return clean.CleanAt(textureCoord, brush, brushSize, surface.RaycastTipRotation);
        return clean.CleaningAtPoint(textureCoord, brush);
    }

    private bool TryDestroyMesh(CleanMesh obj)
    {
        return obj.DestroyMesh();
    }

    private void HandleEffects()
    {
        bool conditionsMet = isActivelyCleaning && isMoving;
        if (conditionsMet)
        {
            if (!cleaningAudioSource.isPlaying)
            {
                cleaningAudioSource.Play();
            }
        }

        if (cleaningVFX != null)
        {
            var emissionModule = cleaningVFX.emission;

            if (conditionsMet)
            {
                emissionModule.rateOverTime = vfxEmissionRate;
            }
            else
            {
                emissionModule.rateOverTime = 0f;
            }
        }
    }

    private void HandleHaptics()
    {
        bool conditionsMet = isActivelyCleaning && isMoving;

        if (conditionsMet)
        {
            // Start continuous haptics only once
            if (!wasCleaningLastFrame)
            {
                HapticManager.Instance.StartContinuous(HapticManager.HapticType.Medium);
            }
        }
        else
        {
            // Stop when movement stops or surface lost
            if (wasCleaningLastFrame)
            {
                HapticManager.Instance.StopContinuous();
            }
        }

        wasCleaningLastFrame = conditionsMet;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * raycastRange);
    }

    private void SetSFXVolume(float vol)
    {
        cleaningAudioSource.volume = vol;
    }
}