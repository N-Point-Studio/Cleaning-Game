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
    public bool isTipPointEnabled = true;


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
            bgmSource.volume = .5f;
        }
        else
        {
            Debug.LogWarning("bgmSource not assigned in SettingManager");
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
        Debug.Log("OnSfxSliderChanged: " + value);
        OnSfxVolumeChanged?.Invoke(value);
    }

    private void OnBgmSliderChanged(float value)
    {
        Debug.Log("OnBgmSliderChanged: " + value);

        // SAFETY CHECK: Only set volume if bgmSource is assigned
        if (bgmSource != null)
        {
            bgmSource.volume = value;
        }
        else
        {
            Debug.LogWarning("bgmSource is null - cannot set volume");
        }
    }
}
