using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Image = UnityEngine.UI.Image;

// Tells students what to do in an AR scene: find the marker first, then how to use the buttons.
// The scene's own buttons live under the image target, so they stay hidden until the marker is found.
public class ARGuide : MonoBehaviour
{
    public string topicName = "Array";
    [TextArea] public string controlsHint = "Pick a shape, then tap Add or Remove.";
    public float hintDuration = 6f;

    private ImageTargetBehaviour[] targets;
    private GameObject searchPanel;
    private TextMeshProUGUI searchTitle;
    private TextMeshProUGUI searchBody;
    private GameObject hintPanel;
    private TextMeshProUGUI hintText;
    private bool hasFoundMarker;
    private Coroutine hideHint;

    private void Start()
    {
        BuildUI();
        ShowSearching();

        targets = FindObjectsOfType<ImageTargetBehaviour>();
        foreach (ImageTargetBehaviour target in targets)
            target.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    private void OnDestroy()
    {
        if (targets == null)
            return;
        foreach (ImageTargetBehaviour target in targets)
        {
            if (target != null)
                target.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        // Same rule as DefaultObserverEventHandler: tracked or extended-tracked counts as visible
        bool found = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
        if (!found)
        {
            ShowSearching();
            return;
        }

        searchPanel.SetActive(false);
        if (!hasFoundMarker)
        {
            hasFoundMarker = true;
            ShowMessage(controlsHint);
        }
    }

    // Shows a short message in the bottom hint panel, e.g. a queue overflow warning
    public void ShowMessage(string message)
    {
        if (hintPanel == null)
            return;

        hintText.text = message;
        hintPanel.SetActive(true);
        if (hideHint != null)
            StopCoroutine(hideHint);
        hideHint = StartCoroutine(HideHintAfterDelay());
    }

    private void ShowSearching()
    {
        searchPanel.SetActive(true);
        hintPanel.SetActive(false);
        searchTitle.text = hasFoundMarker ? "Marker lost" : $"Point your camera at the {topicName} marker";
        searchBody.text = hasFoundMarker
            ? $"Aim back at the {topicName} card to keep going."
            : "Hold your phone about 30 cm from the printed card, in good light.";
    }

    private IEnumerator HideHintAfterDelay()
    {
        yield return new WaitForSeconds(hintDuration);
        hintPanel.SetActive(false);
    }

    private void BuildUI()
    {
        Canvas canvas = UiKit.Create("ARGuideCanvas", transform).gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Color panelColor = new Color32(0x1E, 0x18, 0x38, 0xD9);

        // Centered card while searching for the marker
        Image search = UiKit.Panel(canvas.transform, panelColor);
        search.name = "SearchPanel";
        UiKit.Stretch(search.rectTransform, new Vector2(0.16f, 0.34f), new Vector2(0.84f, 0.66f));
        searchPanel = search.gameObject;

        searchTitle = UiKit.Text(search.transform, "", "Poppins-Bold SDF", 46, Color.white, TextAlignmentOptions.Bottom);
        UiKit.Stretch(searchTitle.rectTransform, new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.88f));
        searchBody = UiKit.Text(search.transform, "", "Poppins-Regular SDF", 34, new Color(1, 1, 1, 0.85f), TextAlignmentOptions.Top);
        UiKit.Stretch(searchBody.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.45f));

        // Short controls hint at the bottom once the marker is found, clear of the corner buttons
        Image hint = UiKit.Panel(canvas.transform, panelColor);
        hint.name = "HintPanel";
        UiKit.Stretch(hint.rectTransform, new Vector2(0.2f, 0.04f), new Vector2(0.66f, 0.15f));
        hintPanel = hint.gameObject;

        hintText = UiKit.Text(hint.transform, controlsHint, "Poppins-Regular SDF", 34, Color.white, TextAlignmentOptions.Center);
        UiKit.Stretch(hintText.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f));
    }
}
