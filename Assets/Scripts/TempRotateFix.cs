using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempRotateFix : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
		SetLandscape();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void SetLandscape()
	{
		Screen.orientation = ScreenOrientation.LandscapeLeft;

		// Optional: lock auto-rotation to landscape only
		Screen.autorotateToPortrait = false;
		Screen.autorotateToPortraitUpsideDown = false;
		Screen.autorotateToLandscapeLeft = true;
		Screen.autorotateToLandscapeRight = true;
		Screen.orientation = ScreenOrientation.AutoRotation;
	}
}
