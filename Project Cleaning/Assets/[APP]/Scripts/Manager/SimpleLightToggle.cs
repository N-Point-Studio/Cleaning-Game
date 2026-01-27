using UnityEngine;
using System.Collections;

/// <summary>
/// Super Simple Light Toggle - Just enables/disables the light GameObject
/// No intensity changes, no lag - just on/off like a light switch!
/// </summary>
public class SimpleLightToggle : MonoBehaviour
{
    [Header("Simple On/Off Control")]
    [SerializeField] private bool startLightOff = true;

    [Header("Flickering Settings")]
    [SerializeField] private bool enableFlickering = true;
    [SerializeField] private float flickerChance = 0.2f; // 20% chance - much more obvious!
    [SerializeField] private float flickerDuration = 0.15f; // Longer flicker
    [SerializeField] private float flickerCheckRate = 0.3f; // Check every 0.3 seconds

    [Header("Manual Control (For Testing)")]
    [SerializeField] private bool manualControl = false;
    [SerializeField] private bool manualLightOn = false;

    private bool isLightOn = false;
    private bool isFlickering = false;
    private Coroutine flickerCoroutine;

    // Singleton for easy access
    public static SimpleLightToggle Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Start with light off if specified
        if (startLightOff)
        {
            TurnOffLight();
        }
        else
        {
            TurnOnLight();
        }

        // Start listening for exploration mode
        StartCoroutine(WatchForExplorationMode());

        Debug.Log($"SimpleLightToggle ready - Light starts: {(startLightOff ? "OFF" : "ON")}");
    }

    /// <summary>
    /// Watch for exploration mode and toggle light automatically
    /// </summary>
    private IEnumerator WatchForExplorationMode()
    {
        // Wait for AdvancedInputManager
        while (AdvancedInputManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        while (true)
        {
            // Manual control override
            if (manualControl)
            {
                if (manualLightOn && !isLightOn)
                {
                    TurnOnLight();
                }
                else if (!manualLightOn && isLightOn)
                {
                    TurnOffLight();
                }
            }
            else
            {
                // Automatic mode - turn on during exploration/zoom
                bool shouldBeOn = AdvancedInputManager.Instance.IsInExplorationMode() ||
                                AdvancedInputManager.Instance.IsInZoomMode();

                if (shouldBeOn && !isLightOn)
                {
                    TurnOnLight();
                }
                else if (!shouldBeOn && isLightOn)
                {
                    TurnOffLight();
                }
            }

            yield return new WaitForSeconds(0.3f); // Check every 0.3 seconds
        }
    }

    /// <summary>
    /// Turn light ON - enable the GameObject and start flickering
    /// </summary>
    public void TurnOnLight()
    {
        gameObject.SetActive(true);
        isLightOn = true;

        // Start flickering if enabled
        if (enableFlickering)
        {
            StartFlickering();
        }

        Debug.Log("💡 Light ON - GameObject enabled with flickering");
    }

    /// <summary>
    /// Turn light OFF - disable the GameObject and stop flickering
    /// </summary>
    public void TurnOffLight()
    {
        StopFlickering();
        gameObject.SetActive(false);
        isLightOn = false;
        Debug.Log("💤 Light OFF - GameObject disabled");
    }

    /// <summary>
    /// Toggle light on/off
    /// </summary>
    public void ToggleLight()
    {
        if (isLightOn)
        {
            TurnOffLight();
        }
        else
        {
            TurnOnLight();
        }
    }

    /// <summary>
    /// Manual test methods
    /// </summary>
    [ContextMenu("Test - Turn On")]
    public void TestTurnOn()
    {
        TurnOnLight();
    }

    [ContextMenu("Test - Turn Off")]
    public void TestTurnOff()
    {
        TurnOffLight();
    }

    [ContextMenu("Test - Toggle")]
    public void TestToggle()
    {
        ToggleLight();
    }

    [ContextMenu("Test - Force Flicker")]
    public void TestForceFlicker()
    {
        if (isLightOn)
        {
            StartFlickering();
        }
        else
        {
            TurnOnLight(); // This will start flickering automatically
        }
    }

    #region Flickering Logic

    /// <summary>
    /// Start the flickering effect
    /// </summary>
    private void StartFlickering()
    {
        if (!enableFlickering) return;

        StopFlickering(); // Stop any existing flicker
        flickerCoroutine = StartCoroutine(FlickerRoutine());
        Debug.Log("🔥 Flickering started!");
    }

    /// <summary>
    /// Stop the flickering effect
    /// </summary>
    private void StopFlickering()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }
        isFlickering = false;
    }

    /// <summary>
    /// Main flickering routine - randomly flickers the light on/off
    /// </summary>
    private IEnumerator FlickerRoutine()
    {
        Debug.Log("🔥 Flicker routine started - should flicker every few seconds");

        while (isLightOn && enableFlickering)
        {
            // Random chance to flicker
            if (Random.Range(0f, 1f) < flickerChance)
            {
                Debug.Log("💫 FLICKER TRIGGERED!");
                StartCoroutine(PerformFlicker());
            }

            yield return new WaitForSeconds(flickerCheckRate);
        }

        Debug.Log("🔥 Flicker routine stopped");
    }

    /// <summary>
    /// Perform a single flicker (quick off-on)
    /// </summary>
    private IEnumerator PerformFlicker()
    {
        if (isFlickering) yield break; // Prevent multiple flickers at once

        isFlickering = true;
        Debug.Log("⚡ FLICKERING NOW - OFF then ON!");

        // Quick flicker: off -> on
        gameObject.SetActive(false);
        Debug.Log("💡 Light OFF for flicker");
        yield return new WaitForSeconds(flickerDuration * 0.4f); // Off period

        gameObject.SetActive(true);
        Debug.Log("💡 Light ON after flicker");
        yield return new WaitForSeconds(flickerDuration * 0.6f); // On period

        isFlickering = false;
    }

    #endregion

    // Public getters
    public bool IsLightOn() => isLightOn;
    public bool IsFlickering() => isFlickering;

    private void OnDestroy()
    {
        StopFlickering();
        if (Instance == this) Instance = null;
    }
}