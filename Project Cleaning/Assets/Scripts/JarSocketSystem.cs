using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum SocketType
{
    BottomSocket,  // Receives pieces from below
    TopSocket      // Receives pieces from above
}

[System.Serializable]
public class ConnectionSocket
{
    public SocketType socketType;
    public Transform socketTransform; // Empty GameObject marking the connection point
    public bool isOccupied = false;
    public JarSocketSystem connectedPiece;
}

public class JarSocketSystem : MonoBehaviour
{
    [Header("Socket Settings")]
    public List<ConnectionSocket> sockets = new List<ConnectionSocket>();
    public float snapDistance = 0.8f;

    [Header("Piece Settings")]
    public JarPieceType pieceType;
    public bool isAssembled = false;

    [Header("Movement")]
    public float dragSpeed = 8f;

    private bool isDragging = false;
    private Camera mainCamera;
    private Vector3 targetPosition;
    private Vector3 originalPosition;
    private JarSocketSystem attachedToParent; // What piece this is connected to

    void Start()
    {
        originalPosition = transform.position;
        targetPosition = transform.position;
        mainCamera = Camera.main;
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
            TryConnectToNearbyPieces();
        }
    }

    void Update()
    {
        if (isDragging && !isAssembled)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * dragSpeed);
        }
    }

    void TryConnectToNearbyPieces()
    {
        // Find all other jar pieces in the scene
        JarSocketSystem[] allPieces = FindObjectsOfType<JarSocketSystem>();

        foreach (JarSocketSystem otherPiece in allPieces)
        {
            if (otherPiece == this) continue;

            // Check if we can connect to any of their sockets
            foreach (ConnectionSocket socket in otherPiece.sockets)
            {
                if (socket.isOccupied) continue;

                float dist = Vector3.Distance(transform.position, socket.socketTransform.position);

                if (dist < snapDistance && CanConnectToSocket(socket))
                {
                    ConnectToSocket(otherPiece, socket);
                    return;
                }
            }
        }

        // If no connection found, return to original position
        Debug.Log($"{pieceType} couldn't connect to anything - returning home");
        ReturnToOriginalPosition();
    }

    bool CanConnectToSocket(ConnectionSocket socket)
    {
        // Bottom piece can only connect to TopSockets
        // Middle piece can connect to both
        // Top piece can only connect to BottomSockets

        switch (pieceType)
        {
            case JarPieceType.Bottom:
                return socket.socketType == SocketType.BottomSocket;

            case JarPieceType.Middle:
                return true; // Middle can connect to any socket

            case JarPieceType.Top:
                return socket.socketType == SocketType.TopSocket;

            default:
                return false;
        }
    }

    void ConnectToSocket(JarSocketSystem targetPiece, ConnectionSocket socket)
    {
        Debug.Log($"Connecting {pieceType} to {targetPiece.pieceType}'s {socket.socketType}");

        isAssembled = true;
        isDragging = false;
        attachedToParent = targetPiece;

        // Mark socket as occupied
        socket.isOccupied = true;
        socket.connectedPiece = this;

        // Snap to socket position with animation
        transform.DOMove(socket.socketTransform.position, 0.6f).SetEase(Ease.OutBack);
        transform.DORotate(socket.socketTransform.rotation.eulerAngles, 0.6f).SetEase(Ease.OutBack);

        // Satisfaction effect
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5);

        // Check if jar is complete
        CheckJarCompletion();
    }

    void CheckJarCompletion()
    {
        // Count how many pieces are assembled
        JarSocketSystem[] allPieces = FindObjectsOfType<JarSocketSystem>();
        int assembledCount = 0;

        foreach (JarSocketSystem piece in allPieces)
        {
            if (piece.isAssembled || piece.attachedToParent != null)
            {
                assembledCount++;
            }
        }

        Debug.Log($"Assembled pieces: {assembledCount}/3");

        if (assembledCount >= 3)
        {
            Debug.Log("🎉 Jar restoration complete! 🎉");
            // Add celebration effects here
        }
    }

    void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.5f).SetEase(Ease.OutQuad);
    }

    void OnDrawGizmosSelected()
    {
        // Draw socket positions and snap range
        foreach (ConnectionSocket socket in sockets)
        {
            if (socket.socketTransform != null)
            {
                Gizmos.color = socket.isOccupied ? Color.red : Color.green;
                Gizmos.DrawWireSphere(socket.socketTransform.position, 0.1f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(socket.socketTransform.position, snapDistance);
            }
        }
    }
}