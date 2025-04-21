using UnityEngine;
using UnityEngine.SceneManagement;

public class ARBackHandler : MonoBehaviour
{
	private void Start()
	{
		SetLandscape();
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






	public void GoBackToQuiz()
	{
		PlayerPrefs.SetInt("ReturnedFromAR", 1);
		PlayerPrefs.Save();
		SetPortrait();

		SceneManager.LoadScene("MainMenu"); // Replace with your actual scene name
	}

	public void Exit()
	{

        PlayerPrefs.SetInt("ReturnedFromAR", 2);
        SetPortrait();

        SceneManager.LoadScene("MainMenu"); // Replace with your actual scene name
    }
}
