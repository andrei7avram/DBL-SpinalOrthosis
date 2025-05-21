using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public float rotationSpeed = 5f;
    public float distance = 5f;
    
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (target != null)
        {
            transform.position = target.position - transform.forward * distance;
        }
    }

    private void Update()
    {
        if (target == null) return;

        // Check if mouse is over this camera's viewport
        Vector3 viewportMousePos = cam.ScreenToViewportPoint(Input.mousePosition);
        bool isMouseOverThisCamera = viewportMousePos.x >= 0 && viewportMousePos.x <= 1 && 
                                    viewportMousePos.y >= 0 && viewportMousePos.y <= 1;

        // Handle mouse drag
        if (Input.GetMouseButtonDown(0) && isMouseOverThisCamera)
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // Rotate only while the left mouse button is pressed and dragging
        if (isDragging && Input.GetMouseButton(0) && isMouseOverThisCamera)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            // Rotate around target based on mouse movement
            transform.RotateAround(target.position, Vector3.up, delta.x * rotationSpeed);
            transform.RotateAround(target.position, transform.right, -delta.y * rotationSpeed);
        }

        // Keep camera looking at target and maintain distance
        transform.LookAt(target.position);
        transform.position = target.position - transform.forward * distance;
    }
}