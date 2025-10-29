using UnityEngine;

/// <summary>
/// Makes jar pieces inspectable - they come to camera when clicked
/// </summary>
public class InspectableJar : MonoBehaviour, IInspectable
{
    [Header("Inspection Settings")]
    public float inspectionScale = 1.5f;       // How much bigger when inspecting
    public bool canBeInspected = true;

    [Header("Visual Feedback")]
    public GameObject highlightEffect;
    public Color inspectionColor = Color.cyan;
    public Material inspectionMaterial;         // Optional special material during inspection

    // Private fields
    private Renderer objectRenderer;
    private Material originalMaterial;
    private Color originalColor;
    private bool isCurrentlyInspected = false;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
            originalColor = objectRenderer.material.color;
        }
    }

    #region IInspectable Implementation

    public bool CanBeInspected()
    {
        return canBeInspected && gameObject.activeInHierarchy && !isCurrentlyInspected;
    }

    public float GetIdealInspectionScale()
    {
        return inspectionScale;
    }

    public void OnInspectionStart()
    {
        isCurrentlyInspected = true;

        Debug.Log($"Started inspecting jar: {gameObject.name}");

        // Enable highlight effect
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(true);
        }

        // Change material or color during inspection
        if (objectRenderer != null)
        {
            if (inspectionMaterial != null)
            {
                objectRenderer.material = inspectionMaterial;
            }
            else
            {
                objectRenderer.material.color = inspectionColor;
            }
        }

        // Optional: Add particle effects, sounds, etc.
        PlayInspectionStartEffects();
    }

    public void OnInspectionEnd()
    {
        isCurrentlyInspected = false;

        Debug.Log($"Stopped inspecting jar: {gameObject.name}");

        // Disable highlight effect
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }

        // Restore original material/color
        if (objectRenderer != null)
        {
            if (originalMaterial != null)
            {
                objectRenderer.material = originalMaterial;
            }
            else
            {
                objectRenderer.material.color = originalColor;
            }
        }

        // Optional: Add exit effects
        PlayInspectionEndEffects();
    }

    public Transform GetInspectionTarget()
    {
        return transform;
    }

    #endregion

    /// <summary>
    /// Play effects when inspection starts
    /// </summary>
    private void PlayInspectionStartEffects()
    {
        // Add particle effects, sound, etc. here
        // Example: Play a "woosh" sound when object comes to camera
    }

    /// <summary>
    /// Play effects when inspection ends
    /// </summary>
    private void PlayInspectionEndEffects()
    {
        // Add particle effects, sound, etc. here
    }

    /// <summary>
    /// Enable/disable inspection for this jar
    /// </summary>
    public void SetInspectable(bool inspectable)
    {
        canBeInspected = inspectable;
    }

    /// <summary>
    /// Set the scale used during inspection
    /// </summary>
    public void SetInspectionScale(float scale)
    {
        inspectionScale = Mathf.Max(0.1f, scale);
    }

    /// <summary>
    /// Check if this jar is currently being inspected
    /// </summary>
    public bool IsBeingInspected()
    {
        return isCurrentlyInspected;
    }

    void OnDrawGizmosSelected()
    {
        // Draw inspection scale preview
        Gizmos.color = canBeInspected ? Color.green : Color.red;

        // Calculate scaled bounds
        Bounds bounds = GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);
        Vector3 scaledSize = bounds.size * inspectionScale;

        Gizmos.DrawWireCube(transform.position, scaledSize);

        // Draw inspection status
        if (isCurrentlyInspected)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}