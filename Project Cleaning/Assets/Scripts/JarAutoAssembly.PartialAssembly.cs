using System.Collections.Generic;
using UnityEngine;

public partial class JarAutoAssembly : MonoBehaviour
{
    List<JarAutoAssembly> GetAssembledPieces()
    {
        List<JarAutoAssembly> assembled = new List<JarAutoAssembly>();

        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null && piece.isAssembled)
            {
                assembled.Add(piece);
            }
        }

        return assembled;
    }

    Transform SetupPartialAssemblyRotation(List<JarAutoAssembly> assembledPieces, ObjectCloseUpManager closeUpManager, bool startRotation = true)
    {
        Debug.Log($"🔗 Setting up partial assembly rotation for {assembledPieces.Count} pieces");

        GameObject tempParent = CreateTemporaryAssemblyParent(assembledPieces);
        if (tempParent != null)
        {
            foreach (JarAutoAssembly piece in assembledPieces)
            {
                var inspectable = piece.GetComponent<IInspectable>();
                if (inspectable != null)
                {
                    inspectable.OnInspectionStart();
                }
            }

            closeUpManager.SetCurrentObject(tempParent.transform);

            if (startRotation)
            {
                var smoothRotator = FindObjectOfType<SmoothObjectRotator>();
                if (smoothRotator != null)
                {
                    smoothRotator.UpdateFixedPosition(tempParent.transform.position);
                    smoothRotator.StartRotating(tempParent.transform);
                    Debug.Log($"🔄 Started partial assembly rotation at position: {tempParent.transform.position}");
                }
            }

            Debug.Log($"✅ Partial assembly ({assembledPieces.Count} pieces) ready for group rotation");
            Debug.Log($"🔒 Unassembled pieces will remain independent");
            return tempParent.transform;
        }

        Debug.LogWarning("⚠️ Failed to create temporary parent - falling back to individual piece inspection");

        var fallbackInspectable = GetComponent<IInspectable>();
        if (fallbackInspectable != null)
        {
            fallbackInspectable.OnInspectionStart();
            closeUpManager.BringObjectToCloseUp(transform);
        }

        return transform;
    }

    public Transform EnsureRotationTarget(ObjectCloseUpManager closeUpManager, bool startRotation)
    {
        if (!isAssembled || closeUpManager == null)
            return transform;

        List<JarAutoAssembly> assembledPieces = GetAssembledPieces();
        if (assembledPieces.Count > 1 && assembledPieces.Count < allPieces.Count)
        {
            return SetupPartialAssemblyRotation(assembledPieces, closeUpManager, startRotation);
        }

        return transform;
    }

    GameObject CreateTemporaryAssemblyParent(List<JarAutoAssembly> assembledPieces)
    {
        CleanupTemporaryAssemblyParent();

        if (assembledPieces.Count == 0)
            return null;

        Vector3 centerPosition = Vector3.zero;
        foreach (JarAutoAssembly piece in assembledPieces)
        {
            centerPosition += piece.transform.position;
        }
        centerPosition /= assembledPieces.Count;

        currentPartialAssemblyParent = new GameObject("PartialAssemblyParent");
        currentPartialAssemblyParent.transform.position = centerPosition;

        foreach (JarAutoAssembly piece in assembledPieces)
        {
            piece.transform.SetParent(currentPartialAssemblyParent.transform, true);
        }

        Debug.Log($"🏗️ Created temporary parent for {assembledPieces.Count} assembled pieces at {centerPosition}");
        return currentPartialAssemblyParent;
    }

    static void CleanupTemporaryAssemblyParent()
    {
        if (currentPartialAssemblyParent != null)
        {
            for (int i = currentPartialAssemblyParent.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = currentPartialAssemblyParent.transform.GetChild(i);
                JarAutoAssembly piece = child.GetComponent<JarAutoAssembly>();
                if (piece != null)
                {
                    child.SetParent(piece.originalParent, true);
                }
            }

            if (Application.isPlaying)
                Object.Destroy(currentPartialAssemblyParent);
            else
                Object.DestroyImmediate(currentPartialAssemblyParent);

            currentPartialAssemblyParent = null;
            Debug.Log("🗑️ Cleaned up temporary assembly parent");
        }
    }

    void TryInspection()
    {
        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager != null && !jarFullyAssembled)
        {
            if (closeUpManager.IsTransitioning)
            {
                Debug.Log($"⏳ Close-up transition active - delaying inspection request for {pieceType}");
                return;
            }

            List<JarAutoAssembly> assembledPieces = GetAssembledPieces();

            if (assembledPieces.Count > 1 && isAssembled)
            {
                Debug.Log($"🔗 Partial assembly detected! {assembledPieces.Count} pieces assembled together");
                SetupPartialAssemblyRotation(assembledPieces, closeUpManager);
            }
            else
            {
                Debug.Log($"Jar piece {pieceType} - bringing to close-up for inspection (assembled: {isAssembled})");

                var inspectable = GetComponent<IInspectable>();
                if (inspectable != null)
                {
                    inspectable.OnInspectionStart();
                    closeUpManager.BringObjectToCloseUp(transform);
                }
            }
        }
    }
}
