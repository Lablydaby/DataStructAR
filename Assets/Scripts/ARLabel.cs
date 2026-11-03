using UnityEngine;

// Keeps a 3D text label (e.g. Head, Null) next to the object it names, offset along the marker's
// row and above it in real-world terms, and always turned to face the camera so it reads correctly.
public class ARLabel : MonoBehaviour
{
    public Transform follow;   // Object the label sits next to
    public Transform marker;   // Frame the row of objects is laid out in
    public float alongRow;     // World units along the marker's right axis
    public float above;        // World units up

    private Vector3 upLocal;

    private void Start()
    {
        upLocal = marker.InverseTransformDirection(marker.up);
    }

    private void LateUpdate()
    {
        if (follow == null || marker == null)
            return;

        Vector3 up = ARUp.ClosestMarkerAxis(marker, marker.TransformDirection(upLocal).normalized);
        upLocal = marker.InverseTransformDirection(up);

        transform.position = follow.position + marker.right * alongRow + up * above;

        // Text reads correctly when its forward points away from the viewer
        Camera camera = Camera.main;
        if (camera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, camera.transform.up);
    }
}
