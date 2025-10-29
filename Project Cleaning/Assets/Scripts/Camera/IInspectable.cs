using UnityEngine;

/// <summary>
/// Interface for objects that can be brought to camera for inspection
/// </summary>
public interface IInspectable
{
    /// <summary>
    /// Whether this object can currently be inspected
    /// </summary>
    bool CanBeInspected();

    /// <summary>
    /// The ideal scale multiplier when inspecting this object
    /// (1.0 = normal size, 2.0 = double size, etc.)
    /// </summary>
    float GetIdealInspectionScale();

    /// <summary>
    /// Called when inspection starts
    /// </summary>
    void OnInspectionStart();

    /// <summary>
    /// Called when inspection ends
    /// </summary>
    void OnInspectionEnd();

    /// <summary>
    /// Get the transform that should be moved for inspection
    /// (usually the object itself, but could be a parent)
    /// </summary>
    Transform GetInspectionTarget();
}