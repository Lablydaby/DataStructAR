using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// Array practice: insert a value at an index. Like a real array insert, the elements from that
// index onward shift one slot right to make room, so the last slot starts empty.
public class InsertValueManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField indexInput;
    public TMP_InputField valueInput;
    public Button insertButton;
    public TextMeshProUGUI feedbackText;

    [Header("Buttons")]
    public List<Button> arrayButtons; // List of button objects representing the array

    [Header("Correctness Check")]
    public string rightIndex;
    public string rightValue = "45";
    public float shiftStepSeconds = 0.3f;

    [Header("Scene Control")]
    public GameObject quizCanvas;
    public GameObject MenuCanvas;
    public OrientationController uiManager;

    private string[] startValues;
    private string[] values;
    private int count; // Filled slots; the rest at the end are empty
    private bool isBusy;

    private void Start()
    {
        indexInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        valueInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        indexInput.characterLimit = 2;
        valueInput.characterLimit = 4;

        // Force TMP to re-validate the new content type
        indexInput.ForceLabelUpdate();
        valueInput.ForceLabelUpdate();

        // Keep the scene's numbers but free the last slot, so an insert has room to shift into
        startValues = new string[arrayButtons.Count];
        for (int i = 0; i < arrayButtons.Count - 1; i++)
            startValues[i] = ButtonText(i).text.Trim();
        startValues[arrayButtons.Count - 1] = "";

        // Room for a two-line message across the screen
        feedbackText.font = UiKit.Font("Poppins-Regular SDF");
        feedbackText.enableAutoSizing = true;
        feedbackText.fontSizeMin = 30;
        feedbackText.fontSizeMax = 56;
        RectTransform feedbackRect = feedbackText.rectTransform;
        feedbackRect.anchorMin = new Vector2(0.08f, feedbackRect.anchorMin.y);
        feedbackRect.anchorMax = new Vector2(0.92f, feedbackRect.anchorMax.y);

        ResetArray();

        insertButton.interactable = false;
        feedbackText.gameObject.SetActive(false);

        indexInput.onValueChanged.AddListener(OnAnyInputChanged);
        valueInput.onValueChanged.AddListener(OnAnyInputChanged);
    }


    private void OnDestroy()
    {
        indexInput.onValueChanged.RemoveListener(OnAnyInputChanged);
        valueInput.onValueChanged.RemoveListener(OnAnyInputChanged);
    }

    private void OnAnyInputChanged(string _)
    {
        insertButton.interactable = !isBusy &&
                                    !string.IsNullOrWhiteSpace(indexInput.text) &&
                                    !string.IsNullOrWhiteSpace(valueInput.text);
        if (!isBusy)
            feedbackText.gameObject.SetActive(false);
    }

    public void OnInsertButtonClicked()
    {
        if (isBusy)
            return;

        if (!int.TryParse(indexInput.text.Trim(), out int index))
        {
            ShowFeedback("Type a whole number for the index.");
            return;
        }

        if (index < 0 || index >= arrayButtons.Count)
        {
            ShowFeedback($"Index out of bounds. This array's indexes go from 0 to {arrayButtons.Count - 1}.");
            return;
        }

        // Arrays are contiguous: the new element can go at most right after the last one
        if (index > count)
        {
            ShowFeedback($"Arrays can't have gaps. The next free index is {count}.");
            return;
        }

        if (count == arrayButtons.Count)
        {
            ShowFeedback("The array is full, so nothing can shift to make room.");
            return;
        }

        StartCoroutine(InsertWithShift(index, valueInput.text.Trim()));
    }

    private IEnumerator InsertWithShift(int index, string value)
    {
        isBusy = true;
        insertButton.interactable = false;
        feedbackText.gameObject.SetActive(false);

        // Move elements right one at a time, starting from the end, so students see each shift
        for (int i = count - 1; i >= index; i--)
        {
            values[i + 1] = values[i];
            values[i] = "";
            Refresh();
            yield return new WaitForSeconds(shiftStepSeconds);
        }

        values[index] = value;
        count++;
        Refresh();

        int shifted = count - 1 - index;
        string shiftNote = shifted == 0 ? "Nothing had to shift."
            : shifted == 1 ? $"{values[index + 1]} shifted right to index {index + 1} to make room."
            : $"{shifted} elements shifted right to make room.";

        if (value == rightValue && index.ToString() == rightIndex)
        {
            ShowFeedback($"Correct! {shiftNote}");
            Progress.MarkPracticeDone("Array");
            yield return new WaitForSeconds(2.5f);
            isBusy = false;
            LoadMenu();
            yield break;
        }

        string goal = value == rightValue ? $"{rightValue} belongs at index {rightIndex}." : $"The task is to insert {rightValue} at index {rightIndex}.";
        ShowFeedback($"{shiftNote} But {goal} Try again.");
        yield return new WaitForSeconds(3f);

        ResetArray();
        isBusy = false;
        OnAnyInputChanged("");
    }

    private void ResetArray()
    {
        values = (string[])startValues.Clone();
        count = arrayButtons.Count - 1;
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < arrayButtons.Count; i++)
        {
            ButtonText(i).text = values[i];
            // Empty slots fade so the free space is visible
            arrayButtons[i].image.color = string.IsNullOrEmpty(values[i]) ? new Color(1, 1, 1, 0.3f) : Color.white;
        }
    }

    private TextMeshProUGUI ButtonText(int index)
    {
        return arrayButtons[index].GetComponentInChildren<TextMeshProUGUI>();
    }

    private void ShowFeedback(string message)
    {
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
    }

    private void LoadMenu()
    {
        quizCanvas.SetActive(false);
        MenuCanvas.SetActive(true);
        uiManager.SetPortrait();
    }
}
