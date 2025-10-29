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

public class JarPiece : MonoBehaviour
{
    [Header("Piece Settings")]
    public JarPieceType pieceType;
    public bool isAssembled = false;

    [Header("Movement Settings")]
    public float dragSpeed = 8f;

    private bool isDragging = false;
    private Camera mainCamera;
    private Vector3 targetPosition;
    private Vector3 originalPosition;
    private JarAssemblyManager assemblyManager;

    void Start()
    {
        originalPosition = transform.position;
        targetPosition = transform.position;
        mainCamera = Camera.main;

        // Find the assembly manager
        assemblyManager = FindObjectOfType<JarAssemblyManager>();
    }

    void OnMouseDown()
    {
        Debug.Log($"OnMouseDown - {pieceType}: isAssembled = {isAssembled}");
        if (!isAssembled)
        {
            isDragging = true;
            Debug.Log($"Started dragging {pieceType}");
        }
        else
        {
            Debug.Log($"Cannot drag {pieceType} - already assembled");
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

            // Try to assemble this piece
            if (assemblyManager != null)
            {
                assemblyManager.TryAssemble(this);
            }
        }
    }

    void Update()
    {
        if (isDragging && !isAssembled)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed);
        }
    }

    public void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.5f).SetEase(Ease.OutQuad);
    }

    public void AssembleToPosition(Vector3 assemblyPosition)
    {
        isAssembled = true;
        isDragging = false;

        // Animate to assembly position
        transform.DOMove(assemblyPosition, 0.6f).SetEase(Ease.OutBack);
        transform.DORotate(Vector3.zero, 0.6f).SetEase(Ease.OutBack);

        // Scale effect for satisfaction
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);
    }
}