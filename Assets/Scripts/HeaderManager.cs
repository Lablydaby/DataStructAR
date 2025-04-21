using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HeaderManager : MonoBehaviour
{
    [Header("UI References")]
    public Animator panelAnimator;
    public TextMeshProUGUI headingText;
    public Button toggleButton;
    public RectTransform arrowIcon;

    [Header("Text Settings")]
    public string openHeading = "Panel Opened";
    public string closedHeading = "Panel Closed";

    public bool isOpen = false;

    private void Start()
    {
        //toggleButton.onClick.AddListener(TogglePanel);
        TogglePanel();
        //UpdateUI();
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;

        // Trigger animation
        if (isOpen)
        {
            panelAnimator.Play("HeaderOpen");

        }
        else
        {
            panelAnimator.Play("HeaderClose");

        }

        // Update UI elements
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Change heading text
        headingText.text = isOpen ? openHeading : closedHeading;

        // Flip arrow icon (rotate Z by 180 degrees)
        arrowIcon.localRotation = Quaternion.Euler(0, 0, isOpen ? 180f : 0f);
    }
}
