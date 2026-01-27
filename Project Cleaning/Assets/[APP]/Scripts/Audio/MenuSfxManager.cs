using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Global UI SFX for the menu: auto-hooks all Buttons and EventTriggers to play click SFX.
/// Put this on one GameObject in the menu, assign AudioSource + clips.
/// Swipe/page change can call PlaySwipe() manually.
/// </summary>
public class MenuSfxManager : MonoBehaviour
{
    public static MenuSfxManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private AudioClip swipeSfx;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool autoHookOnSceneLoad = true;
    [SerializeField] private bool debugLogs = false;

    private readonly HashSet<Object> hookedObjects = new HashSet<Object>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (autoHookOnSceneLoad)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    private void Start()
    {
        StartCoroutine(DelayedHook());
        if (debugLogs) Debug.Log("[MenuSfxManager] Start() running, beginning delayed hook");
    }

    private void OnDestroy()
    {
        if (Instance == this && autoHookOnSceneLoad)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        SettingManager.OnSfxVolumeChanged -= SetVolume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedHook());
    }

    private void OnEnable()
    {
        SettingManager.OnSfxVolumeChanged += SetVolume;
    }

    private void OnDisable()
    {
        SettingManager.OnSfxVolumeChanged -= SetVolume;
    }

    private System.Collections.IEnumerator DelayedHook()
    {
        // Wait a frame so dynamically-built UI is present
        yield return null;
        if (debugLogs) Debug.Log("[MenuSfxManager] DelayedHook executing");

        // Clean stale references from previous scenes and rehook everything fresh
        hookedObjects.Clear();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        HookAllUiInteractables();
    }

    /// <summary>
    /// Hook all buttons and event triggers in the active scene to play click SFX.
    /// </summary>
    public void HookAllUiInteractables()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[MenuSfxManager] AudioSource is null, cannot hook UI.");
            return;
        }

        int hookedButtons = 0;
        int hookedTriggers = 0;

        var buttons = FindObjectsOfType<Button>(true);
        foreach (var btn in buttons)
        {
            if (hookedObjects.Contains(btn)) continue;
            btn.onClick.AddListener(PlayClick);
            hookedObjects.Add(btn);
            hookedButtons++;
        }

        var triggers = FindObjectsOfType<EventTrigger>(true);
        foreach (var trigger in triggers)
        {
            if (hookedObjects.Contains(trigger)) continue;

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => PlayClick());
            trigger.triggers.Add(entry);

            hookedObjects.Add(trigger);
            hookedTriggers++;
        }

        if (debugLogs)
        {
            Debug.Log($"[MenuSfxManager] Hooked {hookedButtons} Buttons, {hookedTriggers} EventTriggers (total tracked: {hookedObjects.Count}).");
        }
    }

    public void PlayClick()
    {
        if (audioSource != null && clickSfx != null)
        {
            audioSource.PlayOneShot(clickSfx, volume);
        }
    }

    public void PlaySwipe()
    {
        if (audioSource != null && swipeSfx != null)
        {
            audioSource.PlayOneShot(swipeSfx, volume);
        }
    }

    public void SetVolume(float value)
    {
        volume = value;
        if (audioSource != null) audioSource.volume = value;
    }
}
