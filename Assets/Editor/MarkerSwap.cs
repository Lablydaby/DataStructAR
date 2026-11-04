using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vuforia;
using Object = UnityEngine.Object;

// Dev tool: switches the AR scenes' image targets to the branded stage cards in Markers/, as
// Instant Image Targets (built from the image itself, so no Vuforia Target Manager database).
// Run from the Build menu, or: Unity.exe -batchmode -projectPath <project> -executeMethod MarkerSwap.ApplyAndQuit
public static class MarkerSwap
{
    private const string SourceFolder = "Markers";
    private const string AssetFolder = "Assets/Markers";

    private static readonly (string scene, string card)[] Scenes =
    {
        ("Assets/Scenes/AR/Array.unity", "array"),
        ("Assets/Scenes/AR/LinkedList.unity", "linkedlist"),
        ("Assets/Scenes/AR/Stack.unity", "stack"),
        ("Assets/Scenes/AR/queue.unity", "queue"),
    };

    [MenuItem("Build/Use Stage-Card Markers")]
    public static void Apply()
    {
        Directory.CreateDirectory(AssetFolder);
        foreach ((string scenePath, string card) in Scenes)
        {
            Texture2D texture = ImportCard(card);
            EditorSceneManager.OpenScene(scenePath);

            ImageTargetBehaviour target = Object.FindObjectsOfType<ImageTargetBehaviour>()
                .First(t => t.gameObject.scene.path == scenePath);
            UseInstantTarget(target, texture, card);

            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[MarkerSwap] {scenePath}: {target.name} now tracks {card}-marker ({texture.width}x{texture.height})");
        }
        AssetDatabase.SaveAssets();
    }

    // How far to move each scene's start point, in card units (x across, y off the card, z up the card),
    // measured with ScreenshotTour.RunMeasureAndQuit at full capacity, so the content sits centred in the
    // card's white area. Re-measure and set these again whenever the cards or layouts change.
    private static readonly (string scene, Vector3 shift)[] Recentering =
    {
        ("Assets/Scenes/AR/Array.unity", new Vector3(0.045f, 0f, 0f)),
        ("Assets/Scenes/AR/LinkedList.unity", new Vector3(-0.05f, 0f, 0f)),
    };

    [MenuItem("Build/Center Content On Stage Cards")]
    public static void Recenter()
    {
        foreach ((string scenePath, Vector3 shift) in Recentering)
        {
            EditorSceneManager.OpenScene(scenePath);
            ImageTargetBehaviour target = Object.FindObjectsOfType<ImageTargetBehaviour>()
                .First(t => t.gameObject.scene.path == scenePath);

            Transform start = FindStartPoint();
            Undo.RecordObject(start, "Center content on card");
            start.position += target.transform.TransformVector(shift);
            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[MarkerSwap] {scenePath}: moved {start.name} by {shift} card units");
        }
    }

    public static void RecenterAndQuit()
    {
        try
        {
            Recenter();
        }
        catch (Exception e)
        {
            Debug.LogError($"[MarkerSwap] Failed: {e}");
            EditorApplication.Exit(1);
        }
        EditorApplication.Exit(0);
    }

    private static Transform FindStartPoint()
    {
        ArrayManager array = Object.FindObjectOfType<ArrayManager>(true);
        if (array != null) return array.startPosition;
        LinkedListManager list = Object.FindObjectOfType<LinkedListManager>(true);
        if (list != null) return list.startPosition;
        QueueManager queue = Object.FindObjectOfType<QueueManager>(true);
        if (queue != null) return queue.startPosition;
        return Object.FindObjectOfType<StackManager>(true).startPos;
    }

    public static void ApplyAndQuit()
    {
        try
        {
            Apply();
        }
        catch (Exception e)
        {
            Debug.LogError($"[MarkerSwap] Failed: {e}");
            EditorApplication.Exit(1);
        }
        EditorApplication.Exit(0);
    }

    private static Texture2D ImportCard(string card)
    {
        string assetPath = $"{AssetFolder}/{card}-marker.jpg";
        File.Copy(Path.Combine(SourceFolder, $"{card}-marker.jpg"), assetPath, true);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        // Vuforia reads the pixels at runtime, so the texture must stay readable and uncompressed
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        CallVuforiaEditor("SetRequiredInstantImageTargetTextureImportSettings", importer);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static void UseInstantTarget(ImageTargetBehaviour target, Texture2D texture, string card)
    {
        float aspect = (float)texture.height / texture.width;

        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty("mImageTargetType").intValue = (int)ImageTargetType.INSTANT;
        serialized.FindProperty("mTrackableName").stringValue = card;
        serialized.FindProperty("mDataSetPath").stringValue = "";
        serialized.FindProperty("mRuntimeTexture").objectReferenceValue = texture;
        serialized.FindProperty("mAspectRatio").floatValue = aspect;
        serialized.FindProperty("mWidth").floatValue = 1f; // Same size as the old targets, so the content keeps its scale
        serialized.FindProperty("mHeight").floatValue = aspect;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // Let Vuforia's own inspector code refresh the preview mesh and internal data, as if set by hand
        Type wrapperType = VuforiaEditorType("Vuforia.EditorClasses.SerializedImageTarget");
        object wrapper = wrapperType?.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(c => TryConstruct(c, serialized))
            .FirstOrDefault(o => o != null);
        if (wrapper == null)
        {
            Debug.LogWarning("[MarkerSwap] Couldn't wrap the target for Vuforia's editor; preview may be stale until it's selected");
            return;
        }

        // Its setter also refreshes the preview, the way picking a texture in the inspector does
        PropertyInfo textureProperty = wrapperType.GetProperty("InstantImageTargetTexture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        try
        {
            textureProperty?.SetValue(wrapper, texture);
        }
        catch (TargetInvocationException e)
        {
            Debug.LogWarning($"[MarkerSwap] Vuforia's preview refresh failed, the target itself is still set: {e.InnerException?.Message}");
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static object TryConstruct(ConstructorInfo constructor, SerializedObject serialized)
    {
        ParameterInfo[] parameters = constructor.GetParameters();
        if (parameters.Length != 1 || !parameters[0].ParameterType.IsAssignableFrom(typeof(SerializedObject)))
            return null;
        return constructor.Invoke(new object[] { serialized });
    }

    private static Type VuforiaEditorType(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Vuforia.Unity.Editor")
            ?.GetType(name);
    }

    // Instance method on Vuforia's ImageTargetEditor; any instance will do
    private static void CallVuforiaEditor(string methodName, object argument)
    {
        Type editorType = VuforiaEditorType("Vuforia.EditorClasses.ImageTargetEditor");
        MethodInfo method = editorType?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
        {
            Debug.LogWarning($"[MarkerSwap] Vuforia's {methodName} not found; using our own import settings");
            return;
        }
        ScriptableObject instance = ScriptableObject.CreateInstance(editorType);
        try
        {
            method.Invoke(instance, new[] { argument });
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
