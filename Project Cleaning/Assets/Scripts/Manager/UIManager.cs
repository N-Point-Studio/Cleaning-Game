using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Separate Progress Bars")]
    [SerializeField] private ProgressBar progressDirts;
    [SerializeField] private ProgressBar progressDusts;
    [SerializeField] private ProgressBar progressAssemble;
    [SerializeField] private GameObject settingCanvas;
    [SerializeField] private Button ExitButton;
    [SerializeField] private Button ResumeButton;
    [SerializeField] private GameObject FinishUI;
    [SerializeField] private GameObject FinishBackground;
    [SerializeField] private Image FinishButtonImage; // The finish button image (drag & drop)
    [SerializeField] private bool useImageAsButton = true; // Toggle untuk menggunakan Image sebagai button
    private bool isSettingShown = false;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ExitButton.onClick.AddListener(ExitButtonInteract);
        ResumeButton.onClick.AddListener(ResumeButtonInteract);

        // Setup finish button listener
        SetupFinishButton();
    }

    /// <summary>
    /// Setup finish button - supports both Button and Image components
    /// </summary>
    private void SetupFinishButton()
    {
        if (useImageAsButton && FinishButtonImage != null)
        {
            // Method 1: Add EventTrigger to Image
            SetupImageAsButton();
            Debug.Log("Finish button image listener added");
        }
        else
        {
            // Method 2: Try to find Button component
            var buttonComponent = FinishButtonImage?.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(FinishButtonInteract);
                Debug.Log("Finish button component listener added");
            }
            else
            {
                Debug.LogWarning("Finish button not assigned or Button component not found!");
            }
        }
    }

    /// <summary>
    /// Setup Image sebagai clickable button menggunakan EventTrigger
    /// </summary>
    private void SetupImageAsButton()
    {
        if (FinishButtonImage == null) return;

        // Pastikan Image bisa di-raycast
        FinishButtonImage.raycastTarget = true;

        // Add EventTrigger component jika belum ada
        EventTrigger trigger = FinishButtonImage.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = FinishButtonImage.gameObject.AddComponent<EventTrigger>();
        }

        // Clear existing listeners
        trigger.triggers.Clear();

        // Add PointerClick event
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((data) => { FinishButtonInteract(); });
        trigger.triggers.Add(entry);

        // Optional: Add hover effects
        SetupFinishButtonHoverEffects(trigger);
    }

    /// <summary>
    /// Add hover effects to finish button image
    /// </summary>
    private void SetupFinishButtonHoverEffects(EventTrigger trigger)
    {
        // Store original color
        Color originalColor = FinishButtonImage.color;

        // PointerEnter (hover)
        EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
        hoverEntry.eventID = EventTriggerType.PointerEnter;
        hoverEntry.callback.AddListener((data) =>
        {
            FinishButtonImage.color = new Color(originalColor.r * 0.8f, originalColor.g * 0.8f, originalColor.b * 0.8f, originalColor.a);
        });
        trigger.triggers.Add(hoverEntry);

        // PointerExit (unhover)
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) =>
        {
            FinishButtonImage.color = originalColor;
        });
        trigger.triggers.Add(exitEntry);

        // PointerDown (click effect)
        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) =>
        {
            FinishButtonImage.color = new Color(originalColor.r * 0.6f, originalColor.g * 0.6f, originalColor.b * 0.6f, originalColor.a);
        });
        trigger.triggers.Add(downEntry);

        // PointerUp (release effect)
        EventTrigger.Entry upEntry = new EventTrigger.Entry();
        upEntry.eventID = EventTriggerType.PointerUp;
        upEntry.callback.AddListener((data) =>
        {
            FinishButtonImage.color = originalColor;
        });
        trigger.triggers.Add(upEntry);
    }

    private void Update()
    {
        ProgressUpdate();
    }

    private void ProgressUpdate()
    {
        float dustProgress = CleanManager.Instance.GetDustProgress();
        progressDusts.SetValue(10);

        float mudProgress = CleanManager.Instance.GetMudProgress();
        progressDirts.SetValue(10);

        float attachProgress = AssembleManager.Instance.GetAttachProgress();
        progressAssemble.SetValue(attachProgress);
    }

    public void ShowSetting(bool isShown)
    {
        settingCanvas.SetActive(isShown);
        TouchManager.Instance.TouchUsed(isShown);
    }

    public void ShowFinishUI(bool isShown)
    {
        FinishUI.SetActive(isShown);
    }

    public void ShowFinishBackground(bool isShown)
    {
        FinishBackground.SetActive(isShown);
    }

    public void ExitButtonInteract()
    {
        Debug.Log("Exit level");
    }

    public void ResumeButtonInteract()
    {
        Debug.Log("Exit level");
        ShowSetting(false);
    }

    public float GetAllProgressValue()
    {
        return (progressDirts.GetValue() + progressAssemble.GetValue() + progressDusts.GetValue()) / 3;
    }

    /// <summary>
    /// Handle finish button click - triggers scene transition
    /// </summary>
    public void FinishButtonInteract()
    {
        Debug.Log("=== FINISH BUTTON CLICKED ===");

        // Check if GamePlayManager exists
        if (GamePlayManager.Instance != null)
        {
            // Trigger the finish button functionality in GamePlayManager
            GamePlayManager.Instance.TriggerFinishButton();
        }
        else
        {
            Debug.LogError("GamePlayManager.Instance not found! Cannot trigger scene transition.");
        }
    }

    /// <summary>
    /// Enable or disable the finish button (works for both Image and Button)
    /// </summary>
    public void SetFinishButtonEnabled(bool enabled)
    {
        if (FinishButtonImage != null)
        {
            // For Image: disable/enable raycast target
            FinishButtonImage.raycastTarget = enabled;

            // Visual feedback
            Color color = FinishButtonImage.color;
            color.a = enabled ? 1f : 0.5f; // Fade when disabled
            FinishButtonImage.color = color;

            Debug.Log($"Finish button {(enabled ? "enabled" : "disabled")}");
        }
    }

    /// <summary>
    /// Show/hide finish button (works for both Image and Button)
    /// </summary>
    public void ShowFinishButton(bool show)
    {
        if (FinishButtonImage != null)
        {
            FinishButtonImage.gameObject.SetActive(show);
            Debug.Log($"Finish button {(show ? "shown" : "hidden")}");
        }
    }

    /// <summary>
    /// Manual method to trigger finish button (for testing)
    /// </summary>
    public void TestFinishButton()
    {
        Debug.Log("=== TESTING FINISH BUTTON ===");
        FinishButtonInteract();
    }
}
