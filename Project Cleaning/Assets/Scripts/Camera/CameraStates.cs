using UnityEngine;

/// <summary>
/// Overview state - wide view of all objects
/// </summary>
public class OverviewState : State
{
    private TopDownCameraController cameraController;

    public OverviewState(TopDownCameraController controller)
    {
        cameraController = controller;
    }

    public override void Enter()
    {
        Debug.Log("Camera: OVERVIEW");
        cameraController.TransitionToOverview();
    }

    public override void Tick(float deltaTime)
    {
        // Handle any continuous overview logic here
    }

    public override void Exit()
    {
        // Cleanup when leaving overview state
    }

    public void HandleObjectClick(Transform clickedObject)
    {
        cameraController.SetFocusTarget(clickedObject);
        cameraController.SwitchState(cameraController.focusState);
    }

    public void HandlePinchIn(Vector2 centerPoint)
    {
        Transform closestObject = cameraController.FindClosestObjectToScreenPoint(centerPoint);
        if (closestObject != null)
        {
            cameraController.SetFocusTarget(closestObject);
            cameraController.SwitchState(cameraController.focusState);
        }
    }
}

/// <summary>
/// Focus state - zoomed in on specific object
/// </summary>
public class FocusState : State
{
    private TopDownCameraController cameraController;

    public FocusState(TopDownCameraController controller)
    {
        cameraController = controller;
    }

    public override void Enter()
    {
        Debug.Log("Camera: FOCUS");
        cameraController.TransitionToFocus();
    }

    public override void Tick(float deltaTime)
    {
        // Handle any continuous focus logic here
    }

    public override void Exit()
    {
        // Cleanup when leaving focus state
    }

    public void HandlePinchOut()
    {
        cameraController.SwitchState(cameraController.overviewState);
    }

    public void HandleTapHold(Vector2 position)
    {
        cameraController.SwitchState(cameraController.navigationState);
    }

    public void HandleObjectClick(Transform clickedObject)
    {
        if (clickedObject != cameraController.currentFocusTarget)
        {
            cameraController.SetFocusTarget(clickedObject);
            cameraController.TransitionToFocus();
        }
    }

    public void HandleBackgroundClick()
    {
        // Background click in focus mode does nothing - only pinch out exits
        // This keeps the camera focused when clicking empty areas
    }
}

/// <summary>
/// Navigation state - temporary overview while in focus mode
/// </summary>
public class NavigationState : State
{
    private TopDownCameraController cameraController;

    public NavigationState(TopDownCameraController controller)
    {
        cameraController = controller;
    }

    public override void Enter()
    {
        Debug.Log("Camera: NAVIGATION");
        cameraController.TransitionToNavigation();
    }

    public override void Tick(float deltaTime)
    {
        // Handle any continuous navigation logic here
    }

    public override void Exit()
    {
        // Cleanup when leaving navigation state
    }

    public void HandleObjectClick(Transform clickedObject)
    {
        // In navigation mode, clicking objects switches focus to them
        cameraController.SetFocusTarget(clickedObject);
        cameraController.SwitchState(cameraController.focusState);
    }

    public void HandleTapHoldRelease()
    {
        // Release hold - return to focused state
        cameraController.SwitchState(cameraController.focusState);
    }

    public void HandlePinchIn(Vector2 centerPoint)
    {
        // Pinch in while exploring - focus on closest object
        Transform closestObject = cameraController.FindClosestObjectToScreenPoint(centerPoint);
        if (closestObject != null)
        {
            cameraController.SetFocusTarget(closestObject);
            cameraController.SwitchState(cameraController.focusState);
        }
    }

    public void HandlePinchOut()
    {
        // Pinch out while exploring - go to overview
        cameraController.SwitchState(cameraController.overviewState);
    }

    public void HandleBackgroundClick()
    {
        // Background click while exploring - just stay in navigation (no action)
        // User can release hold to go back to focus
    }
}