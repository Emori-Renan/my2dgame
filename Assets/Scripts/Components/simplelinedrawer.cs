using UnityEngine;

// This attribute ensures that a LineRenderer component is automatically
// added to the GameObject when you attach this script.
[RequireComponent(typeof(LineRenderer))]
public class SimpleLineDrawer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    void Start()
    {
        // Get the LineRenderer component that's attached to this GameObject.
        lineRenderer = GetComponent<LineRenderer>();
        
        // Let's create a simple material for our line to ensure it's visible.
        // We'll use the "Unlit/Color" shader, which is very basic and always works.
        lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material.color = Color.yellow;
        
        // Set the width of the line at its start and end points.
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        
        // Now, we'll tell the LineRenderer how many points our line will have.
        // To draw a closed square, we need 5 points (4 corners + 1 to close the loop).
        lineRenderer.positionCount = 5;

        // Make sure the GameObject is placed in a visible spot.
        // We'll set it to the origin (0, 0, 0) of the world space.
        // Make sure your camera is positioned to see this point!
        transform.position = Vector3.zero;

        // Define the positions for each point in the line.
        Vector3[] points = new Vector3[5];
        
        // Define the corners of the square, starting from the bottom-left and moving clockwise.
        // These points are relative to the GameObject's transform.
        points[0] = new Vector3(-1, -1, 0); // Bottom-left corner
        points[1] = new Vector3(1, -1, 0);  // Bottom-right corner
        points[2] = new Vector3(1, 1, 0);   // Top-right corner
        points[3] = new Vector3(-1, 1, 0);  // Top-left corner
        points[4] = points[0];              // Return to the first point to complete the square

        // Assign the array of points to the LineRenderer.
        lineRenderer.SetPositions(points);

        Debug.Log("Line successfully created! Check your scene view to confirm.");
    }
}
