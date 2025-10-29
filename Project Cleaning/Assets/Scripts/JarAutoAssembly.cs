using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum JarPieceType
{
    Bottom = 0,
    Middle = 1,
    Top = 2
}

public class JarAutoAssembly : MonoBehaviour
{
    [Header("Assembly Settings")]
    public float snapDistance = 1.5f;
    public float assemblyDuration = 0.8f;

    [Header("Correct Positions & Rotations")]
    [SerializeField] private Vector3 bottomPosition = new Vector3(-0.17f, 1.67f, 0.087f);
    [SerializeField] private Vector3 middlePosition = new Vector3(-0.13f, 0.516f, 0.099f);
    [SerializeField] private Vector3 topPosition = new Vector3(0.285f, 1.617f, -0.177f);
    [SerializeField] private Vector3 correctRotation = new Vector3(-89.98f, 0f, 0f);

    [Header("Piece Settings")]
    public JarPieceType pieceType;
    public bool isAssembled = false;

    [Header("Movement")]
    public float dragSpeed = 8f;

    private bool isDragging = false;
    private Camera mainCamera;
    private Vector3 targetPosition;
    private Vector3 originalPosition;
    private static List<JarAutoAssembly> allPieces = new List<JarAutoAssembly>();
    private static int assembledCount = 0;

    void Start()
    {
        originalPosition = transform.position;
        targetPosition = transform.position;
        mainCamera = Camera.main;

        // Register this piece
        if (!allPieces.Contains(this))
            allPieces.Add(this);
    }

    void OnDestroy()
    {
        allPieces.Remove(this);
    }

    void OnMouseDown()
    {
        if (!isAssembled)
        {
            isDragging = true;
            Debug.Log($"Started dragging {pieceType}");
        }
    }

    void OnMouseDrag()
    {
        if (!isDragging || mainCamera == null || isAssembled) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        float distance;
        if (dragPlane.Raycast(ray, out distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            targetPosition = worldPoint;
        }
    }

    void OnMouseUp()
    {
        if (!isAssembled)
        {
            isDragging = false;
            TryAssemble();
        }
    }

    void Update()
    {
        if (isDragging && !isAssembled)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed);
        }
    }

    void TryAssemble()
    {
        Vector3 correctPos = GetCorrectPosition();
        float distanceToCorrectPos = Vector3.Distance(transform.position, correctPos);

        Debug.Log($"{pieceType} distance to correct position: {distanceToCorrectPos:F2}");

        if (distanceToCorrectPos < snapDistance)
        {
            AssembleToCorrectPosition();
        }
        else
        {
            Debug.Log($"{pieceType} too far - returning to original position");
            ReturnToOriginalPosition();
        }
    }

    Vector3 GetCorrectPosition()
    {
        switch (pieceType)
        {
            case JarPieceType.Bottom:
                return bottomPosition;
            case JarPieceType.Middle:
                return middlePosition;
            case JarPieceType.Top:
                return topPosition;
            default:
                return transform.position;
        }
    }

    void AssembleToCorrectPosition()
    {
        Debug.Log($"Assembling {pieceType} to correct position!");

        isAssembled = true;
        isDragging = false;
        assembledCount++;

        Vector3 correctPos = GetCorrectPosition();

        // Animate to correct position and rotation
        transform.DOMove(correctPos, assemblyDuration).SetEase(Ease.OutBack);
        transform.DORotate(correctRotation, assemblyDuration).SetEase(Ease.OutBack);

        // Satisfaction effect
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);

        // Check completion
        CheckJarCompletion();
    }

    void CheckJarCompletion()
    {
        Debug.Log($"Progress: {assembledCount}/3 pieces assembled");

        if (assembledCount >= 3)
        {
            StartCoroutine(CelebrationSequence());
        }
    }

    IEnumerator CelebrationSequence()
    {
        Debug.Log("🎉 Jar restoration complete! 🎉");

        yield return new WaitForSeconds(0.5f);

        // Simple celebration effect - just a gentle scale pulse
        foreach (JarAutoAssembly piece in allPieces)
        {
            if (piece.isAssembled)
            {
                piece.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 3);
            }
        }

        // Optional: Add particle effects, sound, or UI feedback here
        // No rotation - jar stays in perfect final position
    }

    void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.5f).SetEase(Ease.OutQuad);
    }

    // Reset function for testing
    [ContextMenu("Reset Assembly")]
    public void ResetAssembly()
    {
        foreach (JarAutoAssembly piece in allPieces)
        {
            piece.isAssembled = false;
            piece.isDragging = false;
            piece.transform.position = piece.originalPosition;
            piece.transform.rotation = Quaternion.identity;
        }
        assembledCount = 0;
        Debug.Log("Assembly reset!");
    }

    void OnDrawGizmosSelected()
    {
        // Draw correct position
        Vector3 correctPos = GetCorrectPosition();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(correctPos, 0.2f);

        // Draw snap distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(correctPos, snapDistance);

        // Draw line to correct position
        if (!isAssembled)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, correctPos);
        }
    }
}