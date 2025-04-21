using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UiManager : MonoBehaviour
{
	public float splashTime = 2f;
	public GameObject splash1;
	public GameObject splash2;
	public GameObject options;
	public GameObject info;
	private IEnumerator Start()
	{
		splash1.SetActive(true);
		yield return new WaitForSeconds(splashTime);
		splash1.SetActive(false);
		splash2.SetActive(true);
	}

	public void ReloadBtn()
	{
		SceneManager.LoadScene("MainMenu");
	}

    public void SetPortrait()
    {
        Screen.orientation = ScreenOrientation.Portrait;

        // Optional: lock auto-rotation to portrait only
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }
}
