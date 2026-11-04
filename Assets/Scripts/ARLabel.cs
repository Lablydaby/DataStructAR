using UnityEngine;

// Keeps a 3D text label (e.g. Head, Null) next to the object it names, offset along the marker's
// row and above it in real-world terms, and always turned to face the camera so it reads correctly.
// With a gap set, the label instead keeps clear of the object's actual shape: it sits that far past the
// shape's edge (above or below it, or beside it for a sideways label), so it never covers the shape.
public class ARLabel : MonoBehaviour
{
    public Transform follow;   // Object the label sits next to
    public Transform marker;   // Frame the row of objects is laid out in
    public float alongRow;     // World units along the marker's right axis
    public float above;        // World units up
    public float lift;         // World units off the card's surface, so it never sits behind the card
    public float gap = -1f;    // When 0 or more: world units of space kept between the shape and the label
    public float extra;        // With a gap: further distance out, e.g. to sit one row below another label
    public Transform clearOf;  // With a gap: also keep clear of this (e.g. the slot an element sits in)

    private Vector3 upLocal;
    private MeshFilter chip;

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

        Vector3 offset = marker.right * alongRow + up * above;
        float height = lift;
        if (gap >= 0f && ShapeBounds(out Bounds bounds))
        {
            // Clear the shape in the direction the label points: up/down, or along the row
            bool vertical = Mathf.Abs(above) >= Mathf.Abs(alongRow);
            Vector3 direction = vertical ? up * Mathf.Sign(above) : marker.right * Mathf.Sign(alongRow);
            Vector3 side = vertical ? marker.right * alongRow : up * above;
            float shapeEdge = Vector3.Dot(bounds.center - follow.position, direction) + Extent(bounds, direction);
            offset = direction * (shapeEdge + gap + extra + HalfSize(vertical)) + side;

            // Beside a shape on the card, sit level with the shape's front: nearer the camera than that,
            // perspective would push the chip back over the shape
            if (Mathf.Abs(Vector3.Dot(direction, marker.up)) < 0.5f)
            {
                float front = Vector3.Dot(bounds.center - follow.position, marker.up) + Extent(bounds, marker.up);
                height = Mathf.Max(lift, front + 0.002f);
            }
        }

        transform.position = follow.position + offset + marker.up * height;

        // Text reads correctly when its forward points away from the viewer
        Camera camera = Camera.main;
        if (camera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, camera.transform.up);
    }

    // The visible shape of what the label follows (a node includes its arrow)
    private bool ShapeBounds(out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        Add(follow, ref bounds, ref any);
        if (clearOf != null)
            Add(clearOf, ref bounds, ref any);
        return any;
    }

    private void Add(Transform shape, ref Bounds bounds, ref bool any)
    {
        foreach (Renderer renderer in shape.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || renderer.transform.IsChildOf(transform))
                continue;
            if (any)
                bounds.Encapsulate(renderer.bounds);
            else
                bounds = renderer.bounds;
            any = true;
        }
    }

    private static float Extent(Bounds bounds, Vector3 direction) =>
        Mathf.Abs(direction.x) * bounds.extents.x + Mathf.Abs(direction.y) * bounds.extents.y
        + Mathf.Abs(direction.z) * bounds.extents.z;

    // Half the label's own height (or width, for a sideways label), from its chip
    private float HalfSize(bool vertical)
    {
        if (chip == null)
            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
                if (filter.name == "Chip") // Not the text's own mesh
                    chip = filter;
        if (chip == null || chip.sharedMesh == null)
            return 0f;
        Vector3 extents = chip.sharedMesh.bounds.extents;
        Vector3 scale = chip.transform.lossyScale;
        return vertical ? extents.y * scale.y : extents.x * scale.x;
    }
}
