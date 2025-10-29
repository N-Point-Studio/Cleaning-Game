using UnityEngine;

public class MouseTest : MonoBehaviour
{
    void OnMouseDown()
    {
        Debug.Log($"Mouse clicked on: {gameObject.name}");
    }
}