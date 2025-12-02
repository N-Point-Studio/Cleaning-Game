using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }


    [Header("UI")]
    [SerializeField] private Slider SfxSlider;
    [SerializeField] private Slider BgmSlider;
    [SerializeField] private Switch HapticSwitch;

    [SerializeField] private Switch CueSwitch;

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;



    public static event Action<float> OnSfxVolumeChanged;
    public static event Action<float> OnBgmVolumeChanged;

    public bool isTipPointEnabled = false;



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // SAFETY CHECK: Only add listeners if sliders are assigned
        if (SfxSlider != null)
            SfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        else
            Debug.LogWarning("SfxSlider not assigned in SettingManager");

        if (BgmSlider != null)
            BgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        else
            Debug.LogWarning("BgmSlider not assigned in SettingManager");
    }

    private void Start()
    {
        // SAFETY CHECK: Only set volume if bgmSource is assigned
        if (bgmSource != null)
        {
            bgmSource.volume = GetNormalizedSliderValue(BgmSlider, 0.5f);
        }
        else
        {
            Debug.LogWarning("bgmSource not assigned in SettingManager");
        }

        // Apply initial slider values so listeners get the current setting on scene start
        if (SfxSlider != null)
        {
            OnSfxSliderChanged(SfxSlider.value); // Will normalize inside handler
        }
        if (BgmSlider != null)
        {
            OnBgmSliderChanged(BgmSlider.value); // Will normalize inside handler
        }
    }

    private void Update()
    {
        // SAFETY CHECK: Prevent NullReferenceException during scene transitions
        if (HapticManager.Instance != null && HapticSwitch != null)
        {
            HapticManager.Instance.SetActiveHaptic(HapticSwitch.isOn);
        }

        isTipPointEnabled = CueSwitch != null && CueSwitch.isOn;
    }

    private void OnSfxSliderChanged(float value)
    {
        float normalized = GetNormalizedSliderValue(SfxSlider, value);
        Debug.Log("OnSfxSliderChanged (normalized): " + normalized);
        OnSfxVolumeChanged?.Invoke(normalized);
    }

    private void OnBgmSliderChanged(float value)
    {
        float normalized = GetNormalizedSliderValue(BgmSlider, value);
        Debug.Log("OnBgmSliderChanged (normalized): " + normalized);

        // SAFETY CHECK: Only set volume if bgmSource is assigned
        if (bgmSource != null)
        {
            bgmSource.volume = normalized;
            bgmSource.mute = normalized <= 0.001f;
        }
        else
        {
            Debug.LogWarning("bgmSource is null - cannot set volume");
        }

        OnBgmVolumeChanged?.Invoke(normalized);

        // Belt-and-suspenders: apply to all looped/tagged music sources in scene
        ApplyBgmVolumeToScene(normalized);
    }

    private float GetNormalizedSliderValue(Slider slider, float fallbackIfNull)
    {
        if (slider == null) return fallbackIfNull;

        // Normalize slider value to 0..1 regardless of min/max so UI can use 0-100 or other ranges.
        if (Mathf.Approximately(slider.maxValue, slider.minValue))
        {
            return Mathf.Clamp01(slider.value);
        }

        float t = Mathf.InverseLerp(slider.minValue, slider.maxValue, slider.value);
        return Mathf.Clamp01(t);
    }

    private void ApplyBgmVolumeToScene(float normalized)
    {
        var audioSources = FindObjectsOfType<AudioSource>(true);
        foreach (var src in audioSources)
        {
            bool looksLikeMusic = src.loop || src.CompareTag("BGM");
            if (!looksLikeMusic) continue;

            src.volume = normalized;
            src.mute = normalized <= 0.001f;
        }
    }
}
