using System.Reflection;
using UnityEngine;
using Vuforia;

// Dev tool for screenshots: the editor has no camera feed, so this stands in for Vuforia finding the marker.
public static class ARDemo
{
    public static QueueManager Queue => Object.FindObjectOfType<QueueManager>();

    public static void PretendMarkerFound()
    {
        ImageTargetBehaviour target = RevealTarget();

        // There is no real camera pose without a webcam, so aim the camera at the queue's slots
        Camera camera = TakeOverCamera();
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

    // Views the card head-on with its top edge up, like a marker shown on a monitor or stuck to a wall
    public static void PretendUprightMarkerFound()
    {
        RevealTarget();
        Camera camera = TakeOverCamera();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.93f, 0.93f, 0.95f);
        FrameUpright();
    }

    // Points the camera straight at the card so everything shown on it fits, card top edge up
    public static void FrameUpright()
    {
        ImageTargetBehaviour target = FindMarker();
        Transform marker = target.transform;
        Camera camera = Camera.main;

        bool any = false;
        Bounds bounds = new Bounds(marker.position, Vector3.zero);
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;
            if (!any)
                bounds = renderer.bounds;
            else
                bounds.Encapsulate(renderer.bounds);
            any = true;
        }

        float size = Mathf.Max(bounds.extents.magnitude, 0.05f);
        float distance = size / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.1f;
        camera.transform.position = bounds.center + marker.up * distance;
        camera.transform.LookAt(bounds.center, marker.forward);
        camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, distance * 0.05f);
    }

    // The scene's real image target. Vuforia also adds an emulator ground plane in the editor, so take
    // the target that holds the most content rather than whichever one is found first.
    private static ImageTargetBehaviour FindMarker()
    {
        ImageTargetBehaviour best = null;
        int bestCount = -1;
        foreach (ImageTargetBehaviour target in Object.FindObjectsOfType<ImageTargetBehaviour>())
        {
            int count = target.GetComponentsInChildren<Renderer>(true).Length;
            if (count > bestCount)
            {
                best = target;
                bestCount = count;
            }
        }
        return best;
    }

    // What DefaultObserverEventHandler would show once the marker is tracked
    private static ImageTargetBehaviour RevealTarget()
    {
        ImageTargetBehaviour target = FindMarker();

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;
        foreach (Canvas canvas in target.GetComponentsInChildren<Canvas>(true))
            canvas.enabled = true;
        foreach (Collider collider in target.GetComponentsInChildren<Collider>(true))
            collider.enabled = true;

        // Scenes hook extra setup to the marker-found event, e.g. the Stack scene switches its manager on
        DefaultObserverEventHandler handler = target.GetComponent<DefaultObserverEventHandler>();
        if (handler != null)
        {
            handler.OnTargetFound?.Invoke();
            // Without a camera feed Vuforia reports the marker as lost, which would undo the above
            Object.Destroy(handler);
        }

        // Let the AR guide react exactly as it would to Vuforia's tracking event
        ARGuide guide = Object.FindObjectOfType<ARGuide>();
        if (guide != null)
        {
            typeof(ARGuide).GetMethod("OnTargetStatusChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(guide, new object[] { target, new TargetStatus(Status.TRACKED, StatusInfo.NORMAL) });
        }
        return target;
    }

    private static Camera TakeOverCamera()
    {
        Camera camera = Camera.main;
        VuforiaBehaviour vuforia = camera.GetComponent<VuforiaBehaviour>();
        if (vuforia != null)
            vuforia.enabled = false;
        return camera;
    }
}
