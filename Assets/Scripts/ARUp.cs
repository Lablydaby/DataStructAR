using UnityEngine;

// Which way is up around an AR marker. An image target's own up is the card's normal, which only
// points up when the card lies on a table; on a wall or monitor the card's top edge points up instead.
public static class ARUp
{
    // Real-world up from the phone's gravity sensor, in world space.
    // Falls back to the camera's up where there is no accelerometer, e.g. in the editor.
    public static Vector3 WorldUp()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return Vector3.up;

        // Already adjusted to the screen orientation (Input.compensateSensors); +z is out of the screen
        Vector3 gravity = Input.acceleration;
        if (gravity.sqrMagnitude < 0.01f)
            return camera.transform.up;

        return -camera.transform.TransformDirection(new Vector3(gravity.x, gravity.y, -gravity.z)).normalized;
    }

    // The marker axis closest to real-world up: its normal, or either way along its top edge or sides.
    // Keeps the current axis unless another is clearly better, so it doesn't flip back and forth
    // when the card is tilted halfway.
    public static Vector3 ClosestMarkerAxis(Transform marker, Vector3 current, float slack = 0.2f)
    {
        Vector3 up = WorldUp();
        Vector3 best = current;
        foreach (Vector3 axis in new[] { marker.up, marker.forward, -marker.forward, marker.right, -marker.right })
        {
            if (Vector3.Dot(axis, up) > Vector3.Dot(best, up))
                best = axis;
        }
        return Vector3.Dot(best, up) > Vector3.Dot(current, up) + slack ? best : current;
    }
}
