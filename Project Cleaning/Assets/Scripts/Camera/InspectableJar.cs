using UnityEngine;

/// <summary>
/// Makes jar pieces inspectable - they come to camera when clicked
/// </summary>
public class InspectableJar : MonoBehaviour, IInspectable
{
    [Header("Inspection Settings")]
    public float inspectionScale = 1.5f;       // How much bigger when inspecting
    public bool canBeInspected = true;

    [Header("Size-Aware Inspection")]
    [Tooltip("Use different inspection scale for assembled jar vs individual pieces")]
    public bool useContextualScale = true;
    [Tooltip("Scale for individual jar pieces")]
    public float pieceInspectionScale = 1.5f;
    [Tooltip("Scale for assembled jar (typically smaller since jar is larger)")]
    public float assembledJarInspectionScale = 1.0f;

    [Header("Visual Feedback")]
    public GameObject highlightEffect;
    public Color inspectionColor = Color.cyan;
    public Material inspectionMaterial;         // Optional special material during inspection

    [Header("Outline System")]
    public float outlineActivationDistance = 3f; // Distance to activate outline

    // Private fields
    private Renderer objectRenderer;
    private Material originalMaterial;
    private Material[] originalMaterials;  // Store all original materials
    private Color originalColor;
    private bool isCurrentlyInspected = false;
    private bool isShowingOutline = false;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
            originalMaterials = objectRenderer.materials;  // Store all materials
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
        if (useContextualScale)
        {
            // Check if this is an assembled jar
            if (IsAssembledJar())
            {
                return assembledJarInspectionScale;
            }
            else
            {
                return pieceInspectionScale;
            }
        }

        return inspectionScale;
    }

    /// <summary>
    /// Check if this object is an assembled jar
    /// </summary>
    private bool IsAssembledJar()
    {
        // Check if this transform is referenced as an assembled jar root by any jar pieces
        JarAutoAssembly[] allJarPieces = FindObjectsOfType<JarAutoAssembly>();
        foreach (var piece in allJarPieces)
        {
            if (piece.IsAssembledJarRoot(transform))
            {
                return true;
            }
        }
        return false;
    }

    public void OnInspectionStart()
    {
        // Prevent multiple calls to OnInspectionStart
        if (isCurrentlyInspected)
        {
            Debug.Log($"OnInspectionStart already called for {gameObject.name} - skipping duplicate call");
            return;
        }

        isCurrentlyInspected = true;
        Debug.Log($"🔍 Started inspecting jar: {gameObject.name}");

        // Enable highlight effect
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(true);
        }

        // Set up materials for inspection - preserve dual material setup
        if (objectRenderer != null)
        {
            if (inspectionMaterial != null)
            {
                // Create array with both materials: main texture + outline
                Material[] inspectionMaterials = new Material[2];
                inspectionMaterials[0] = originalMaterials[0];  // Keep original main material

                // DON'T add the outline material yet - only add it when needed
                // For now, just use the original material
                inspectionMaterials[1] = originalMaterials[0]; // Placeholder - same as main material
                objectRenderer.materials = inspectionMaterials;
                Debug.Log($"🎯 Initial materials: Element 0: {inspectionMaterials[0].name}, Element 1: {inspectionMaterials[1].name}");
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

        // Hide outline if it was showing
        if (isShowingOutline)
        {
            HideOutline();
        }

        // Disable highlight effect
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }

        // Restore original materials
        if (objectRenderer != null)
        {
            if (originalMaterials != null && originalMaterials.Length > 0)
            {
                objectRenderer.materials = originalMaterials;
            }
            else if (originalMaterial != null)
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
    /// Check if this jar is currently being inspected
    /// </summary>
    public bool IsBeingInspected()
    {
        return isCurrentlyInspected;
    }

    /// <summary>
    /// Show outline when another object is being dragged nearby
    /// </summary>
    public void ShowOutline()
    {
        if (isShowingOutline || !isCurrentlyInspected || objectRenderer == null || inspectionMaterial == null)
            return;

        isShowingOutline = true;

        // NOW replace Element 1 with the actual outline material
        Material[] materials = objectRenderer.materials;
        if (materials.Length > 1)
        {
            // Create a copy of the outline material and make it visible
            Material outlineMaterialCopy = new Material(inspectionMaterial);
            if (outlineMaterialCopy.HasProperty("_Outline_Color"))
            {
                Color outlineColor = outlineMaterialCopy.GetColor("_Outline_Color");
                outlineColor.a = 1f; // Make outline visible
                outlineMaterialCopy.SetColor("_Outline_Color", outlineColor);
            }

            materials[1] = outlineMaterialCopy;  // Replace Element 1 with visible outline
            objectRenderer.materials = materials;
        }

        Debug.Log($"🔴 Showing outline on inspected {gameObject.name} - drag target detected");
    }

    /// <summary>
    /// Hide outline when drag object moves away or drag ends
    /// </summary>
    public void HideOutline()
    {
        if (!isShowingOutline)
            return;

        isShowingOutline = false;

        // Replace Element 1 with the original material (no outline)
        if (objectRenderer != null && originalMaterials != null && originalMaterials.Length > 0)
        {
            Material[] materials = objectRenderer.materials;
            if (materials.Length > 1)
            {
                materials[1] = originalMaterials[0];  // Replace with original material (no outline)
                objectRenderer.materials = materials;
            }
        }

        Debug.Log($"⚪ Hiding outline on {gameObject.name} - drag target moved away");
    }

    /// <summary>
    /// Check if outline is currently being shown
    /// </summary>
    public bool IsShowingOutline()
    {
        return isShowingOutline;
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