using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared layout and flow for the landscape practice puzzles (v4 design): instructions banner at the top,
// the structure in the middle, controls on an indigo bar at the bottom, and a success card that saves
// "Practice done" and returns to the menu.
public abstract class PuzzleScreen : MonoBehaviour
{
    protected static readonly Vector2 Landscape = new Vector2(844, 390);

    protected RectTransform root, stage, controls;
    protected abstract string Topic { get; }
    protected abstract string Instructions { get; }
    protected virtual string DefaultMessage => "";
    protected virtual float SuccessSeconds => 2f;

    private RectTransform bannerOpen, bannerClosed;
    private TextMeshProUGUI instructionText, messageText;
    private Image closedDot;
    private GameObject successOverlay;
    private TextMeshProUGUI successText;
    private RectTransform successBar;
    private bool built;
    protected bool Solved { get; private set; }

    private void Awake()
    {
        root = DK.Screen(transform, Landscape, true, DK.White, "Puzzle");

        stage = UiKit.Create("Stage", root);
        DK.Stretch(stage, 0, 84, 0, 84);

        Image bar = DK.Box(root, DK.Indigo700, 0, "Controls");
        DK.BottomRow(bar, 0, 0, 0, 84);
        controls = bar.rectTransform;

        Image back = DK.Box(root, DK.Lavender100, 12, "Back");
        DK.At(back, 16, 14, 44, 44);
        DK.MakeButton(back, Close);
        RectTransform arrow = DK.Icon(back.transform, "arrow-left", 22, DK.Indigo900);
        DK.Anchor(arrow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));

