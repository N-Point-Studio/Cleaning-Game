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
    public Ease returnEase = Ease.OutQuad;
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
    /// Update animation settings at runtime
    /// </summary>
    public void UpdateSettings(float distance, Vector3 offset, float scale)
    {
        distanceFromCamera = distance;
        positionOffset = offset;
        scaleMultiplier = scale;
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

        // Calculate close-up position
        Vector3 closeUpPosition = CalculateCloseUpPosition();

        Debug.Log($"Animating {targetObject.name} to close-up position: {closeUpPosition}");

        // Create animation sequence
        Sequence animationSequence = DOTween.Sequence();

        // Move to close-up position
        animationSequence.Append(
            targetObject.DOMove(closeUpPosition, animationDuration)
                .SetEase(moveToCloseUpEase)
        );

        // Scale up for better visibility
        animationSequence.Join(
            targetObject.DOScale(originalState.scale * scaleMultiplier, animationDuration)
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
        animationSequence.OnComplete(() => onComplete?.Invoke());
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
            targetObject.DOMove(originalState.position, animationDuration)
                .SetEase(returnEase)
        );

        // Return to original scale
        returnSequence.Join(
            targetObject.DOScale(originalState.scale, animationDuration)
                .SetEase(returnEase)
        );

        // Return to original rotation
        returnSequence.Join(
            targetObject.DORotate(originalState.rotation, animationDuration)
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
    }

    /// <summary>
    /// Calculate where the object should appear for close-up
    /// </summary>
    Vector3 CalculateCloseUpPosition()
    {
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

    /// <summary>
    /// Stop only rotation animations while keeping position/scale animations
    /// </summary>
    public void StopRotationAnimations()
    {
        if (currentAnimatingObject != null)
        {
            // Kill only rotation tweens, keep position and scale
            currentAnimatingObject.DOKill(false);
            DOTween.Kill(currentAnimatingObject, false);

            Debug.Log($"Stopped rotation animations for {currentAnimatingObject.name}");
        }
    }

    /// <summary>
    /// Enable manual rotation mode - stops conflicting animations
    /// </summary>
    public void EnableManualRotation(Transform targetObject)
    {
        if (targetObject != null)
        {
            // Kill any rotation tweens on this object
            targetObject.DOKill(false);
            Debug.Log($"Enabled manual rotation for {targetObject.name}");
        }
    }

    /// <summary>
    /// Set animation duration at runtime
    /// </summary>
    public void SetAnimationDuration(float duration)
    {
        animationDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// Set scale multiplier for close-up view
    /// </summary>
    public void SetScaleMultiplier(float multiplier)
    {
        scaleMultiplier = Mathf.Max(0.1f, multiplier);
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