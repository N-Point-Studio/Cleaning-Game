using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CleaningToolController : MonoBehaviour
{
    // speed at which tool moves (optional smoothing)
    public float moveSpeed = 10f;

    // if using touch or mouse
    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // get input position
        Vector3 inputPos = Input.mousePosition;

        // convert to world point — we assume tool moves on a plane z = some value
        // For example, set a fixed distance from camera
        float toolZDistance = 5f;  // adjust this to where your tool plane is
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, toolZDistance));

        // smoothly move tool to this position
        transform.position = Vector3.Lerp(transform.position, worldPos, Time.deltaTime * moveSpeed);
    }
}