        BuildBanner();
        BuildSuccess();
        Build();
        built = true;
    }

    private void OnEnable()
    {
        if (!built)
            return;
        StopAllCoroutines();
        Solved = false;
        successOverlay.SetActive(false);
        SetBanner(true);
        ResetPuzzle();
        Message(DefaultMessage, DK.Indigo500);
    }

    protected abstract void Build();
    protected abstract void ResetPuzzle();

    private void BuildBanner()
    {
        Image open = DK.Box(root, DK.Lavender100, 14, "Banner");
        bannerOpen = open.rectTransform;
        bannerOpen.anchorMin = new Vector2(0, 1);
        bannerOpen.anchorMax = new Vector2(1, 1);
        bannerOpen.pivot = new Vector2(0.5f, 1);
        bannerOpen.offsetMin = new Vector2(70, -14);
        bannerOpen.offsetMax = new Vector2(-16, -14);
        HorizontalLayoutGroup row = DK.Line(open.gameObject, 10);
        row.padding = new RectOffset(16, 6, 8, 8);
        open.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform texts = UiKit.Create("Texts", open.transform);
        DK.Column(texts.gameObject, 4);
        DK.Size(texts, -1, -1, 1);
        instructionText = DK.Text(texts, "", 15, DK.Indigo900, true);
        messageText = DK.Text(texts, "", 13, DK.Indigo500, true);

        Image hide = DK.Box(open.transform, DK.Clear, 10, "Hide");
        DK.Size(hide, 36, 36);
        DK.MakeButton(hide, () => SetBanner(false));
        RectTransform up = DK.Icon(hide.transform, "chevron-up", 20, DK.Indigo900);
        DK.Anchor(up, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));

        Button closed = DK.Pill(root, "Instructions", DK.Lavender100, DK.Indigo900, 12, 14, () => SetBanner(true), null, "chevron-down");
        bannerClosed = (RectTransform)closed.transform;
        DK.At(bannerClosed, 70, 14, 150, 44);
        closedDot = DK.Box(closed.transform.Find("Row"), DK.Clear, 4, "Dot");
        DK.Size(closedDot, 8, 8);
        closedDot.transform.SetAsFirstSibling();
    }

    protected void SetBanner(bool open)
    {
        bannerOpen.gameObject.SetActive(open);
        bannerClosed.gameObject.SetActive(!open);
    }

    // Message under the instructions; errors reopen the banner so students see them
    protected void Message(string text, Color color)
    {
        instructionText.text = Instructions;
        messageText.text = text;
        messageText.color = color;
        messageText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        closedDot.color = string.IsNullOrEmpty(text) ? DK.Clear : color;
    }

    protected void Error(string text)
    {
        Message(text, DK.Red);
        SetBanner(true);
    }

    protected void Success(string text)
    {
        if (Solved)
            return;
        Solved = true;
        Message(text, DK.Green);
        StartCoroutine(SuccessThenClose(text));
    }

    private void BuildSuccess()
    {
        Image dim = DK.Box(root, new Color(DK.Indigo900.r, DK.Indigo900.g, DK.Indigo900.b, 0.42f), 0, "Success");
        UiKit.Stretch(dim.rectTransform, Vector2.zero, Vector2.one);
        dim.raycastTarget = true;
        successOverlay = dim.gameObject;

        Image card = DK.Box(dim.transform, DK.White, 24, "Card");
        DK.Anchor(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 0));
        DK.Column(card.gameObject, 14, new RectOffset(28, 28, 28, 24), TextAnchor.UpperCenter);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform badgeCell = UiKit.Create("BadgeCell", card.transform);
        DK.Size(badgeCell, -1, 80);
        Image badge = DK.Outlined(badgeCell, DK.Green, DK.GreenTint, 40, 8, "Badge");
        DK.Anchor(badge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80, 80));
        RectTransform check = DK.Icon(badge.transform, "check", 30, DK.White);
        DK.Anchor(check, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30));

        successText = DK.Text(card.transform, "", 19, DK.Indigo900, true, TextAlignmentOptions.Top);
        successText.alignment = TextAlignmentOptions.Center;

        Image track = DK.Box(card.transform, DK.Lavender100, 3, "Track");
        DK.Size(track, -1, 6);
        Image fill = DK.Box(track.transform, DK.Indigo700, 3, "Fill");
        successBar = fill.rectTransform;
        successBar.anchorMin = Vector2.zero;
        successBar.anchorMax = new Vector2(0, 1);
        successBar.offsetMin = successBar.offsetMax = Vector2.zero;

        TextMeshProUGUI saving = DK.Text(card.transform, "Saving \"Practice done\" and returning to the menu", 13, DK.Indigo500, false, TextAlignmentOptions.Center);
        saving.alignment = TextAlignmentOptions.Center;
        successOverlay.SetActive(false);
    }

    private IEnumerator SuccessThenClose(string text)
    {
        yield return new WaitForSeconds(0.6f);
        successText.text = text;
        successOverlay.SetActive(true);
        for (float t = 0; t < SuccessSeconds; t += Time.deltaTime)
        {
            successBar.anchorMax = new Vector2(t / SuccessSeconds, 1);
            yield return null;
        }
        Progress.MarkPracticeDone(Topic);
        Close();
    }

    // Back to the menu underneath, in portrait
    protected void Close()
    {
        OrientationController.Instance.SetPortrait();
        gameObject.SetActive(false);
        AppScreens screens = FindObjectOfType<AppScreens>();
        if (screens != null)
            screens.ShowMenu();
    }

    // ---- Shared pieces for the puzzles ----

    // A slot: filled with an indigo block and label, or an empty light outline
    protected class Slot
    {
        public Image outer, fill;
        public TextMeshProUGUI label;
        public Image glow;

        public void Set(string value, float fontSize = 26)
        {
            bool empty = string.IsNullOrEmpty(value);
            outer.color = empty ? DK.Dash : DK.Indigo700;
            fill.color = empty ? DK.White : DK.Indigo700;
            label.text = value ?? "";
            label.fontSize = fontSize;
        }

        public void Highlight(Color? color)
        {
            glow.gameObject.SetActive(color.HasValue);
            if (color.HasValue)
                glow.color = color.Value;
        }
    }

    protected static Slot MakeSlot(Transform parent, Vector2 size, float radius)
    {
        Slot slot = new Slot();
        RectTransform cell = UiKit.Create("Slot", parent);
        DK.Size(cell, size.x, size.y);
        slot.glow = DK.Outlined(cell, DK.White, DK.Lavender400, radius + 5, 2, "Glow");
        DK.Stretch(slot.glow.rectTransform, -5, -5, -5, -5);
        slot.glow.gameObject.SetActive(false);
        slot.outer = DK.Outlined(cell, DK.White, DK.Dash, radius, 2, "Block");
        UiKit.Stretch(slot.outer.rectTransform, Vector2.zero, Vector2.one);
        slot.fill = DK.Fill(slot.outer);
        slot.label = DK.Text(slot.outer.transform, "", 26, DK.White, true, TextAlignmentOptions.Center);
        UiKit.Stretch(slot.label.rectTransform, Vector2.zero, Vector2.one);
        slot.label.enableWordWrapping = false;
        return slot;
    }

    protected RectTransform ControlRow(float spacing = 12)
    {
        RectTransform row = UiKit.Create("Row", controls);
        DK.Anchor(row, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 60));
        DK.Line(row.gameObject, spacing, TextAnchor.MiddleCenter);
        return row;
    }

    protected static Button OutlineButton(Transform parent, string label, float w, float h, System.Action onClick, string icon = null, float fontSize = 16)
    {
        Button button = DK.Pill(parent, label, DK.Indigo700, DK.White, 10, fontSize, onClick, icon, null, DK.Lavender50, true);
        DK.Size(button, w, h);
        return button;
    }

    protected static Button LightButton(Transform parent, string label, float w, float h, System.Action onClick, string icon = null)
    {
        Button button = DK.Pill(parent, label, DK.Lavender50, DK.Indigo900, 10, 16, onClick, icon, null, null, true);
        DK.Size(button, w, h);
        return button;
    }

    protected static TMP_InputField Input(Transform parent, string placeholder, float w, float h, Color bg, int limit)
    {
        Image box = DK.Box(parent, bg, 10, "Input");
        DK.Size(box, w, h);
        box.raycastTarget = true;
        RectTransform area = UiKit.Create("Text Area", box.transform);
        DK.Stretch(area, 14, 0, 12, 0);
        area.gameObject.AddComponent<RectMask2D>();
        TextMeshProUGUI hint = DK.Text(area, placeholder, 17, DK.Lavender400, false, TextAlignmentOptions.MidlineLeft, "Placeholder");
        UiKit.Stretch(hint.rectTransform, Vector2.zero, Vector2.one);
        hint.enableWordWrapping = false;
        TextMeshProUGUI text = DK.Text(area, "", 17, DK.Indigo900, false, TextAlignmentOptions.MidlineLeft, "Text");
        UiKit.Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        text.enableWordWrapping = false;

        TMP_InputField field = box.gameObject.AddComponent<TMP_InputField>();
        field.textViewport = area;
        field.textComponent = text;
        field.placeholder = hint;
        field.targetGraphic = box;
        field.contentType = TMP_InputField.ContentType.IntegerNumber;
        field.characterLimit = limit;
        field.fontAsset = text.font;
        field.pointSize = 17;
        return field;
    }

    protected static TextMeshProUGUI SmallCaps(Transform parent, string text, Color color, float size = 11)
    {
        return DK.Caps(parent, text, size, color, TextAlignmentOptions.Center);
    }
}
