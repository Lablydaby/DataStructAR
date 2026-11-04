using TMPro;
using UnityEngine;

// Small 3D labels for AR items, kept next to the item they name and turned to face the camera by ARLabel.
// As in the "AR scenes" design they sit on rounded chips: values in indigo on white, index numbers in red
// on white, and tags (FRONT, REAR, HEAD, NULL, TOP) in white capitals on dark indigo.
public static class ARText
{
    public static readonly Color Ink = Display(new Color32(0x3A, 0x31, 0x6B, 0xFF));
    public static readonly Color IndexRed = Display(new Color32(0xC2, 0x45, 0x3A, 0xFF));

    // The project renders in linear colour space, where 3D text and chip vertex colours aren't converted from
    // sRGB, so design colours would come out washed out without this
    private static Color Display(Color srgb) =>
        QualitySettings.activeColorSpace == ColorSpace.Linear ? srgb.linear : srgb;

    public static TextMeshPro Label(Transform parent, string text, float size, Color color,
        Transform follow, Transform marker, float alongRow, float above)
    {
        GameObject go = new GameObject("Label-" + text);
        go.transform.SetParent(parent, false);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.font = UiKit.Font("Poppins-Bold SDF");
        tmp.text = text;
        // Same visible size whatever the parent's scale (the stack's parent is at half scale)
        tmp.fontSize = size / Mathf.Max(parent.lossyScale.x, 0.0001f);
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.5f);

        ARLabel label = go.AddComponent<ARLabel>();
        label.follow = follow;
        label.marker = marker;
        label.alongRow = alongRow;
        label.above = above;
        label.lift = 0.03f;
        return tmp;
    }

    // A value: indigo number on a white chip
    public static TextMeshPro Value(Transform parent, string text, float size, Transform follow, Transform marker,
        float alongRow, float above)
    {
        TextMeshPro tmp = Label(parent, text, size, Ink, follow, marker, alongRow, above);
        Chip(tmp, Color.white);
        return tmp;
    }

    // A tag: white capitals on a dark indigo chip
    public static TextMeshPro Tag(Transform parent, string text, float size, Transform follow, Transform marker,
        float alongRow, float above)
    {
        TextMeshPro tmp = Label(parent, text.ToUpperInvariant(), size, Color.white, follow, marker, alongRow, above);
        StyleTag(tmp);
        return tmp;
    }

    public static void StyleTag(TextMeshPro tmp)
    {
        tmp.text = tmp.text.ToUpperInvariant();
        tmp.color = Color.white;
        tmp.characterSpacing = 6f;
        tmp.alignment = TextAlignmentOptions.Center;
        Chip(tmp, Ink);
    }

    // Puts (or recolours) a rounded chip behind a label, sized to its text
    public static void Chip(TextMeshPro tmp, Color background)
    {
        Transform existing = tmp.transform.Find("Chip");
        GameObject go = existing != null ? existing.gameObject : null;
        if (go == null)
        {
            go = new GameObject("Chip");
            go.transform.SetParent(tmp.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 0.001f); // Just behind the text, seen from the camera
            go.AddComponent<MeshFilter>().sharedMesh = new Mesh { name = "Chip" };
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ChipMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = 0;
        }
        // Draw the text over its chip
        tmp.sortingOrder = 1;

        tmp.ForceMeshUpdate();
        Vector2 text = tmp.GetPreferredValues(tmp.text);
        float height = text.y * 1.05f;
        float width = text.x + text.y * 0.8f;
        RoundedRect(go.GetComponent<MeshFilter>().sharedMesh, width, height, height * 0.3f, background);
    }

    private static Material chipMaterial;
    // Sprites/Default is always included in builds and tints by vertex colour
    private static Material ChipMaterial => chipMaterial != null ? chipMaterial
        : chipMaterial = new Material(Shader.Find("Sprites/Default")) { name = "AR Chip" };

    // A flat rounded rectangle centred on the origin, facing -Z like the text
    private static void RoundedRect(Mesh mesh, float width, float height, float radius, Color color)
    {
        const int arc = 6;
        radius = Mathf.Min(radius, Mathf.Min(width, height) * 0.5f);
        float hx = width * 0.5f - radius, hy = height * 0.5f - radius;
        Vector2[] centres = { new Vector2(hx, hy), new Vector2(-hx, hy), new Vector2(-hx, -hy), new Vector2(hx, -hy) };

        var vertices = new System.Collections.Generic.List<Vector3> { Vector3.zero };
        for (int c = 0; c < 4; c++)
            for (int i = 0; i <= arc; i++)
            {
                float angle = (c * 90f + i * 90f / arc) * Mathf.Deg2Rad;
                vertices.Add(centres[c] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

        int ring = vertices.Count - 1;
        int[] triangles = new int[ring * 3];
        for (int i = 0; i < ring; i++)
        {
            // Clockwise as seen from -Z, so the face points the same way as the text
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = 1 + (i + 1) % ring;
            triangles[i * 3 + 2] = 1 + i;
        }

        Color[] colors = new Color[vertices.Count];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = color;

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    public static void Move(TextMeshPro label, Transform follow, float alongRow, float above)
    {
        ARLabel arLabel = label.GetComponent<ARLabel>();
        arLabel.follow = follow;
        arLabel.alongRow = alongRow;
        arLabel.above = above;
    }

    // Distinct small numbers for the items in a structure
    public static int NewValue(System.Collections.Generic.ICollection<int> taken)
    {
        int value;
        do
            value = Random.Range(1, 100);
        while (taken.Contains(value));
        return value;
    }
}
