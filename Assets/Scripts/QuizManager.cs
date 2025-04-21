using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class QuizQuestion
    {
        public string question;
        public string[] options;
        public int correctAnswerIndex;
    }

    [System.Serializable]
    public class QuizCategory
    {
        public string categoryName;
        public List<QuizQuestion> questions;
    }

    public List<QuizCategory> quizCategories;

    [Header("UI Elements")]
    public TMP_Text questionText;
    [Header("Progress Display")]
    public TMP_Text questionNumberText;
    [Space(20)]
    public Button[] optionButtons;
    public TMP_Text[] optionTexts;
    public TMP_Text resultText;

    [Header("Star Rating")]
    public Image[] starImages;
    public Sprite normalStarSprite;
    public Sprite glowingStarSprite;

    [Header("Category Buttons")]
    public Button arrayButton;
    public Button linkedListButton;
    public Button stackButton;
    public Button queueButton;

    [Header("AR Button")]
    public Button startARButton;

    [Header("Completion Object")]
    public GameObject onQuizCompleteObject;

    [Header("Quiz Panel")]
    public GameObject quizPanel;

    public GameObject resultPanel;
    public GameObject optionPanel;

    private List<QuizQuestion> currentQuestions;
    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool isReturningFromAR = false;


    [Header("Completion Panels Per Category")]
    public GameObject arrayCompletePanel;
    public GameObject linkedListCompletePanel;
    public GameObject stackCompletePanel;
    public GameObject queueCompletePanel;



    void Start()
    {
        Debug.Log("QuizManager started.");

        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
            Debug.Log("Quiz panel hidden by default.");
        }

        arrayButton.onClick.AddListener(() => SelectCategory("Array"));
        linkedListButton.onClick.AddListener(() => SelectCategory("Linked List"));
        stackButton.onClick.AddListener(() => SelectCategory("Stack"));
        queueButton.onClick.AddListener(() => SelectCategory("Queue"));

        startARButton.onClick.AddListener(LoadARScene);

        if (onQuizCompleteObject != null)
            onQuizCompleteObject.SetActive(false);

        ResetStars();

        if (PlayerPrefs.HasKey("ReturnedFromAR") && PlayerPrefs.GetInt("ReturnedFromAR") == 1)
        {
            isReturningFromAR = true;
            PlayerPrefs.SetInt("ReturnedFromAR", 0);
            PlayerPrefs.Save();
            Debug.Log("Returned from AR detected.");

            string category = PlayerPrefs.GetString("SelectedCategory", "");
            Debug.Log("Last selected category: " + category);

            if (!string.IsNullOrEmpty(category))
            {
                if (quizPanel != null)
                    quizPanel.SetActive(true);

                StartQuiz(category);
            }
        }

        if (PlayerPrefs.HasKey("ReturnedFromAR") && PlayerPrefs.GetInt("ReturnedFromAR") == 2)
        {
            isReturningFromAR = false;
            PlayerPrefs.SetInt("ReturnedFromAR", 0);
            PlayerPrefs.Save();
            Debug.Log("Returned from AR detected Back.");

            string category = PlayerPrefs.GetString("SelectedCategory", "");
            Debug.Log("Last selected category: " + category);
            optionPanel.SetActive(true);
            
        }
    }

    public void SelectCategory(string categoryName)
    {
        Debug.Log("Category selected: " + categoryName);
        PlayerPrefs.SetString("SelectedCategory", categoryName);
        PlayerPrefs.Save();

        // Do not show quiz panel or start quiz here.
    }

    public void StartQuiz(string categoryName)
    {
        Debug.Log("Starting quiz for: " + categoryName);

        QuizCategory selectedCategory = quizCategories.Find(cat => cat.categoryName == categoryName);

        if (selectedCategory != null)
        {
            currentQuestions = new List<QuizQuestion>(selectedCategory.questions);
            currentQuestionIndex = 0;
            score = 0;

            if (onQuizCompleteObject != null)
                onQuizCompleteObject.SetActive(false);

            if (quizPanel != null && isReturningFromAR)
            {
                quizPanel.SetActive(true);
                Debug.Log("Quiz panel enabled (returning from AR).");
            }

            ResetStars();
            DisplayQuestion();
        }
        else
        {
            Debug.LogError("Category not found: " + categoryName);
        }
    }

    void DisplayQuestion()
    {
        if (currentQuestionIndex < currentQuestions.Count)
        {
            QuizQuestion question = currentQuestions[currentQuestionIndex];
            questionText.text = question.question;

            // ✅ Update question number
            if (questionNumberText != null)
                questionNumberText.text = "0" + (currentQuestionIndex + 1).ToString();

            Debug.Log("Displaying question: " + question.question);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionTexts[i].text = question.options[i];
                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => SubmitAnswer(index));
            }
        }
        else
        {
            Debug.Log("No more questions. Ending quiz.");

            // Optional: Clear question number at end
            if (questionNumberText != null)
                questionNumberText.text = "";

            EndQuiz();
        }
    }


    public void SubmitAnswer(int selectedIndex)
    {
        if (currentQuestionIndex >= currentQuestions.Count)
        {
            Debug.LogWarning("SubmitAnswer() called after quiz is already finished.");
            return; // Prevent crash
        }

        Debug.Log("Answer submitted. Selected index: " + selectedIndex);

        if (selectedIndex == currentQuestions[currentQuestionIndex].correctAnswerIndex)
        {
            Debug.Log("Correct answer!");
            score++;
        }
        else
        {
            Debug.Log("Incorrect answer.");
        }

        currentQuestionIndex++;
        DisplayQuestion();
    }


    void EndQuiz()
    {
        questionText.text = "Quiz Completed!";
        resultText.text = score + "/" + currentQuestions.Count;
        Debug.Log("Quiz completed. Score: " + score);

        UpdateStars(score);

        if (onQuizCompleteObject != null)
            onQuizCompleteObject.SetActive(true);

        // Disable all buttons
        foreach (Button btn in optionButtons)
        {
            btn.interactable = false;
            btn.gameObject.GetComponent<CanvasGroup>().alpha = 0.5f; // Optional: dim the buttons
        }

        // (Optional) Clear question number
        if (questionNumberText != null)
            questionNumberText.text = "";
    }


    public void DoneQuiz()
    {
        // ✅ Get the category and show corresponding completion panel
        string category = PlayerPrefs.GetString("SelectedCategory", "");

        arrayCompletePanel?.SetActive(false);
        linkedListCompletePanel?.SetActive(false);
        stackCompletePanel?.SetActive(false);
        queueCompletePanel?.SetActive(false);

        switch (category)
        {
            case "Array":
                arrayCompletePanel?.SetActive(true);
                Debug.Log("Enabled Array Completion Panel");
                break;

            case "Linked List":
                linkedListCompletePanel?.SetActive(true);
                Debug.Log("Enabled Linked List Completion Panel");
                break;

            case "Stack":
                stackCompletePanel?.SetActive(true);
                Debug.Log("Enabled Stack Completion Panel");
                break;

            case "Queue":
                queueCompletePanel?.SetActive(true);
                Debug.Log("Enabled Queue Completion Panel");
                break;

            default:
                Debug.LogWarning("Unknown category, no completion panel enabled.");
                break;
        }
        resultPanel.SetActive(false);
        optionPanel.SetActive(true);

        OrientationController.Instance.SetLandscape();
    }


    void ResetStars()
    {
        Debug.Log("Resetting star icons to default.");

        foreach (Image star in starImages)
            star.sprite = normalStarSprite;
    }

    void UpdateStars(int correctAnswers)
    {
        Debug.Log("Updating stars. Correct answers: " + correctAnswers);

        for (int i = 0; i < starImages.Length; i++)
        {
            starImages[i].sprite = i < correctAnswers ? glowingStarSprite : normalStarSprite;
        }
    }

    public void LoadARScene()
    {
        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
            Debug.Log("Quiz panel hidden before loading AR scene.");
        }

        string selectedCategory = PlayerPrefs.GetString("SelectedCategory", "");
        Debug.Log("Loading AR scene for category: " + selectedCategory);

        switch (selectedCategory)
        {
            case "Array":
                SceneManager.LoadScene("Array");
                break;
            case "Linked List":
                SceneManager.LoadScene("LinkedList");
                break;
            case "Stack":
                SceneManager.LoadScene("Stack");
                break;
            case "Queue":
                SceneManager.LoadScene("Queue");
                break;
            default:
                Debug.LogWarning("Invalid or empty category selected for AR.");
                break;
        }
    }
}
