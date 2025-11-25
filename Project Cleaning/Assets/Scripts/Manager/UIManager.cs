using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public enum ProgressType { Dirt, Assemble, Dust }

    [Header("Separate Progress Bars")]
    [SerializeField] private ProgressBar progressDirts;
    [SerializeField] private ProgressBar progressDusts;
    [SerializeField] private ProgressBar progressAssemble;
    [SerializeField] private GameObject progressDirtsGO;
    [SerializeField] private GameObject progressDustsGO;
    [SerializeField] private GameObject progressAssembleGO;
    [SerializeField] private Text artefactNameText;
    [SerializeField] private GameObject settingCanvas;
    [SerializeField] private Button FinishButton; // Optional direct button reference
    [SerializeField] private Button ExitButton;
    [SerializeField] private Button ResumeButton;
    [SerializeField] private GameObject FinishUI;
    [SerializeField] private GameObject FinishBackground;
    [SerializeField] private Image FinishButtonImage; // The finish button image (drag & drop)
    [SerializeField] private bool useImageAsButton = true; // Toggle untuk menggunakan Image sebagai button
    private bool isSettingShown = false;
    private bool isSceneUnloading = false;
    private int minusFactor = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    private void Start()
    {
        ExitButton.onClick.AddListener(ExitButtonInteract);
        ResumeButton.onClick.AddListener(ResumeButtonInteract);

        EnsureUIInputReady();

        // Setup finish button listener
        SetupFinishButton();
    }

    /// <summary>
    /// Setup finish button - supports both Button and Image components
    /// </summary>
    private void SetupFinishButton()
    {
        // Prefer explicit button reference if provided
        if (FinishButton != null)
        {
            FinishButton.onClick.AddListener(FinishButtonInteract);
            Debug.Log("Finish Button listener added via Button reference");
            return;
        }

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
        // SAFETY CHECK: Stop processing if scene is unloading
        if (isSceneUnloading)
            return;

        try
        {
            ProgressUpdate();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"UIManager Update failed (likely scene unloading): {ex.Message}");
            isSceneUnloading = true; // Stop further processing
        }
    }

    private void ProgressUpdate()
    {
        // SAFETY CHECK: Verify all progress bars still exist
        if (progressDusts == null || progressDirts == null || progressAssemble == null)
        {
            Debug.LogWarning("ProgressBar destroyed - stopping UIManager updates");
            isSceneUnloading = true;
            return;
        }

        // SAFETY CHECK: Verify managers still exist
        if (CleanManager.Instance == null || AssembleManager.Instance == null)
        {
            Debug.LogWarning("Manager destroyed - stopping UIManager updates");
            isSceneUnloading = true;
            return;
        }

        try
        {
            float dustProgress = CleanManager.Instance.GetDustProgress();
            progressDusts.SetValue(dustProgress);

            float mudProgress = CleanManager.Instance.GetMudProgress();
            progressDirts.SetValue(mudProgress);

            float attachProgress = AssembleManager.Instance.GetAttachProgress();
            progressAssemble.SetValue(attachProgress);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"ProgressUpdate failed: {ex.Message}");
            isSceneUnloading = true;
        }
    }

    public void ShowSetting(bool isShown)
    {
        settingCanvas.SetActive(isShown);
        TouchManager.Instance.TouchUsed(isShown);
        if (isShown)
        {
            EnsureUIInputReady(); // pastikan EventSystem & raycaster aktif saat overlay dibuka
        }
    }

    public void ShowFinishUI(bool isShown)
    {
        // SAFETY CHECK: Verify UI objects still exist before accessing
        if (FinishUI == null)
        {
            Debug.LogWarning("FinishUI has been destroyed - cannot show/hide");
            isSceneUnloading = true;
            return;
        }

        try
        {
            FinishUI.SetActive(isShown);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"ShowFinishUI failed: {ex.Message}");
            isSceneUnloading = true;
        }
    }

    public void ShowFinishBackground(bool isShown)
    {
        // SAFETY CHECK: Verify UI objects still exist before accessing
        if (FinishBackground == null)
        {
            Debug.LogWarning("FinishBackground has been destroyed - cannot show/hide");
            isSceneUnloading = true;
            return;
        }

        try
        {
            FinishBackground.SetActive(isShown);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"ShowFinishBackground failed: {ex.Message}");
            isSceneUnloading = true;
        }
    }

    public void ExitButtonInteract()
    {
        Debug.Log("Exit level");

        // Pastikan mode kamera kembali ke eksplorasi sebelum pindah scene
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.ReturnToExplorationMode();
        }

        // Kembali ke main menu tanpa men-trigger ContentSwitcher (tidak menyimpan completion).
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ResetTransitionData(); // pastikan flag trigger dimatikan
            // Coba transition normal terlebih dahulu
            if (SceneTransitionManager.Instance.IsTransitionInProgress())
            {
                Debug.LogWarning("Transition in progress detected during Exit - forcing immediate load to menu.");
                SceneTransitionManager.Instance.ForceTransitionImmediate("New Start Game Sandy");
            }
            else
            {
                SceneTransitionManager.Instance.TransitionToSceneDirect("New Start Game Sandy");
            }
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager not found - loading menu directly");
            SceneManager.LoadScene("New Start Game Sandy");
        }
    }

    /// <summary>
    /// Pastikan EventSystem dan GraphicRaycaster aktif sehingga tombol dapat diklik dengan mudah.
    /// </summary>
    private void EnsureUIInputReady()
    {
        // Ensure EventSystem exists
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem").AddComponent<EventSystem>();
            es.gameObject.AddComponent<StandaloneInputModule>();
            Debug.Log("[UIManager] Created missing EventSystem for UI input");
        }

        // Enable all GraphicRaycaster on parent canvases
        var raycasters = GetComponentsInParent<GraphicRaycaster>(true);
        foreach (var rc in raycasters)
        {
            if (rc != null && !rc.enabled)
            {
                rc.enabled = true;
            }
        }
    }

    public void ResumeButtonInteract()
    {
        Debug.Log("Exit level");
        ShowSetting(false);
    }

    public float GetAllProgressValue()
    {
        if (progressDirts == null || progressAssemble == null || progressDusts == null)
        {
            Debug.LogWarning("Progress bars not assigned on UIManager.");
            return 0f;
        }

        return (progressDirts.GetValue() + progressAssemble.GetValue() + progressDusts.GetValue()) / (3 + minusFactor);
    }

    /// <summary>
    /// Reset progress bars to zero at the start of a new session.
    /// </summary>
    public void ResetProgressBars()
    {
        if (progressDirts != null) progressDirts.SetValue(0);
        if (progressDusts != null) progressDusts.SetValue(0);
        if (progressAssemble != null) progressAssemble.SetValue(0);
        Debug.Log("[UIManager] Progress bars reset.");
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
            Debug.LogWarning("GamePlayManager.Instance not found! Trying alternative transition...");

            // FALLBACK: Try to trigger scene transition directly via SceneTransitionManager
            if (SceneTransitionManager.Instance != null)
            {
                Debug.Log("Using SceneTransitionManager fallback for button transition");

                // Use a default object type for transition
                ObjectType fallbackObjectType = ObjectType.ChinaCoin; // Default fallback

                // Try to detect object type from scene name
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
                if (sceneName.Contains("coin") || sceneName.Contains("china coin"))
                {
                    fallbackObjectType = ObjectType.ChinaCoin;
                }
                else if (sceneName.Contains("jar") || sceneName.Contains("china jar"))
                {
                    fallbackObjectType = ObjectType.ChinaJar;
                }
                else if (sceneName.Contains("horse"))
                {
                    fallbackObjectType = ObjectType.ChinaHorse;
                }
                else if (sceneName.Contains("kendin") || sceneName.Contains("indonesia"))
                {
                    fallbackObjectType = ObjectType.IndonesiaKendin;
                }
                else if (sceneName.Contains("winged") || sceneName.Contains("mesir"))
                {
                    fallbackObjectType = ObjectType.MesirWingedScared;
                }

                Debug.Log($"Using fallback ObjectType: {fallbackObjectType}");
                SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(fallbackObjectType, "New Start Game Sandy", "UIManager");
            }
            else
            {
                Debug.LogError("SceneTransitionManager.Instance also not found! Creating one...");

                // Last resort: Create SceneTransitionManager and use it
                GameObject stmGO = new GameObject("SceneTransitionManager");
                stmGO.AddComponent<SceneTransitionManager>();

                // Wait a frame then try again
                StartCoroutine(RetryTransitionAfterFrame());
            }
        }
    }

    /// <summary>
    /// Retry transition after SceneTransitionManager is created
    /// </summary>
    private System.Collections.IEnumerator RetryTransitionAfterFrame()
    {
        yield return null; // Wait one frame

        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log("SceneTransitionManager created successfully, triggering transition");
            SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(ObjectType.ChinaCoin, "New Start Game Sandy", "UIManager");
        }
        else
        {
            Debug.LogError("Failed to create SceneTransitionManager even after retry!");
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
        if (FinishButton != null) FinishButton.gameObject.SetActive(show);
        if (FinishButtonImage != null) FinishButtonImage.gameObject.SetActive(show);
        Debug.Log($"Finish button {(show ? "shown" : "hidden")}");
    }

    /// <summary>
    /// Manual method to trigger finish button (for testing)
    /// </summary>
    public void TestFinishButton()
    {
        Debug.Log("=== TESTING FINISH BUTTON ===");
        FinishButtonInteract();
    }

    /// <summary>
    /// Update artefact name label if assigned.
    /// </summary>
    public void SetArtefactName(string artefactName)
    {
        if (artefactNameText == null)
        {
            Debug.LogWarning("artefactNameText is not assigned on UIManager.");
            return;
        }

        artefactNameText.text = artefactName;
    }

    /// <summary>
    /// Show or hide individual progress bars.
    /// </summary>
    public void ShowProgress(ProgressType type, bool isShown)
    {
        switch (type)
        {
            case ProgressType.Dirt:
                progressDirtsGO.SetActive(isShown);
                minusFactor += isShown ? 1 : -1;
                break;
            case ProgressType.Dust:
                progressDustsGO.SetActive(isShown);
                minusFactor += isShown ? 1 : -1;
                break;
            case ProgressType.Assemble:
                progressAssembleGO.SetActive(isShown);
                minusFactor += isShown ? 1 : -1;
                break;
        }
    }

    /// <summary>
    /// Prepare UIManager for scene unload - stop all processing
    /// </summary>
    public void PrepareForSceneTransition()
    {
        Debug.Log("=== PREPARING UI MANAGER FOR SCENE TRANSITION ===");

        // Stop all Update() processing
        isSceneUnloading = true;

        // Stop all coroutines
        StopAllCoroutines();

        // Clear instance reference to prevent cross-scene access
        if (Instance == this)
        {
            Instance = null;
        }

        Debug.Log("UIManager prepared for scene unload");
    }

    private void OnDestroy()
    {
        // Clear instance when destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
