using System.Collections.Generic;
using UnityEngine;

public partial class JarAutoAssembly : MonoBehaviour
{
    List<JarAutoAssembly> GetAssembledPieces()
    {
        List<JarAutoAssembly> assembled = new List<JarAutoAssembly>();
        string piecesFound = "";
        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece != null && piece.isAssembled)
            {
                assembled.Add(piece);
                piecesFound += piece.pieceType.ToString() + " ";
            }
        }
        Debug.Log($"[GetAssembledPieces] Found {assembled.Count} assembled pieces: {piecesFound}");
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
        Debug.Log($"[EnsureRotationTarget] Called for piece: {pieceType}");

        if (closeUpManager == null)
        {
            Debug.LogError("[EnsureRotationTarget] closeUpManager is null!");
            return transform;
        }

        List<JarAutoAssembly> assembledPieces = GetAssembledPieces();
        bool isThisPieceAssembled = isAssembled;
        bool isJarFullyAssembledNow = jarFullyAssembled;

        Debug.Log($"[EnsureRotationTarget] State for {pieceType}: isAssembled={isThisPieceAssembled}, assembledPieces.Count={assembledPieces.Count}, jarFullyAssembled={isJarFullyAssembledNow}");

        // If this piece is part of a partial assembly (more than 1 piece assembled, but not all)
        if (isThisPieceAssembled && assembledPieces.Count > 1 && !isJarFullyAssembledNow)
        {
            Debug.Log($"[EnsureRotationTarget] Decision: Partial assembly detected. Returning group transform.");
            return SetupPartialAssemblyRotation(assembledPieces, closeUpManager, startRotation);
        }
        else
        {
            Debug.Log($"[EnsureRotationTarget] Decision: Not a partial assembly. Returning single piece transform.");
            return transform;
        }
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

        JarAutoAssembly.currentPartialAssemblyParent = new GameObject("PartialAssemblyParent");
        JarAutoAssembly.currentPartialAssemblyParent.transform.position = centerPosition;

        foreach (JarAutoAssembly piece in assembledPieces)
        {
            piece.transform.SetParent(JarAutoAssembly.currentPartialAssemblyParent.transform, true);
        }

        Debug.Log($"🏗️ Created temporary parent for {assembledPieces.Count} assembled pieces at {centerPosition}");
        return JarAutoAssembly.currentPartialAssemblyParent;
    }

    static void CleanupTemporaryAssemblyParent()
    {
        if (JarAutoAssembly.currentPartialAssemblyParent != null)
        {
            for (int i = JarAutoAssembly.currentPartialAssemblyParent.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = JarAutoAssembly.currentPartialAssemblyParent.transform.GetChild(i);
                JarAutoAssembly piece = child.GetComponent<JarAutoAssembly>();
                if (piece != null)
                {
                    child.SetParent(piece.originalParent, true);
                }
            }

            if (Application.isPlaying)
                Object.Destroy(JarAutoAssembly.currentPartialAssemblyParent);
            else
                Object.DestroyImmediate(JarAutoAssembly.currentPartialAssemblyParent);

            JarAutoAssembly.currentPartialAssemblyParent = null;
            Debug.Log("🗑️ Cleaned up temporary assembly parent");
        }
    }

    void TryInspection()
    {
        Debug.Log($"[TryInspection] Called for piece: {pieceType}");

        ObjectCloseUpManager closeUpManager = FindObjectOfType<ObjectCloseUpManager>();
        if (closeUpManager == null)
        {
            Debug.LogError("[TryInspection] ObjectCloseUpManager not found!");
            return;
        }

        if (closeUpManager.IsTransitioning)
        {
            Debug.Log($"[TryInspection] Close-up transition active, ignoring request.");
            return;
        }

        List<JarAutoAssembly> assembledPieces = GetAssembledPieces();
        bool isThisPieceAssembled = isAssembled;
        bool isJarFullyAssembledNow = jarFullyAssembled;

        Debug.Log($"[TryInspection] State for {pieceType}: isAssembled={isThisPieceAssembled}, assembledPieces.Count={assembledPieces.Count}, jarFullyAssembled={isJarFullyAssembledNow}");

        if (isThisPieceAssembled && assembledPieces.Count > 1)
        {
            if (!isJarFullyAssembledNow)
            {
                Debug.Log($"[TryInspection] Decision: Partial assembly detected. Inspecting as a group.");
                SetupPartialAssemblyRotation(assembledPieces, closeUpManager);
            }
            else
            {
                Debug.Log($"[TryInspection] Decision: Full assembly detected. Letting root object handle inspection.");
            }
        }
        else
        {
            Debug.Log($"[TryInspection] Decision: Inspecting piece individually.");
            var inspectable = GetComponent<IInspectable>();
            if (inspectable != null)
            {
                inspectable.OnInspectionStart();
                closeUpManager.BringObjectToCloseUp(transform);
            }
        }
    }
}
