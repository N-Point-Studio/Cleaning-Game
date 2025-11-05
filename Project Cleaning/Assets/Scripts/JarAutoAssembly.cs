using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum JarPieceType
{
    Bottom = 0,
    Middle = 1,
    Top = 2
}

public partial class JarAutoAssembly : MonoBehaviour
{
    [Header("Assembly Settings")]
    public float snapDistance = 1.5f;
    public float assemblyDuration = 0.8f;

    [Header("Correct Positions & Rotations")]
    [SerializeField] private Vector3 bottomPosition = new Vector3(-0.17f, 1.67f, 0.087f);
    [SerializeField] private Vector3 middlePosition = new Vector3(-0.13f, 0.516f, 0.099f);
    [SerializeField] private Vector3 topPosition = new Vector3(0.285f, 1.617f, -0.177f);
    [SerializeField] private Vector3 correctRotation = new Vector3(-89.98f, 0f, 0f);

    [Header("Final Assembly Positions & Rotations")]
    [Tooltip("Exact final positions and rotations when all pieces are assembled")]
    [SerializeField] private Vector3 finalBottomPosition = new Vector3(-0.0399f, 4.12828f, -1.46515f);
    [SerializeField] private Vector3 finalMiddlePosition = new Vector3(0f, 2.97428f, -1.45318f);
    [SerializeField] private Vector3 finalTopPosition = new Vector3(0.415f, 4.07528f, -1.72915f);
    [SerializeField] private Vector3 finalRotation = new Vector3(-89.98f, 0f, 0f);

    [Header("Completion Animation")]
    [SerializeField] private float completionMoveDuration = 0.7f;
    [SerializeField] private Ease completionMoveEase = Ease.OutCubic;
    [SerializeField] private Ease completionRotateEase = Ease.OutCubic;
    [SerializeField] private float completionScaleOvershoot = 0.05f;
    [SerializeField] private float completionScaleDuration = 0.35f;
    [SerializeField] private float completionCascadeDelay = 0.08f;

    [Header("Reassembly Settings")]
    [SerializeField] private bool allowReassemblyAfterCompletion = true;
    [SerializeField] private float reassemblyReturnDuration = 0.45f;
    [SerializeField] private Ease reassemblyEase = Ease.OutBack;

    [Header("Scale Adaptation")]
    [Tooltip("Positions were authored using this uniform piece scale. Target locations adapt automatically when actual scale differs.")]
    [SerializeField] private float referencePieceScale = 100f;
    [Tooltip("Apply automatic scaling to configured world/final positions based on current piece scale.")]
    [SerializeField] private bool autoScaleConfiguredPositions = true;

    [Header("Piece Settings")]
    public JarPieceType pieceType;
    public bool isAssembled = false;

    [Header("Movement")]
    public float dragSpeed = 8f;

    [Header("Input Settings")]
    public float holdTimeForDrag = 2f; // Time to hold before drag starts

    [Header("Assembled Object Settings")]
    [SerializeField] private Transform assembledJarRoot;
    [SerializeField] private Collider assembledJarCollider;
    [SerializeField] private bool disablePieceCollidersOnCompletion = true;

    private bool isDragging = false;
    private bool isHoldingForDrag = false;
    private float holdStartTime = 0f;
    private bool reassemblyHoldTriggered = false;
    private bool awaitingReassemblyDecision = false;
    private Vector2 reassemblyHoldStartScreenPos;
    private Camera mainCamera;
    private Vector3 targetPosition;
    private Vector3 originalPosition;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Quaternion originalRotation;
    private Vector3 originalScale;

    private static List<JarAutoAssembly> allPieces = new List<JarAutoAssembly>();
    private static bool jarFullyAssembled = false;
    private static bool staticsInitialized = false;
    private static List<JarAutoAssembly> assemblyOrder = new List<JarAutoAssembly>();
    public static GameObject currentPartialAssemblyParent = null;

    public static bool IsJarFullyAssembled => jarFullyAssembled;

    private Collider pieceCollider;
    private Transform originalParent;
    private bool initialAssembledColliderState;
    private Vector3 correctLocalPosition;
    private Quaternion correctLocalRotation;
    private bool localOffsetsComputed = false;
    private InspectableJar inspectableComponent;
    private bool initialCanBeInspected = true;
    private bool initialScriptEnabled;

    private static void EnsureStaticState()
    {
        if (staticsInitialized)
            return;

        allPieces = new List<JarAutoAssembly>();
        assemblyOrder.Clear();
        jarFullyAssembled = false;
        staticsInitialized = true;
    }

    private static int GetAssembledPieceCount()
    {
        CleanupNullPieces();

        int count = 0;

        for (int i = 0; i < allPieces.Count; i++)
        {
            JarAutoAssembly piece = allPieces[i];
            if (piece != null && piece.isAssembled)
            {
                count++;
            }
        }

        return count;
    }

    private static void CleanupNullPieces()
    {
        for (int i = allPieces.Count - 1; i >= 0; i--)
        {
            if (allPieces[i] == null)
            {
                allPieces.RemoveAt(i);
            }
        }
    }

    void Awake()
    {
        EnsureStaticState();
    }

    void Start()
    {
        originalPosition = transform.position;
        targetPosition = transform.position;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        mainCamera = Camera.main;
        pieceCollider = GetComponent<Collider>();
        originalParent = transform.parent;
        inspectableComponent = GetComponent<InspectableJar>();
        initialScriptEnabled = enabled;

        if (inspectableComponent != null)
        {
            initialCanBeInspected = inspectableComponent.canBeInspected;
        }

        if (assembledJarRoot != null && assembledJarCollider == null)
        {
            assembledJarCollider = assembledJarRoot.GetComponent<Collider>();
        }

        if (assembledJarCollider != null)
        {
            initialAssembledColliderState = assembledJarCollider.enabled;

            if (!jarFullyAssembled)
            {
                assembledJarCollider.enabled = false;
            }
        }

        if (!allPieces.Contains(this))
            allPieces.Add(this);

        ComputeLocalAssemblyOffsets();
    }

    void OnDestroy()
    {
        allPieces.Remove(this);

        if (allPieces.Count == 0)
        {
            CleanupTemporaryAssemblyParent();
        }
    }

    public Transform AssembledRoot => assembledJarRoot;

    void OnEnable()
    {
        TouchManager.OnMouseDown += HandleMouseDown;
        TouchManager.OnMouseUp += HandleMouseUp;
        TouchManager.OnMouseDrag += HandleMouseDrag;
    }

    void OnDisable()
    {
        TouchManager.OnMouseDown -= HandleMouseDown;
        TouchManager.OnMouseUp -= HandleMouseUp;
        TouchManager.OnMouseDrag -= HandleMouseDrag;
    }

    internal bool IsReassemblyHoldLocked => reassemblyHoldTriggered;
}
