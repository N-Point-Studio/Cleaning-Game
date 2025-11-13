using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ClickableObject : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private bool enableHoverEffect = true;
    [SerializeField] private bool enableClickEffect = true;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    [Header("Events")]
    public UnityEvent OnObjectClicked;
    public UnityEvent OnObjectHovered;
    public UnityEvent OnObjectUnhovered;

    private Renderer objRenderer;
    private AudioSource audioSource;
    private bool isHovered = false;
    private bool isFocused = false;

    private void Awake()
    {
        // Get components
        objRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        // Store normal material if not set
        if (normalMaterial == null && objRenderer != null)
        {
            normalMaterial = objRenderer.material;
        }

        // Ensure collider is set to trigger for hover detection
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            // Add a trigger collider for hover detection if main collider isn't trigger
            GameObject triggerObj = new GameObject("HoverTrigger");
            triggerObj.transform.SetParent(transform);
            triggerObj.transform.localPosition = Vector3.zero;
            triggerObj.transform.localRotation = Quaternion.identity;
            triggerObj.transform.localScale = Vector3.one * 1.1f; // Slightly larger for easier hovering

            Collider triggerCol = triggerObj.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
        }
    }

    /// <summary>
    /// Called when object is clicked
    /// </summary>
    public void OnClick()
    {
        if (TopDownCameraController.Instance.IsTransitioning()) return;

        // Visual feedback
        if (enableClickEffect)
        {
            StartClickEffect();
        }

        // Audio feedback
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // Focus camera on this object
        TopDownCameraController.Instance.SetFocusTarget(transform);
        TopDownCameraController.Instance.SwitchState(TopDownCameraController.Instance.focusState);

        // Mark as focused
        isFocused = true;

        // Trigger custom events
        OnObjectClicked?.Invoke();

        Debug.Log($"Clicked on: {gameObject.name}");
    }

    /// <summary>
    /// Called when mouse hovers over object
    /// </summary>
    public void OnHover()
    {
        if (isHovered || isFocused) return;

        isHovered = true;

        // Visual feedback
        if (enableHoverEffect && highlightMaterial != null && objRenderer != null)
        {
            objRenderer.material = highlightMaterial;
        }

        // Audio feedback
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }

        // Trigger custom events
        OnObjectHovered?.Invoke();
    }

    /// <summary>
    /// Called when mouse stops hovering over object
    /// </summary>
    public void OnUnhover()
    {
        if (!isHovered || isFocused) return;

        isHovered = false;

        // Restore normal material
        if (objRenderer != null && normalMaterial != null)
        {
            objRenderer.material = normalMaterial;
        }

        // Trigger custom events
        OnObjectUnhovered?.Invoke();
    }

    /// <summary>
    /// Called when camera returns to overview (object loses focus)
    /// </summary>
    public void OnLoseFocus()
    {
        isFocused = false;
        isHovered = false;

        // Restore normal material
        if (objRenderer != null && normalMaterial != null)
        {
            objRenderer.material = normalMaterial;
        }
    }

    /// <summary>
    /// Visual effect for click feedback
    /// </summary>
    private void StartClickEffect()
    {
        // Simple scale pulse effect using Unity's built-in animation
        StartCoroutine(ScalePulseEffect());
    }

    /// <summary>
    /// Scale pulse coroutine for click feedback
    /// </summary>
    private IEnumerator ScalePulseEffect()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        float duration = 0.1f;

        // Scale up
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;

        // Scale back down
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
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
    }

    // Optional: Unity Event triggers for Inspector setup
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
}