using System.Collections.Generic;
using UnityEngine;

public class ToolManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolInputReader inputReader;

    [Header("Tools List (auto-filled if empty)")]
    [SerializeField] private List<ToolBase> tools = new List<ToolBase>();
    [SerializeField] private int activeToolIndex = 0;

    private ToolBase activeTool;

    private void Awake()
    {
        // Kalau list kosong, cari semua tool di anak-anak GameObject ini
        if (tools.Count == 0)
        {
            tools.AddRange(GetComponentsInChildren<ToolBase>(includeInactive: true));
        }

        // Nonaktifkan semua tool di awal
        foreach (var tool in tools)
        {
            tool.gameObject.SetActive(false);
        }

        // Aktifkan tool pertama (default)
        if (tools.Count > 0)
        {
            SetActiveTool(activeToolIndex);
        }
    }

    private void OnEnable()
    {
        inputReader.OnTouchStart += HandleTouchStart;
        inputReader.OnTouchMove += HandleTouchMove;
        inputReader.OnTouchEnd += HandleTouchEnd;
        inputReader.OnTouchTap += HandleTouchTap;
    }

    private void OnDisable()
    {
        inputReader.OnTouchStart -= HandleTouchStart;
        inputReader.OnTouchMove -= HandleTouchMove;
        inputReader.OnTouchEnd -= HandleTouchEnd;
        inputReader.OnTouchTap -= HandleTouchTap;
    }

    // =========================
    //  Tool Switching
    // =========================

    public void SetActiveTool(int index)
    {
        if (index < 0 || index >= tools.Count) return;

        // Deactivate previous tool
        if (activeTool != null)
        {
            activeTool.OnToolDeactivate();
            activeTool.gameObject.SetActive(false);
        }

        // Activate new tool
        activeToolIndex = index;
        activeTool = tools[activeToolIndex];
        activeTool.gameObject.SetActive(true);
        activeTool.OnToolActivate();

        Debug.Log($"🧰 Active Tool: {activeTool.name}");
    }

    public void SetActiveTool(string toolName)
    {
        int index = tools.FindIndex(t => t.name == toolName);
        if (index != -1)
            SetActiveTool(index);
    }

    private void Update()
    {
        // Optional: ganti tool pakai tombol 1, 2, 3, ...
        for (int i = 0; i < tools.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SetActiveTool(i);
            }
        }
    }

    // =========================
    //  Forward Input
    // =========================
    private void HandleTouchStart(Vector2 pos) => activeTool?.OnToolDragStart(pos);
    private void HandleTouchMove(Vector2 pos) => activeTool?.OnToolDragging(pos);
    private void HandleTouchEnd(Vector2 pos) => activeTool?.OnToolDragEnd(pos);
    private void HandleTouchTap(Vector2 pos) => activeTool?.OnToolTap(pos);
}
