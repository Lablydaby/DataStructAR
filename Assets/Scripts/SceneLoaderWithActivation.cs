using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoaderWithActivation : MonoBehaviour
{
	[Header("Set in Inspector")]
	public Button loadButton;                // Reference to the button
	public string sceneToLoad;              // Name of the scene to load
	public string gameObjectToEnableName;   // Name of the GameObject to enable after scene load

	void Start()
	{
		if (loadButton != null)
		{
			loadButton.onClick.AddListener(OnLoadButtonClicked);
		}
	}

	void OnLoadButtonClicked()
	{
		// Start loading the scene asynchronously
		SceneManager.sceneLoaded += OnSceneLoaded;
		SceneManager.LoadScene(sceneToLoad);
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// Enable the GameObject after the scene is loaded
		GameObject objToEnable = GameObject.Find(gameObjectToEnableName);
		if (objToEnable != null)
		{
			objToEnable.SetActive(true);
		}
		else
		{
			Debug.LogWarning($"GameObject named '{gameObjectToEnableName}' not found in scene.");
		}

		// Unsubscribe to avoid memory leak
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}
}
