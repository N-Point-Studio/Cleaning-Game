using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS
using CandyCoded.HapticFeedback;
#endif

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance;

    [Header("Continuous Haptic Settings")]
    public float continuousInterval = 0.05f;
    private bool continuousActive = false;
    private float timer;
    private HapticType currentType;
    public bool IsActivated { get; private set; } = false;

    public enum HapticType
    {
        Default,
        Light,
        Medium,
        Heavy
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!continuousActive || !IsActivated)
            return;

        timer += Time.deltaTime;

        if (timer >= continuousInterval)
        {
            Play(currentType);
            timer = 0f;
        }
    }

    public void Play(HapticType type)
    {
        switch (type)
        {
            case HapticType.Default:
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
                break;

            case HapticType.Light:
#if UNITY_ANDROID || UNITY_IOS
                HapticFeedback.LightFeedback();
#endif
                break;

            case HapticType.Medium:
#if UNITY_ANDROID || UNITY_IOS
                HapticFeedback.MediumFeedback();
#endif
                break;

            case HapticType.Heavy:
#if UNITY_ANDROID || UNITY_IOS
                HapticFeedback.HeavyFeedback();
#endif
                break;
        }
    }

    public void Default() => Play(HapticType.Default);
    public void Light() => Play(HapticType.Light);
    public void Medium() => Play(HapticType.Medium);
    public void Heavy() => Play(HapticType.Heavy);

    public void StartContinuous(HapticType type)
    {
        continuousActive = true;
        currentType = type;
        timer = 0f;
    }

    public void StopContinuous()
    {
        continuousActive = false;
    }

    public void SetActiveHaptic(bool isActive)
    {
        IsActivated = isActive;
    }
}
