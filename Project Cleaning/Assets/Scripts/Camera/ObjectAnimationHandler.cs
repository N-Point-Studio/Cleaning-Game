using UnityEngine;
using DG.Tweening;
using System;

/// <summary>
/// Handles smooth animations for bringing objects to camera and returning them
/// </summary>
public class ObjectAnimationHandler : MonoBehaviour
{
    [Header("Animation Settings")]
    public Ease moveToCloseUpEase = Ease.OutBack;
    public Ease returnEase = Ease.InQuad;
    public float punchScaleAmount = 0.1f;
    public float punchDuration = 0.3f;

    // Animation parameters (set by manager)
    private float distanceFromCamera = 2f;
    private Vector3 positionOffset = Vector3.zero;
    private float scaleMultiplier = 1.0f;
    private float animationDuration = 1f;

    // Storage for object's original state
    private struct ObjectState
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public Transform parent;
    }

    private ObjectState originalState;
    private Transform currentAnimatingObject;

    /// <summary>
    /// Setup animation handler with parameters
    /// </summary>
    public void Setup(float distance, Vector3 offset, float scale, float duration)
    {
        distanceFromCamera = distance;
        positionOffset = offset;
        scaleMultiplier = scale;
        animationDuration = duration;

        Debug.Log("Object animation handler initialized");
    }

    

    /// <summary>
    /// Animate object to close-up position
    /// </summary>
    public void AnimateToCloseUp(Transform targetObject, Action onComplete = null)
    {
        if (targetObject == null) return;

        currentAnimatingObject = targetObject;

        // Store original state
        StoreOriginalState(targetObject);

        // Calculate close-up position using the manager's method that supports dynamic distance
        Vector3 closeUpPosition = CalculateCloseUpPosition(targetObject);

        Debug.Log($"Animating {targetObject.name} to close-up position: {closeUpPosition}");

        // Create animation sequence
        Sequence animationSequence = DOTween.Sequence();

        // Move to close-up position
        animationSequence.Append(
            targetObject.DOMove(closeUpPosition, animationDuration)
                .SetEase(moveToCloseUpEase)
        );

        // Keep original scale (no scaling during inspection)
        // Note: scaleMultiplier is ignored to maintain object's original size
        animationSequence.Join(
            targetObject.DOScale(originalState.scale, animationDuration)
                .SetEase(moveToCloseUpEase)
        );

        // Don't force rotation - keep object's current rotation
        // This fixes the "wrong rotation" issue

        // Add punch effect when animation completes
        animationSequence.AppendCallback(() =>
        {
            if (targetObject != null)
            {
                targetObject.DOPunchScale(Vector3.one * punchScaleAmount, punchDuration, 5);
            }
        });

        // Call completion callback
        animationSequence.OnComplete(() =>
        {
            currentAnimatingObject = null;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Animate object back to original position
    /// </summary>
    public void AnimateFromCloseUp(Transform targetObject, Action onComplete = null)
    {
        if (targetObject == null) return;

        Debug.Log($"Returning {targetObject.name} to original position: {originalState.position}");

        // Create return sequence
        Sequence returnSequence = DOTween.Sequence();

        // Return to original position
        returnSequence.Append(
            targetObject.DOMove(originalState.position, animationDuration * 0.6f)
                .SetEase(returnEase)
        );

        // Return to original scale
        returnSequence.Join(
            targetObject.DOScale(originalState.scale, animationDuration * 0.6f)
                .SetEase(returnEase)
        );

        // Return to original rotation
        returnSequence.Join(
            targetObject.DORotate(originalState.rotation, animationDuration * 0.6f)
                .SetEase(returnEase)
        );

        // Restore original parent if it had one
        if (originalState.parent != null)
        {
            returnSequence.AppendCallback(() =>
            {
                targetObject.SetParent(originalState.parent);
            });
        }

        // Call completion callback
        returnSequence.OnComplete(() =>
        {
            currentAnimatingObject = null;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Immediately return the object to its stored original state
    /// </summary>
    public void ReturnToOriginalInstant(Transform targetObject, Action onComplete = null)
    {
        if (targetObject == null) return;

        if (currentAnimatingObject == targetObject)
        {
            currentAnimatingObject = null;
        }

        targetObject.SetParent(originalState.parent);
        targetObject.position = originalState.position;
        targetObject.rotation = Quaternion.Euler(originalState.rotation);
        targetObject.localScale = originalState.scale;

        onComplete?.Invoke();
    }

    /// <summary>
    /// Store the object's original transform state
    /// </summary>
    void StoreOriginalState(Transform targetObject)
    {
        originalState = new ObjectState
        {
            position = targetObject.position,
            rotation = targetObject.eulerAngles,
            scale = targetObject.localScale,
            parent = targetObject.parent
        };

        LogStoredOriginalState(targetObject);
    }

    /// <summary>
    /// Emit debug information about the cached original transform values
    /// </summary>
    void LogStoredOriginalState(Transform targetObject)
    {
        if (targetObject == null)
            return;

        string parentName = originalState.parent != null ? originalState.parent.name : "<None>";
        Debug.Log($"🗂️ Cached original transform for {targetObject.name}");
        Debug.Log($"    Position: {originalState.position}");
        Debug.Log($"    Rotation (Euler): {originalState.rotation}");
        Debug.Log($"    Local Scale: {originalState.scale}");
        Debug.Log($"    Parent: {parentName}");
    }

    /// <summary>
    /// Calculate where the object should appear for close-up
    /// </summary>
    Vector3 CalculateCloseUpPosition(Transform targetObject = null)
    {
        // Try to get position from ObjectCloseUpManager for dynamic distance calculation
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null)
        {
            return closeUpManager.GetCloseUpPosition(targetObject);
        }

        // Fallback to basic calculation
        Camera cam = Camera.main;
        return cam.transform.position +
               cam.transform.forward * distanceFromCamera +
               positionOffset;
    }

    /// <summary>
    /// Stop any currently running animations
    /// </summary>
    public void StopCurrentAnimation()
    {
        if (currentAnimatingObject != null)
        {
            currentAnimatingObject.DOKill();
            currentAnimatingObject = null;
        }
    }

    

    // Public properties
    public bool IsAnimating => currentAnimatingObject != null;
    public Transform CurrentAnimatingObject => currentAnimatingObject;
    public float AnimationDuration => animationDuration;

    void OnDestroy()
    {
        // Clean up any running animations
        StopCurrentAnimation();
    }
}
