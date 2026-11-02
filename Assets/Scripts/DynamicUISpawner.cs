using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UIPrefabGroup
{
	public string groupName;              // e.g., "Array"
	public List<GameObject> prefabs;      // Prefabs in this group
	public Button triggerButton;          // Button that triggers this group
}

public class DynamicUISpawner : MonoBehaviour
{
	[Header("Parent under ScrollView -> Viewport -> Content")]
	public Transform content;

	[Header("Prefab Groups with Buttons")]
	public List<UIPrefabGroup> prefabGroups = new List<UIPrefabGroup>();

	public TextMeshProUGUI heading; // Optional feedback text

    void Start()
	{
		// Hook up button listeners
		foreach (var group in prefabGroups)
		{
			if (group.triggerButton != null)
			{
				string groupName = group.groupName; // Capture for closure
				group.triggerButton.onClick.AddListener(() => SpawnGroup(groupName));
			}
		}

		// Lessons should always open at the top
		ScrollRect scrollRect = content.GetComponentInParent<ScrollRect>(true);
		if (scrollRect != null && scrollRect.GetComponent<ScrollToTopOnEnable>() == null)
			scrollRect.gameObject.AddComponent<ScrollToTopOnEnable>();
	}

	public void SpawnGroup(string groupName)
	{
		// Clear previous UI elements under content
		foreach (Transform child in content)
		{
			Destroy(child.gameObject);
		}

		heading.text = groupName;

		// Prefer the text lesson in Resources/Lessons; the image prefabs are the fallback
		TextAsset lesson = LessonBuilder.Load(groupName);
		if (lesson != null)
		{
			LessonBuilder.Build(content, lesson.text);
			return;
		}

		// Find the correct group and spawn its prefabs
		UIPrefabGroup selectedGroup = prefabGroups.Find(g => g.groupName == groupName);

		if (selectedGroup != null)
		{
			foreach (GameObject prefab in selectedGroup.prefabs)
			{
				if (prefab != null)
				{
					GameObject spawned = Instantiate(prefab, content);
					spawned.GetComponent<RectTransform>().localScale = Vector3.one;
				}
			}
		}
		else
		{
			Debug.LogWarning($"Group '{groupName}' not found.");
		}
	}
}
