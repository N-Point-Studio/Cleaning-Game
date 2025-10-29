using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class JarAssemblyManager : MonoBehaviour
{
    [Header("Assembly Settings")]
    public Transform assemblyBase; // Where the jar will be built
    public float snapThreshold = 2.0f; // Increased for easier assembly
    public float pieceHeight = 0.5f; // Height between each piece

    [Header("Assembly Progress")]
    public List<JarPiece> assembledPieces = new List<JarPiece>();

    private bool isCompleted = false;

    public void TryAssemble(JarPiece piece)
    {
        if (isCompleted) return;

        // Check if piece is close enough to assembly area
        float distanceToBase = Vector3.Distance(piece.transform.position, assemblyBase.position);

        Debug.Log($"Piece {piece.pieceType} distance to base: {distanceToBase:F2}");

        if (distanceToBase < snapThreshold)
        {
            // Check if this piece can be assembled now
            if (CanAssemblePiece(piece))
            {
                AssemblePiece(piece);
            }
            else
            {
                Debug.Log($"Cannot assemble {piece.pieceType} yet. Wrong order!");
                piece.ReturnToOriginalPosition();

                // Show what piece is needed next
                ShowNextPieceHint();
            }
        }
        else
        {
            Debug.Log("Too far from assembly area");
            piece.ReturnToOriginalPosition();
        }
    }

    private bool CanAssemblePiece(JarPiece piece)
    {
        // Check if this piece type is already assembled
        foreach (JarPiece assembledPiece in assembledPieces)
        {
            if (assembledPiece.pieceType == piece.pieceType)
            {
                Debug.Log($"{piece.pieceType} piece already assembled!");
                return false; // Can't place same type twice
            }
        }

        // Allow any piece to be placed (no sequence required)
        return true;
    }

    private void AssemblePiece(JarPiece piece)
    {
        Debug.Log($"Assembling {piece.pieceType}!");

        // Calculate position based on piece type (not assembly order)
        Vector3 assemblyPosition = GetPositionForPieceType(piece.pieceType);

        // Add to assembled list
        assembledPieces.Add(piece);

        // Animate piece to position
        piece.AssembleToPosition(assemblyPosition);

        // Check if jar is complete
        if (assembledPieces.Count >= 3) // All 3 pieces assembled
        {
            StartCoroutine(CompleteAssembly());
        }
        else
        {
            // Show progress feedback
            ShowProgressFeedback();
        }
    }

    private Vector3 GetPositionForPieceType(JarPieceType pieceType)
    {
        // Each piece goes to its correct height regardless of assembly order
        switch (pieceType)
        {
            case JarPieceType.Bottom:
                return assemblyBase.position; // Ground level

            case JarPieceType.Middle:
                return assemblyBase.position + Vector3.up * pieceHeight; // One level up

            case JarPieceType.Top:
                return assemblyBase.position + Vector3.up * (pieceHeight * 2); // Two levels up

            default:
                return assemblyBase.position;
        }
    }

    private void ShowNextPieceHint()
    {
        // Show which pieces are still needed (any order)
        List<string> neededPieces = new List<string>();

        bool hasBottom = assembledPieces.Any(p => p.pieceType == JarPieceType.Bottom);
        bool hasMiddle = assembledPieces.Any(p => p.pieceType == JarPieceType.Middle);
        bool hasTop = assembledPieces.Any(p => p.pieceType == JarPieceType.Top);

        if (!hasBottom) neededPieces.Add("Bottom");
        if (!hasMiddle) neededPieces.Add("Middle");
        if (!hasTop) neededPieces.Add("Top");

        if (neededPieces.Count > 0)
        {
            Debug.Log($"Still needed: {string.Join(", ", neededPieces)}");
        }
    }

    private void ShowProgressFeedback()
    {
        Debug.Log($"Progress: {assembledPieces.Count}/3 pieces assembled");

        // Optional: UI progress bar update
        // ProgressUI.SetProgress(assembledPieces.Count / 3f);
    }

    private IEnumerator CompleteAssembly()
    {
        isCompleted = true;
        Debug.Log("Jar assembly completed! 🏺");

        yield return new WaitForSeconds(0.5f);

        // Completion effects
        // 1. Scale pulse effect
        assemblyBase.DOPunchScale(Vector3.one * 0.2f, 0.8f, 8);

        // 2. Rotation celebration
        assemblyBase.DORotate(new Vector3(0, 360, 0), 1f, RotateMode.LocalAxisAdd);

        // 3. Optional: Particle effects, sound, UI celebration
        Debug.Log("🎉 Jar restoration complete! 🎉");
    }

    void OnDrawGizmosSelected()
    {
        if (assemblyBase != null)
        {
            // Draw snap threshold area
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(assemblyBase.position, snapThreshold);

            // Draw assembly height levels
            for (int i = 0; i < 3; i++)
            {
                Vector3 levelPos = assemblyBase.position + Vector3.up * (i * pieceHeight);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(levelPos, Vector3.one * 0.3f);
            }
        }
    }
}