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

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;

    public static event Action<float> OnSfxVolumeChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        SfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        BgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
    }

    private void Start()
    {
        bgmSource.volume = .5f;
    }

    private void Update()
    {
        HapticManager.Instance.SetActiveHaptic(HapticSwitch.isOn);
    }

    private void OnSfxSliderChanged(float value)
    {
        Debug.Log("OnSfxSliderChanged: " + value);
        OnSfxVolumeChanged?.Invoke(value);
    }

    private void OnBgmSliderChanged(float value)
    {
        Debug.Log("OnBgmSliderChanged: " + value);
        bgmSource.volume = value;
    }
}
