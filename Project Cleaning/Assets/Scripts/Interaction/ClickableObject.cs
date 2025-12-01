using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class ClickableObject : MonoBehaviour
{

    [Header("Audio")]
    [SerializeField] private AudioClip clickSound;

    [Header("Scene Navigation")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private bool canChangeScene = false;
    [SerializeField] private bool useTransitionAnimation = true;
    [SerializeField] private EasyTransition.TransitionSettings transitionSettings;

    [Header("Staged Scene Transition")]
    [Tooltip("Enable this to use a two-stage transition via an intermediary scene.")]
    [SerializeField] private bool useStagedTransition = false;
    [Tooltip("The name of the intermediary scene to load first.")]
    [SerializeField] private string intermediarySceneName = "SceneTransition";
    [Tooltip("The delay in seconds to wait in the intermediary scene.")]
    [SerializeField] private float intermediaryDelay = 3.0f;

    [Header("Text Popup")]
    [SerializeField] private GameObject popupTextGameObject;
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private float characterAnimationSpeed = 0.05f;
    [SerializeField] private float scaleAnimationDuration = 0.6f;
    [SerializeField] private bool autoHideAfterSeconds = true;
    [SerializeField] private float autoHideDelay = 3f;
    [SerializeField] private bool onlyShowInExplorationMode = true;

    [Header("Shake Animation")]
    [SerializeField] private bool enableShakeAnimation = true;
    [SerializeField] private float shakeIntensity = 0.05f;
    [SerializeField] private float shakeDuration = 3f;
    [SerializeField] private bool shakeOnlyWhenFocused = true;

    [Header("Object Identification")]
    [SerializeField] private ObjectType objectType = ObjectType.ChinaCoin;
    [SerializeField] private bool detectObjectOnClick = true;

    [Header("ContentSwitcher Integration")]
    [Tooltip("Drag GameObject with ContentSwitcher component here (REQUIRED for ContentSwitcher to work)")]
    [SerializeField] private GameObject contentSwitcherObject;

    [Header("Object State Changes")]
    [Tooltip("GameObjects that will be affected when ContentSwitcher completes")]
    [SerializeField] private GameObject[] objectsToChange; // Objects to modify after ContentSwitcher
    [SerializeField] private bool autoFindRelatedObjects = true;
    [Header("Unlock Requirements")]
    [SerializeField] private bool lockUntilPrerequisiteComplete = false;
    [SerializeField] private ObjectType prerequisiteObjectType = ObjectType.ChinaCoin;
    [SerializeField] private GameObject lockedVisual; // Optional alternate visual when locked
    [SerializeField] private GameObject unlockedVisual; // Normal visual when unlocked
    private bool subscribedToSaveEvents = false;
    private Coroutine waitForSaveSystemRoutine;

    // Public accessors for AdvancedInputManager
    public AudioClip ClickSound => clickSound;

    [Header("Events")]
    public UnityEvent OnObjectClicked;

    private bool isFocused = false;

    // Text popup components
    private bool isPopupVisible = false;
    private string fullTextContent;
    private Sequence currentTextSequence;

    // Shake animation components
    private Vector3 originalPosition;
    private Sequence currentShakeSequence;

    // ContentSwitcher integration
    private ContentSwitcher linkedContentSwitcher;
    private bool hasValidContentSwitcher = false;

    private void Awake()
    {
        SetupPopupImage();
        SetupShakeAnimation();

        SetupContentSwitcherDetection();
    }

    private void SetupPopupImage()
    {
        if (popupTextGameObject != null)
        {
            // Auto-find TextMeshPro component if not assigned
            if (textMeshPro == null)
            {
                textMeshPro = popupTextGameObject.GetComponent<TextMeshProUGUI>();
                if (textMeshPro == null)
                {
                    textMeshPro = popupTextGameObject.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (textMeshPro != null)
            {
                fullTextContent = textMeshPro.text;
                popupTextGameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("No TextMeshProUGUI component found in popup GameObject!");
            }
        }
        else
        {
            Debug.LogWarning("popupTextGameObject is null!");
        }
    }

    private void SetupShakeAnimation()
    {
        // Store the original position
        originalPosition = transform.localPosition;

        // Don't start shake animation automatically - only when focused/clicked
    }



    private void SetupContentSwitcherDetection()
    {
        if (contentSwitcherObject != null)
        {
            ValidateContentSwitcher();
        }
        else
        {
            hasValidContentSwitcher = false;
            linkedContentSwitcher = null;
        }

        if (autoFindRelatedObjects)
        {
            FindRelatedObjectsToChange();
        }
    }

    /// <summary>
    /// Called when object is clicked
    /// </summary>
    public void OnClick()
    {
        // Object type detection and logging
        if (detectObjectOnClick)
        {
            Debug.Log($"=== OBJECT CLICKED ===");
            Debug.Log($"Object Name: {gameObject.name}");
            Debug.Log($"Object Type: {objectType}");
            Debug.Log($"Chapter: {GetChapterFromObjectType()}");

            // NEW: Log completion status
            if (IsCompleted())
            {
                Debug.Log($"Status: ✅ COMPLETED (Replaying)");
            }
            else
            {
                Debug.Log($"Status: 🔥 NEW");
            }

            Debug.Log($"===================");
        }

        // If locked and prerequisite not met, block interaction
        if (lockUntilPrerequisiteComplete && !PrerequisiteCompleted())
        {
            Debug.LogWarning($"[{name}] Locked until {prerequisiteObjectType} completed.");
            ShowLockedVisual();
            return;
        }

        // CRITICAL FIX: Immediately save focus state the moment an object is clicked.
        // This prevents the focus data from being lost before a scene transition.
        if (SimpleCameraFocusRestore.Instance != null)
        {
            SimpleCameraFocusRestore.Instance.SaveCurrentFocus();
        }

        bool alreadyCompleted = IsCompleted();

        // Trigger the Unity Event first
        OnObjectClicked?.Invoke();

        // Show popup text (allow even during camera transitions)
        if (!alreadyCompleted)
        {
            ShowPopupText();
        }
        else
        {
            HidePopupText();
        }

        // Set focus state and start shake animation when clicked (if in exploration mode and enabled)
        if (enableShakeAnimation && shakeOnlyWhenFocused)
        {
            if (AdvancedInputManager.Instance != null && AdvancedInputManager.Instance.IsInExplorationMode())
            {
                isFocused = true;
                StartShakeAnimation();
            }
        }
    }


    /// <summary>
    /// Called when camera returns to overview (object loses focus)
    /// </summary>
    public void OnLoseFocus()
    {
        isFocused = false;

        // Stop shake animation when losing focus
        if (enableShakeAnimation && shakeOnlyWhenFocused)
        {
            StopShakeAnimation();
        }


    }


    /// <summary>
    /// Check if this object is currently camera-focused
    /// </summary>
    public bool IsFocused()
    {
        return isFocused;
    }

    /// <summary>
    /// Force focus state (useful for external control)
    /// </summary>
    public void SetFocusState(bool focused)
    {
        isFocused = focused;
        if (!focused)
        {
            OnLoseFocus();
        }
        else
        {
            // Start shake animation when gaining focus (if enabled and in exploration mode)
            if (enableShakeAnimation && shakeOnlyWhenFocused)
            {
                if (AdvancedInputManager.Instance != null && AdvancedInputManager.Instance.IsInExplorationMode())
                {
                    StartShakeAnimation();
                }
            }


        }
    }

    /// <summary>
    /// Get the target scene name for this clickable object
    /// </summary>
    public string GetSceneName()
    {
        return targetSceneName;
    }

    /// <summary>
    /// Check if this object can trigger a scene change
    /// </summary>
    public bool CanChangeScene()
    {
        // FIXED: Allow completed objects to be clicked again for replay
        // This enables users to replay completed levels/objects

        // Basic validation: must have scene name and be configured for scene change
        if (!canChangeScene || string.IsNullOrEmpty(targetSceneName))
        {
            return false;
        }

        // REMOVED: Completion status check - allow completed objects to be replayed
        // Original blocking code:
        // if (SaveSystem.Instance != null && IsCompleted())
        // {
        //     return false;
        // }

        return true;
    }

    /// <summary>
    /// Check if transition animation should be used
    /// </summary>
    public bool UseTransitionAnimation()
    {
        return useTransitionAnimation;
    }

    /// <summary>
    /// Get the Easy Transition settings for scene change
    /// </summary>
    public EasyTransition.TransitionSettings GetTransitionSettings()
    {
        return transitionSettings;
    }

    /// <summary>
    /// Check if this object should use a staged scene transition.
    /// </summary>
    public bool UseStagedTransition()
    {
        return useStagedTransition;
    }

    /// <summary>
    /// Get the name of the intermediary scene for a staged transition.
    /// </summary>
    public string GetIntermediarySceneName()
    {
        return intermediarySceneName;
    }

    /// <summary>
    /// Get the delay in the intermediary scene for a staged transition.
    /// </summary>
    public float GetIntermediaryDelay()
    {
        return intermediaryDelay;
    }



    /// <summary>
    /// Get the object type of this clickable object
    /// </summary>
    public ObjectType GetObjectType()
    {
        return objectType;
    }

    /// <summary>
    /// Set the object type of this clickable object
    /// </summary>
    public void SetObjectType(ObjectType newObjectType)
    {
        objectType = newObjectType;
    }

    /// <summary>
    /// Get the chapter type based on the object type
    /// </summary>
    public ChapterType GetChapterFromObjectType()
    {
        switch (objectType)
        {
            case ObjectType.ChinaCoin:
            case ObjectType.ChinaJar:
            case ObjectType.ChinaHorse:
                return ChapterType.China;
            case ObjectType.IndonesiaKendin:
                return ChapterType.Indonesia;
            case ObjectType.MesirWingedScared:
                return ChapterType.Mesir;
            default:
                return ChapterType.China;
        }
    }

    /// <summary>
    /// Check if this object belongs to a specific chapter
    /// </summary>
    public bool BelongsToChapter(ChapterType chapter)
    {
        return GetChapterFromObjectType() == chapter;
    }

    /// <summary>
    /// Get detailed object information as string
    /// </summary>
    public string GetObjectInfo()
    {
        return $"Object: {gameObject.name}, Type: {objectType}, Chapter: {GetChapterFromObjectType()}";
    }

    /// <summary>
    /// Check if this object is already completed (saved progress)
    /// Only works when SaveSystem is available (main scene)
    /// </summary>
    public bool IsCompleted()
    {
        // If SaveSystem not available (like in gameplay scene), assume not completed
        if (SaveSystem.Instance == null)
        {
            return false;
        }

        try
        {
            // Primary check: exact name + type
            if (SaveSystem.Instance.IsObjectCompleted(gameObject.name, objectType))
            {
                return true;
            }

            // Fallback: match by object type only (handles different GameObject names vs saved objectName)
            var saveData = SaveSystem.Instance.GetSaveData();
            foreach (var completed in saveData.completedObjects)
            {
                if (completed.objectType == objectType)
                {
                    return true;
                }
            }

            return false;
        }
        catch (System.Exception)
        {
            // If any error occurs, assume not completed
            return false;
        }
    }

    #region ContentSwitcher Integration Methods


    /// <summary>
    /// Validate manually assigned ContentSwitcher
    /// </summary>
    private void ValidateContentSwitcher()
    {
        Debug.Log($"=== VALIDATING CONTENT SWITCHER FOR {gameObject.name} ===");

        if (contentSwitcherObject == null)
        {
            Debug.LogError($"❌ ContentSwitcher Object is NULL for {gameObject.name}");
            hasValidContentSwitcher = false;
            linkedContentSwitcher = null;
            return;
        }

        Debug.Log($"✅ ContentSwitcher Object assigned: {contentSwitcherObject.name}");

        linkedContentSwitcher = contentSwitcherObject.GetComponent<ContentSwitcher>();

        if (linkedContentSwitcher != null)
        {
            Debug.Log($"✅ ContentSwitcher component found on {contentSwitcherObject.name}");
            Debug.Log($"   Chapter Type: {linkedContentSwitcher.GetChapterType()}");
            Debug.Log($"   Object Type: {linkedContentSwitcher.GetObjectType()}");
            hasValidContentSwitcher = true;
        }
        else
        {
            Debug.LogError($"❌ NO ContentSwitcher component found on {contentSwitcherObject.name}");
            Debug.LogError($"   Available components on {contentSwitcherObject.name}:");

            Component[] allComponents = contentSwitcherObject.GetComponents<Component>();
            foreach (Component comp in allComponents)
            {
                Debug.LogError($"   - {comp.GetType().Name}");
            }

            hasValidContentSwitcher = false;
        }

        Debug.Log($"=== VALIDATION RESULT: {(hasValidContentSwitcher ? "VALID" : "INVALID")} ===");
    }

    /// <summary>
    /// Find objects that might be affected when ContentSwitcher completes
    /// </summary>
    private void FindRelatedObjectsToChange()
    {
        if (objectsToChange == null || objectsToChange.Length == 0)
        {
            // Look for child objects that might need to change
            List<GameObject> foundObjects = new List<GameObject>();

            // Add self as potential object to change
            foundObjects.Add(gameObject);

            // Look for child objects with specific keywords
            string[] keywords = { "after", "changed", "completed", "revealed", "new" };

            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                string childName = child.name.ToLower();
                foreach (string keyword in keywords)
                {
                    if (childName.Contains(keyword))
                    {
                        foundObjects.Add(child.gameObject);
                        break;
                    }
                }
            }

            objectsToChange = foundObjects.ToArray();

        }
    }


    /// <summary>
    /// Get the linked ContentSwitcher
    /// </summary>
    public ContentSwitcher GetLinkedContentSwitcher()
    {
        return linkedContentSwitcher;
    }

    /// <summary>
    /// Check if this object has a valid ContentSwitcher linked
    /// </summary>
    public bool HasValidContentSwitcher()
    {
        Debug.Log($"=== CHECKING VALIDITY FOR {gameObject.name} ===");
        Debug.Log($"hasValidContentSwitcher: {hasValidContentSwitcher}");
        Debug.Log($"linkedContentSwitcher != null: {linkedContentSwitcher != null}");
        Debug.Log($"contentSwitcherObject != null: {contentSwitcherObject != null}");

        if (contentSwitcherObject != null)
        {
            Debug.Log($"Assigned ContentSwitcher Object: {contentSwitcherObject.name}");

            // Re-validate in case something changed
            var currentComponent = contentSwitcherObject.GetComponent<ContentSwitcher>();
            Debug.Log($"ContentSwitcher component currently exists: {currentComponent != null}");

            // FAILSAFE: Auto re-validation if state inconsistent
            if (!hasValidContentSwitcher && currentComponent != null)
            {
                Debug.LogWarning($"⚠️ Auto-fixing validation state for {gameObject.name}");
                linkedContentSwitcher = currentComponent;
                hasValidContentSwitcher = true;
            }
            else if (hasValidContentSwitcher && currentComponent == null)
            {
                Debug.LogWarning($"⚠️ ContentSwitcher component lost! Invalidating for {gameObject.name}");
                linkedContentSwitcher = null;
                hasValidContentSwitcher = false;
            }
        }
        else if (hasValidContentSwitcher)
        {
            // If ContentSwitcher object is null but we think it's valid, fix this
            Debug.LogWarning($"⚠️ ContentSwitcher Object is null! Invalidating for {gameObject.name}");
            hasValidContentSwitcher = false;
            linkedContentSwitcher = null;
        }

        bool isValid = hasValidContentSwitcher && linkedContentSwitcher != null;
        Debug.Log($"FINAL VALIDATION RESULT: {isValid}");

        return isValid;
    }

    /// <summary>
    /// Get the objects that should change when ContentSwitcher completes
    /// </summary>
    public GameObject[] GetObjectsToChange()
    {
        return objectsToChange;
    }

    /// <summary>
    /// Manually set the ContentSwitcher GameObject
    /// </summary>
    public void SetContentSwitcherObject(GameObject contentSwitcher)
    {
        contentSwitcherObject = contentSwitcher;
        ValidateContentSwitcher();
    }

    /// <summary>
    /// Manually set objects to change
    /// </summary>
    public void SetObjectsToChange(GameObject[] objects)
    {
        objectsToChange = objects;
    }

    /// <summary>
    /// Apply changes to all related objects (called after ContentSwitcher completes)
    /// </summary>
    public void ApplyContentSwitcherChanges()
    {
        if (objectsToChange == null || objectsToChange.Length == 0)
            return;

        foreach (GameObject obj in objectsToChange)
        {
            if (obj != null)
            {
                ApplyChangesToObject(obj);
            }
        }
    }

    /// <summary>
    /// Apply specific changes to an individual object
    /// </summary>
    private void ApplyChangesToObject(GameObject targetObj)
    {
        // 1. Enable/disable child objects
        Transform afterImage = targetObj.transform.Find("afterImage");
        if (afterImage != null)
        {
            afterImage.gameObject.SetActive(true);
        }

        // 2. Change material/color
        Renderer renderer = targetObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.green;
        }

        // 3. Trigger particle effects
        ParticleSystem particles = targetObj.GetComponent<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
        }

        // 4. Scale animation
        if (targetObj.transform != null)
        {
            targetObj.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 3, 1);
        }

        // 5. FIXED: Keep clickable functionality enabled for replay
        // Original: canChangeScene = false; (disabled replay)
        // NEW: Keep enabled so user can replay completed objects

        if (targetObj == gameObject)
        {
            Debug.Log($"✅ Object {gameObject.name} marked as completed but remains clickable for replay");
        }
    }

    /// <summary>
    /// Force re-setup ContentSwitcher detection (Inspector method)
    /// </summary>
    [ContextMenu("Re-Setup ContentSwitcher Detection")]
    public void ForceSetupContentSwitcher()
    {
        SetupContentSwitcherDetection();
    }

    /// <summary>
    /// Validate current ContentSwitcher setup (Inspector method)
    /// </summary>
    [ContextMenu("Validate ContentSwitcher Setup")]
    public void ValidateSetup()
    {
        Debug.Log($"=== MANUAL VALIDATION STARTED FOR {name} ===");

        SetupContentSwitcherDetection();

        if (hasValidContentSwitcher)
        {
            Debug.Log($"✅ ContentSwitcher setup is VALID for {name}");
            Debug.Log($"   Linked ContentSwitcher: {linkedContentSwitcher.name}");
            Debug.Log($"   Chapter Type: {linkedContentSwitcher.GetChapterType()}");
            Debug.Log($"   Object Type: {linkedContentSwitcher.GetObjectType()}");
        }
        else
        {
            Debug.LogError($"❌ ContentSwitcher setup is INVALID for {name}");

            if (contentSwitcherObject == null)
            {
                Debug.LogError("   Problem: No ContentSwitcher Object assigned!");
                Debug.LogError("   Solution: Drag a GameObject with ContentSwitcher component to Content Switcher Object field");
            }
            else
            {
                var component = contentSwitcherObject.GetComponent<ContentSwitcher>();
                if (component == null)
                {
                    Debug.LogError($"   Problem: Object '{contentSwitcherObject.name}' has no ContentSwitcher component!");
                    Debug.LogError("   Solution: Add ContentSwitcher component to the assigned GameObject");
                }
            }
        }

        Debug.Log($"=== MANUAL VALIDATION COMPLETED ===");
    }

    /// <summary>
    /// Force refresh ContentSwitcher validation (Inspector method)
    /// </summary>
    [ContextMenu("Force Refresh ContentSwitcher")]
    public void ForceRefreshContentSwitcher()
    {
        Debug.Log($"=== FORCE REFRESHING CONTENT SWITCHER FOR {name} ===");

        // Reset validation state
        hasValidContentSwitcher = false;
        linkedContentSwitcher = null;

        // Re-run validation
        ValidateContentSwitcher();

        Debug.Log($"=== FORCE REFRESH COMPLETED ===");
    }

    /// <summary>
    /// Test apply changes without ContentSwitcher trigger (Inspector method)
    /// </summary>
    [ContextMenu("Test Apply Changes")]
    public void TestApplyChanges()
    {
        ApplyContentSwitcherChanges();
        Debug.Log($"Applied ContentSwitcher changes for {name}");
    }

    #endregion

    #region Lock / Unlock prerequisite

    private bool PrerequisiteCompleted()
    {
        if (!lockUntilPrerequisiteComplete)
        {
            return true;
        }

        if (SaveSystem.Instance == null)
        {
            // If no SaveSystem (e.g., gameplay scene), default to locked to prevent premature unlocks
            return false;
        }

        // Check completion for required object type
        var saveData = SaveSystem.Instance.GetSaveData();
        foreach (var completed in saveData.completedObjects)
        {
            if (completed.objectType == prerequisiteObjectType)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowLockedVisual()
    {
        if (lockedVisual != null) lockedVisual.SetActive(true);
        if (unlockedVisual != null) unlockedVisual.SetActive(false);
    }

    private void ShowUnlockedVisual()
    {
        if (lockedVisual != null) lockedVisual.SetActive(false);
        if (unlockedVisual != null) unlockedVisual.SetActive(true);
    }

    private void UpdateLockVisual()
    {
        if (SaveSystem.Instance == null)
        {
            return; // Don't change visuals until save data is available
        }

        // If this object itself is already completed, always show the unlocked state
        // to avoid regressions when prerequisites are misread after app restart.
        if (IsCompleted())
        {
            ShowUnlockedVisual();
            return;
        }

        if (PrerequisiteCompleted())
        {
            ShowUnlockedVisual();
        }
        else
        {
            ShowLockedVisual();
        }
    }

    private void OnEnable()
    {
        // Ensure SaveSystem exists so completion/lock checks work on cold app launch
        EnsureSaveSystem();

        if (SaveSystem.Instance == null)
        {
            if (waitForSaveSystemRoutine == null)
            {
                waitForSaveSystemRoutine = StartCoroutine(WaitForSaveSystemThenRefreshLock());
            }
            return; // Keep current inspector state until save data is ready
        }

        // When SaveSystem isn't ready yet (happens on cold launch), wait for it before evaluating locks
        if (lockUntilPrerequisiteComplete && SaveSystem.Instance == null)
        {
            if (lockedVisual != null) lockedVisual.SetActive(true); // show locked overlay without disabling this object

            if (waitForSaveSystemRoutine == null)
            {
                waitForSaveSystemRoutine = StartCoroutine(WaitForSaveSystemThenRefreshLock());
            }
            return;
        }

        UpdateLockVisual();
        SubscribeToSaveSystemEvents();


        // In case SaveSystem initializes a frame later, run a delayed refresh
        StartCoroutine(RefreshLockVisualNextFrame());
    }

    private void EnsureSaveSystem()
    {
        if (SaveSystem.Instance != null)
        {
            return;
        }

        var existing = GameObject.FindObjectOfType<SaveSystem>();
        if (existing != null)
        {
            return;
        }

        GameObject go = new GameObject("SaveSystem");
        go.AddComponent<SaveSystem>();
        DontDestroyOnLoad(go);
    }

    private void OnDisable()
    {
        if (waitForSaveSystemRoutine != null)
        {
            StopCoroutine(waitForSaveSystemRoutine);
            waitForSaveSystemRoutine = null;
        }

        if (SaveSystem.Instance != null && subscribedToSaveEvents)
        {
            SaveSystem.Instance.OnDataLoaded -= HandleSaveDataChanged;
            SaveSystem.Instance.OnDataSaved -= HandleSaveDataChanged;
            subscribedToSaveEvents = false;
        }
    }

    private void HandleSaveDataChanged(SaveData _)
    {
        UpdateLockVisual();
    }

    private void SubscribeToSaveSystemEvents()
    {
        if (SaveSystem.Instance != null && !subscribedToSaveEvents)
        {
            SaveSystem.Instance.OnDataLoaded += HandleSaveDataChanged;
            SaveSystem.Instance.OnDataSaved += HandleSaveDataChanged;
            subscribedToSaveEvents = true;
        }
    }

    private System.Collections.IEnumerator WaitForSaveSystemThenRefreshLock()
    {
        // Wait until SaveSystem singleton is available
        while (SaveSystem.Instance == null)
        {
            yield return null;
        }

        waitForSaveSystemRoutine = null;

        SubscribeToSaveSystemEvents();
        UpdateLockVisual();
        StartCoroutine(RefreshLockVisualNextFrame());
    }

    private System.Collections.IEnumerator RefreshLockVisualNextFrame()
    {
        yield return null;
        UpdateLockVisual();
    }

    #endregion

    /// <summary>
    /// Show popup text with character-by-character animation (Fixed version)
    /// </summary>
    public void ShowPopupText()
    {
        // Jangan tampilkan popup jika sudah completed (hanya bekerja saat SaveSystem tersedia, mis. di menu)
        if (SaveSystem.Instance != null && IsCompleted())
        {
            HidePopupText();
            return;
        }

        // Auto-find components if not set
        if (textMeshPro == null)
        {
            textMeshPro = popupTextGameObject?.GetComponent<TextMeshProUGUI>();
            if (textMeshPro == null)
                textMeshPro = popupTextGameObject?.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (popupTextGameObject == null || textMeshPro == null)
        {
            Debug.LogError($"TextMeshPro setup failed! GameObject: {popupTextGameObject != null}, TextMeshPro: {textMeshPro != null}");
            return;
        }

        if (string.IsNullOrEmpty(fullTextContent))
            fullTextContent = textMeshPro.text;

        if (string.IsNullOrEmpty(fullTextContent))
        {
            Debug.LogError("No text content to display!");
            return;
        }

        if (isPopupVisible) return;

        // Check exploration mode
        if (onlyShowInExplorationMode && AdvancedInputManager.Instance != null)
        {
            if (!AdvancedInputManager.Instance.IsInExplorationMode())
                return;
        }

        Debug.Log($"Starting text animation: '{fullTextContent}'");

        isPopupVisible = true;
        popupTextGameObject.SetActive(true);

        // Kill existing animation
        if (currentTextSequence != null && currentTextSequence.IsActive())
            currentTextSequence.Kill();

        // Set full text, start with no visible characters
        textMeshPro.text = fullTextContent;
        textMeshPro.maxVisibleCharacters = 0;

        // Character-by-character animation
        currentTextSequence = DOTween.Sequence();
        currentTextSequence.Append(DOTween.To(() => textMeshPro.maxVisibleCharacters,
                                             x => textMeshPro.maxVisibleCharacters = x,
                                             fullTextContent.Length,
                                             fullTextContent.Length * characterAnimationSpeed)
                                           .SetEase(Ease.Linear));

        // Auto hide (commented out to keep text visible)
        // if (autoHideAfterSeconds)
        // {
        //     currentTextSequence.AppendInterval(autoHideDelay);
        //     currentTextSequence.AppendCallback(HidePopupText);
        // }

        Debug.Log("Text animation started!");
    }

    /// <summary>
    /// Hide popup text (Simple version)
    /// </summary>
    public void HidePopupText()
    {
        if (popupTextGameObject == null || !isPopupVisible) return;

        Debug.Log("Hiding text popup");

        // Kill any existing animation
        if (currentTextSequence != null && currentTextSequence.IsActive())
        {
            currentTextSequence.Kill();
        }

        // Simple hide
        isPopupVisible = false;
        textMeshPro.maxVisibleCharacters = 0;
        popupTextGameObject.SetActive(false);
    }

    /// <summary>
    /// Toggle popup visibility
    /// </summary>
    public void TogglePopupText()
    {
        if (isPopupVisible)
            HidePopupText();
        else
            ShowPopupText();
    }

    /// <summary>
    /// Test popup manually (for debugging)
    /// </summary>
    [System.Obsolete("For debugging only")]
    public void TestPopupManual()
    {
        Debug.Log("=== MANUAL TEST TEXT POPUP ===");
        Debug.Log($"Popup GameObject: {(popupTextGameObject != null ? popupTextGameObject.name : "NULL")}");
        Debug.Log($"TextMeshPro: {(textMeshPro != null ? "Found" : "NULL")}");
        Debug.Log($"Text Content: '{fullTextContent}'");
        Debug.Log($"Is Popup Visible: {isPopupVisible}");

        ShowPopupText();
    }

    /// <summary>
    /// Start the slow shake animation for the clickable object
    /// </summary>
    public void StartShakeAnimation()
    {
        if (!enableShakeAnimation) return;

        // Only start shake if focused and in exploration mode (when shakeOnlyWhenFocused is enabled)
        if (shakeOnlyWhenFocused)
        {
            if (!isFocused) return;
            if (AdvancedInputManager.Instance != null && !AdvancedInputManager.Instance.IsInExplorationMode()) return;
        }

        // Kill existing shake animation
        if (currentShakeSequence != null && currentShakeSequence.IsActive())
            currentShakeSequence.Kill();

        // Create a smooth continuous shake animation using a single tween
        // This creates a seamless up-down motion without any stops
        var shakeTween = transform.DOLocalMoveY(originalPosition.y + shakeIntensity, shakeDuration / 2)
                                 .SetEase(Ease.InOutSine)
                                 .SetLoops(-1, LoopType.Yoyo);

        // Wrap in sequence for proper cleanup
        currentShakeSequence = DOTween.Sequence();
        currentShakeSequence.Append(shakeTween);
    }

    /// <summary>
    /// Stop the shake animation and return to original position
    /// </summary>
    public void StopShakeAnimation()
    {
        if (currentShakeSequence != null && currentShakeSequence.IsActive())
        {
            currentShakeSequence.Kill();
        }

        // Return to original position
        transform.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Toggle shake animation on/off
    /// </summary>
    public void ToggleShakeAnimation()
    {
        if (currentShakeSequence != null && currentShakeSequence.IsActive())
            StopShakeAnimation();
        else
            StartShakeAnimation();
    }

    /// <summary>
    /// Clean up current scene before transitioning to new scene
    /// Called before scene change to free up memory and resources
    /// </summary>
    public static void CleanupCurrentScene()
    {
        Debug.Log("=== CLEANING UP CURRENT SCENE ===");

        // 1. Stop all DOTween animations
        DOTween.KillAll();

        // 2. Clean up all ClickableObjects
        ClickableObject[] allClickables = FindObjectsOfType<ClickableObject>();
        foreach (ClickableObject clickable in allClickables)
        {
            clickable.CleanupObject();
        }

        // 3. Clean up particle systems
        ParticleSystem[] allParticles = FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem particles in allParticles)
        {
            particles.Stop();
            particles.Clear();
        }

        // 4. Clean up audio sources
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audio in allAudioSources)
        {
            audio.Stop();
        }

        // 5. Force garbage collection
        System.GC.Collect();

        Debug.Log($"Scene cleanup completed. Cleaned {allClickables.Length} clickables, {allParticles.Length} particles, {allAudioSources.Length} audio sources");
    }

    /// <summary>
    /// Cleanup this specific ClickableObject
    /// </summary>
    private void CleanupObject()
    {
        // Stop animations
        if (currentTextSequence != null && currentTextSequence.IsActive())
        {
            currentTextSequence.Kill();
        }

        if (currentShakeSequence != null && currentShakeSequence.IsActive())
        {
            currentShakeSequence.Kill();
        }

        // Hide popup
        if (isPopupVisible)
        {
            HidePopupText();
        }

        // Stop shake animation
        StopShakeAnimation();



        // Clear focus
        isFocused = false;
    }

    private void OnDestroy()
    {
        CleanupObject();
    }

    // NOTE: Unity's built-in mouse events are disabled to prevent conflicts with AdvancedInputManager
    // The AdvancedInputManager handles all input and calls OnClick() directly when needed

    // Optional: Unity Event triggers for Inspector setup (DISABLED - using AdvancedInputManager instead)
    /*
    private void OnMouseEnter()
    {
        if (enableHoverEffect)
        {
            OnHover();
        }
    }

    private void OnMouseExit()
    {
        if (enableHoverEffect)
        {
            OnUnhover();
        }
    }

    private void OnMouseDown()
    {
        OnClick();
    }
    */
}
