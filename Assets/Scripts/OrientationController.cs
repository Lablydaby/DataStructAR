using UnityEngine;

public class OrientationController : MonoBehaviour
{
    public static OrientationController Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Avoid duplicates
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Optional: persists across scenes
    }

    // Call this to switch to landscape mode
    public void SetLandscape()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        // Lock auto-rotation to landscape only
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    // Call this to switch back to portrait (vertical) mode
    public void SetPortrait()
    {
        Screen.orientation = ScreenOrientation.Portrait;

        // Lock auto-rotation to portrait only
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }
}
