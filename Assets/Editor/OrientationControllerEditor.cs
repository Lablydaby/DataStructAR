using UnityEngine;
using UnityEditor;

public static class OrientationControllerMenu
{
    [MenuItem("Tools/Set Orientation/Portrait")]
    public static void SetPortrait()
    {
        CallOrientationMethod("SetPortrait");
    }

    [MenuItem("Tools/Set Orientation/Landscape")]
    public static void SetLandscape()
    {
        CallOrientationMethod("SetLandscape");
    }

    private static void CallOrientationMethod(string methodName)
    {
        OrientationController controller = Object.FindObjectOfType<OrientationController>();
        if (controller == null)
        {
            Debug.LogError("OrientationController not found in the scene. Please add it to a GameObject.");
            return;
        }

        var method = typeof(OrientationController).GetMethod(methodName);
        if (method != null)
        {
            method.Invoke(controller, null);
            Debug.Log($"Called {methodName} on OrientationController.");
        }
        else
        {
            Debug.LogError($"Method '{methodName}' not found in OrientationController.");
        }
    }
}
