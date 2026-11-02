using System.Reflection;
using UnityEngine;
using Vuforia;

// Dev tool for screenshots: the editor has no camera feed, so this stands in for Vuforia finding the marker.
public static class ARDemo
{
    public static QueueManager Queue => Object.FindObjectOfType<QueueManager>();

    public static void PretendMarkerFound()
    {
        ImageTargetBehaviour target = Object.FindObjectOfType<ImageTargetBehaviour>();

        // What DefaultObserverEventHandler would show once the marker is tracked
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;
        foreach (Canvas canvas in target.GetComponentsInChildren<Canvas>(true))
            canvas.enabled = true;
        foreach (Collider collider in target.GetComponentsInChildren<Collider>(true))
            collider.enabled = true;

        // Let the AR guide react exactly as it would to Vuforia's tracking event
        ARGuide guide = Object.FindObjectOfType<ARGuide>();
        if (guide != null)
        {
            typeof(ARGuide).GetMethod("OnTargetStatusChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(guide, new object[] { target, new TargetStatus(Status.TRACKED, StatusInfo.NORMAL) });
        }

        // There is no real camera pose without a webcam, so aim the camera at the queue's slots
        Camera camera = Camera.main;
        VuforiaBehaviour vuforia = camera.GetComponent<VuforiaBehaviour>();
        if (vuforia != null)
            vuforia.enabled = false;

        Bounds bounds = new Bounds(Queue.transform.position, Vector3.zero);
        foreach (Renderer renderer in Queue.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(renderer.bounds);

        float size = Mathf.Max(bounds.size.x, 0.5f);
        Transform marker = target.transform;
        camera.transform.position = bounds.center + marker.up * size * 0.45f - marker.forward * size * 0.85f;
        camera.transform.LookAt(bounds.center + marker.up * size * 0.08f, marker.up);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.17f, 0.17f, 0.2f);
    }
}
