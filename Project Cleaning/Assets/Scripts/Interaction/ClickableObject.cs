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

    [Header("Inspectable Object")]
    [SerializeField] private bool isInspectable = true;

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

    // Inspectable components
    private InspectableJar inspectableJar;

    // ContentSwitcher integration
    private ContentSwitcher linkedContentSwitcher;
    private bool hasValidContentSwitcher = false;

    private void Awake()
    {
        SetupPopupImage();
        SetupShakeAnimation();
        SetupInspectableObject();
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

    private void SetupInspectableObject()
    {
        if (isInspectable)
        {
            // Get or add InspectableJar component
            inspectableJar = GetComponent<InspectableJar>();
            if (inspectableJar == null)
            {
                inspectableJar = gameObject.AddComponent<InspectableJar>();
            }

            // Disable inspectable functionality initially - only enable when focused in zoom mode
            inspectableJar.enabled = false;
        }
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
            Debug.Log($"===================");
        }

        // Trigger the Unity Event first
        OnObjectClicked?.Invoke();

        // Show popup text (allow even during camera transitions)
        ShowPopupText();

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

        // Disable inspectable functionality when losing focus
        if (isInspectable && inspectableJar != null)
        {
            inspectableJar.enabled = false;
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

            // Enable inspectable functionality when entering zoom mode
            if (isInspectable && inspectableJar != null && AdvancedInputManager.Instance != null)
            {
                if (AdvancedInputManager.Instance.IsInZoomMode())
                {
                    inspectableJar.enabled = true;
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
        return canChangeScene && !string.IsNullOrEmpty(targetSceneName);
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
    /// Check if this object can be inspected with pinch/zoom gestures
    /// </summary>
    public bool IsInspectable()
    {
        return isInspectable;
    }

    /// <summary>
    /// Enable or disable inspectable functionality
    /// </summary>
    public void SetInspectableEnabled(bool enabled)
    {
        if (isInspectable && inspectableJar != null)
        {
            inspectableJar.enabled = enabled;
        }
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

    #region ContentSwitcher Integration Methods


    /// <summary>
    /// Validate manually assigned ContentSwitcher
    /// </summary>
    private void ValidateContentSwitcher()
    {
        if (contentSwitcherObject == null)
        {
            hasValidContentSwitcher = false;
            linkedContentSwitcher = null;
            return;
        }

        linkedContentSwitcher = contentSwitcherObject.GetComponent<ContentSwitcher>();
        hasValidContentSwitcher = linkedContentSwitcher != null;
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
        return hasValidContentSwitcher && linkedContentSwitcher != null;
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

        // 5. Disable clickable functionality if this is the clicked object
        if (targetObj == gameObject)
        {
            canChangeScene = false;
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
        SetupContentSwitcherDetection();

        if (hasValidContentSwitcher)
        {
            Debug.Log($"✅ ContentSwitcher setup is VALID for {name}");
        }
        else
        {
            Debug.LogError($"❌ ContentSwitcher setup is INVALID for {name}");
        }
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

    /// <summary>
    /// Show popup text with character-by-character animation (Fixed version)
    /// </summary>
    public void ShowPopupText()
    {
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

    private void OnDestroy()
    {
        // Clean up DOTween animations
        if (currentTextSequence != null && currentTextSequence.IsActive())
        {
            currentTextSequence.Kill();
        }

        if (currentShakeSequence != null && currentShakeSequence.IsActive())
        {
            currentShakeSequence.Kill();
        }
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