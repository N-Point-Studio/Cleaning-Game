using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceDetection : MonoBehaviour
{
    public enum CollisionToolsType
    {
        Texture,
        Mesh,
    }

    [Header("Raycast Settings")]
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private LayerMask dirtsLayerMask;
    [SerializeField] private CollisionToolsType surfaceType = CollisionToolsType.Texture;
    [SerializeField] private RectTransform pointerUI;

    public Vector2 TipPoint = new Vector2(0, 0);

    public Vector3 RaycastTipPos { get; private set; }
    public Vector3 RaycastTipNormal { get; private set; }
    public bool IsSurfaceDetected { get; private set; }
    public float RaycastTipRotation { get; private set; }

    public Clean CleaningSurface { get; private set; }
    public Vector2 TextureSurface { get; private set; }
    public CleanMesh MudObject { get; private set; }

    public bool isUsed = false;

    void Awake()
    {
        CleaningSurface = null;
    }

    private void Update()
    {
        if (TouchManager.Instance.isClickedOn && TouchManager.Instance.isInteracting)
        {
            if (isUsed)
            {
                PerformRaycastTouch();
                ShowPointer(TipPoint);
            }
        }
        else
        {
            IsSurfaceDetected = false;
            pointerUI.gameObject.SetActive(false);
        }

    }

    private void PerformRaycastTouch()
    {
        if (!TouchManager.Instance.isClickedOn)
        {
            IsSurfaceDetected = false;
            return;
        }

        Vector2 touchPos = TouchManager.Instance.curScreenPos;
        Ray ray = Camera.main.ScreenPointToRay(new Vector2(touchPos.x, touchPos.y + 400));
        TipPoint = new Vector2(touchPos.x, touchPos.y + 400);
        Debug.Log("ray point: " + TipPoint);

        Debug.Log("current screen pos: " + touchPos);
        Debug.Log("current screen pos: " + new Vector3(touchPos.x, touchPos.y + 100));

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayLength, dirtsLayerMask))
        {
            switch (surfaceType)
            {
                case CollisionToolsType.Mesh:
                    var mud = hit.collider.GetComponent<CleanMesh>();
                    EssentialDetecting(hit);
                    MudObject = mud;
                    break;
                case CollisionToolsType.Texture:
                    var clean = hit.collider.GetComponent<Clean>();
                    EssentialDetecting(hit);
                    CleaningSurface = clean;
                    break;

            }
            Debug.Log("hitting: " + hit.transform.name);
        }
        else
        {
            IsSurfaceDetected = false;
            RaycastTipPos = Vector3.positiveInfinity;
            CleaningSurface = null;
            MudObject = null;
        }
    }

    private void ShowPointer(Vector2 screenPos)
    {
        if (SettingManager.Instance.isTipPointEnabled == false) return;

        pointerUI.position = screenPos;
        pointerUI.gameObject.SetActive(true);
    }

    private void EssentialDetecting(RaycastHit hit)
    {

        IsSurfaceDetected = true;
        RaycastTipPos = hit.point;
        RaycastTipNormal = hit.normal;

        Vector3 projectedUp = Vector3.ProjectOnPlane(Vector3.up, hit.normal);
        RaycastTipRotation = Vector3.SignedAngle(Vector3.up, projectedUp, hit.normal);

        TextureSurface = hit.textureCoord;
        Debug.Log("uv coord: " + TextureSurface);
        Debug.Log("hitting: " + hit.transform.name + "is surface detected: " + IsSurfaceDetected);

        // --- TAMBAHAN TUTORIAL ---
        // Jika terdeteksi menyentuh permukaan, anggap user sudah bisa cleaning
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CompleteStep(1); // Index 1 = Cleaning
        }
        // -------------------------

    }
}
