using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("Scene Control")]
    public GameObject quizCanvas;
    public GameObject MenuCanvas;
    public OrientationController uiManager;

    private void Start()
    {
        //// Only allow numeric input
        //indexInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        //valueInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        // Force TMP to re-validate the new content type
        indexInput.ForceLabelUpdate();
        valueInput.ForceLabelUpdate();

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
        insertButton.interactable = !string.IsNullOrWhiteSpace(indexInput.text) &&
                                    !string.IsNullOrWhiteSpace(valueInput.text);
        feedbackText.gameObject.SetActive(false);
    }

    public void OnInsertButtonClicked()
    {
        if (!int.TryParse(indexInput.text.Trim(), out int index))
        {
            feedbackText.text = "Invalid index.";
            feedbackText.gameObject.SetActive(true);
            return;
        }

        if (index < 0 || index >= arrayButtons.Count)
        {
            feedbackText.text = "Index out of bounds.";
            feedbackText.gameObject.SetActive(true);
            return;
        }

        string value = valueInput.text.Trim();

        TextMeshProUGUI textComponent = arrayButtons[index].GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = value;

            if (value == "45" && index.ToString() == rightIndex)
            {
                feedbackText.text = "Correct value.";
                Invoke("LoadMenu", 1f);
            }
            else
            {
                feedbackText.text = "Wrong value.";
            }

            feedbackText.gameObject.SetActive(true);
            
        }
    }

    private void LoadMenu()
    {
        quizCanvas.SetActive(false);
        MenuCanvas.SetActive(true);
        uiManager.SetPortrait();
    }
}
