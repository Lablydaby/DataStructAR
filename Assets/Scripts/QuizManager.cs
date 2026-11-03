using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
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
        [TextArea] public string explanation; // Shown after answering, so students learn why
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
    private bool isRevealingAnswer = false;

    [Header("Answer Feedback")]
    public float answerRevealSeconds = 1f;
    public float explanationSeconds = 6f; // Moves on by itself after this, or sooner on a tap

    private GameObject explanationPanel;
    private TextMeshProUGUI explanationText;
    private bool continueTapped;


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
        HighlightCategory("");
        SetUpRetryButton();
        BuildExplanationPanel();
        AddProgressLabel(arrayButton, "Array");
        AddProgressLabel(linkedListButton, "Linked List");
        AddProgressLabel(stackButton, "Stack");
        AddProgressLabel(queueButton, "Queue");

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
        HighlightCategory(categoryName);

        // Do not show quiz panel or start quiz here.
    }

    // Tints the chosen topic so students can see what Explore will open
    private void HighlightCategory(string categoryName)
    {
        SetHighlight(arrayButton, categoryName == "Array");
        SetHighlight(linkedListButton, categoryName == "Linked List");
        SetHighlight(stackButton, categoryName == "Stack");
        SetHighlight(queueButton, categoryName == "Queue");
    }

    private static void SetHighlight(Button button, bool selected)
    {
        if (button != null && button.image != null)
            button.image.color = selected ? UiKit.Selected : Color.white;
    }

    // The result screen's retry icon retakes the quiz instead of returning to the topic list
    private void SetUpRetryButton()
    {
        Transform reload = resultPanel != null ? resultPanel.transform.Find("BG/Reload") : null;
        if (reload == null)
            return;

        Button retry = reload.GetComponent<Button>();
        for (int i = 0; i < retry.onClick.GetPersistentEventCount(); i++)
            retry.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        retry.onClick.AddListener(RetryQuiz);
    }

    private static void AddProgressLabel(Button button, string topic)
    {
        if (button != null)
            button.gameObject.AddComponent<TopicProgressLabel>().topic = topic;
    }

    // Card between the question and the options that explains the right answer.
    // Tapping it (or anywhere over it) moves on to the next question.
    private void BuildExplanationPanel()
    {
        Transform quizBG = optionButtons[0].transform.parent;

        Image panel = UiKit.Panel(quizBG, UiKit.CardTint);
        panel.name = "Explanation";
        panel.raycastTarget = true;
        UiKit.Stretch(panel.rectTransform, new Vector2(0.06f, 0.495f), new Vector2(0.95f, 0.605f));
        panel.gameObject.AddComponent<Button>().onClick.AddListener(() => continueTapped = true);

        explanationText = UiKit.Text(panel.transform, "", "Poppins-Regular SDF", 34, UiKit.Body, TextAlignmentOptions.Left);
        explanationText.enableAutoSizing = true;
        explanationText.fontSizeMin = 22;
        explanationText.fontSizeMax = 34;
        UiKit.Stretch(explanationText.rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));

        explanationPanel = panel.gameObject;
        explanationPanel.SetActive(false);
    }

    public void RetryQuiz()
    {
        resultPanel.SetActive(false);
        quizPanel.SetActive(true);
        StartQuiz(PlayerPrefs.GetString("SelectedCategory", ""));
    }

    public void StartQuiz(string categoryName)
    {
        Debug.Log("Starting quiz for: " + categoryName);

        QuizCategory selectedCategory = quizCategories.Find(cat => cat.categoryName == categoryName);

        if (selectedCategory != null)
        {
            currentQuestions = ShuffledQuestions(selectedCategory.questions);
            currentQuestionIndex = 0;
            score = 0;

            // Drop a reveal left running by leaving mid-question
            StopAllCoroutines();
            isRevealingAnswer = false;
            if (explanationPanel != null)
                explanationPanel.SetActive(false);

            if (onQuizCompleteObject != null)
                onQuizCompleteObject.SetActive(false);

            // Undo the dimming from a previous attempt
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].interactable = true;
                ResetOptionStyle(i);
            }

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

    // A fresh order of questions and of each question's options for every attempt, so a retry
    // can't be passed by remembering letters. Copies, so the scene's questions stay as authored.
    private static List<QuizQuestion> ShuffledQuestions(List<QuizQuestion> source)
    {
        List<QuizQuestion> questions = new List<QuizQuestion>();
        foreach (QuizQuestion original in source)
        {
            int[] order = new int[original.options.Length];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            Shuffle(order);

            QuizQuestion copy = new QuizQuestion
            {
                question = original.question.Trim(),
                options = new string[order.Length],
                explanation = original.explanation,
            };
            for (int i = 0; i < order.Length; i++)
            {
                copy.options[i] = original.options[order[i]].Trim();
                if (order[i] == original.correctAnswerIndex)
                    copy.correctAnswerIndex = i;
            }
            questions.Add(copy);
        }

        QuizQuestion[] shuffled = questions.ToArray();
        Shuffle(shuffled);
        return new List<QuizQuestion>(shuffled);
    }

    private static void Shuffle<T>(T[] items)
    {
        for (int i = items.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    void DisplayQuestion()
    {
        if (currentQuestionIndex < currentQuestions.Count)
        {
            QuizQuestion question = currentQuestions[currentQuestionIndex];
            questionText.text = question.question;

            // ✅ Update question number, with the total so students know how far along they are
            if (questionNumberText != null)
                questionNumberText.text = $"{currentQuestionIndex + 1}/{currentQuestions.Count}";

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
        if (isRevealingAnswer)
        {
            continueTapped = true; // A tap on an option while the explanation shows moves on
            return;
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

        StartCoroutine(RevealAnswerThenContinue(selectedIndex));
    }

    // Marks the right answer green and a wrong pick red for a moment before the next question
    private IEnumerator RevealAnswerThenContinue(int selectedIndex)
    {
        isRevealingAnswer = true;
        int correctIndex = currentQuestions[currentQuestionIndex].correctAnswerIndex;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            Image letterBadge = GetLetterBadge(i);
            if (i == correctIndex && letterBadge != null)
                letterBadge.color = UiKit.Correct;
            else if (i == selectedIndex && letterBadge != null)
                letterBadge.color = UiKit.Wrong;
            else
                SetOptionAlpha(i, 0.45f);
        }

        QuizQuestion question = currentQuestions[currentQuestionIndex];
        if (!string.IsNullOrWhiteSpace(question.explanation) && explanationPanel != null)
        {
            bool correct = selectedIndex == correctIndex;
            string verdict = correct
                ? $"<b><color=#{ColorUtility.ToHtmlStringRGB(UiKit.Correct)}>Correct!</color></b>"
                : $"<b><color=#{ColorUtility.ToHtmlStringRGB(UiKit.Wrong)}>Not quite.</color></b> The answer is <b>{question.options[correctIndex]}</b>.";
            explanationText.text = $"{verdict} {question.explanation}\n<size=80%><color=#{ColorUtility.ToHtmlStringRGB(UiKit.Brand)}>Tap to continue</color></size>";
            explanationPanel.SetActive(true);

            // Let go of the tap that picked the answer before listening for the next one
            yield return new WaitForSeconds(0.3f);
            continueTapped = false;
            float shownFor = 0.3f;
            while (!continueTapped && shownFor < explanationSeconds)
            {
                shownFor += Time.deltaTime;
                yield return null;
            }
            explanationPanel.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(answerRevealSeconds);
        }

        for (int i = 0; i < optionButtons.Length; i++)
            ResetOptionStyle(i);

        isRevealingAnswer = false;
        currentQuestionIndex++;
        DisplayQuestion();
    }

    // The white rounded square behind each option's letter
    private Image GetLetterBadge(int optionIndex)
    {
        Transform badge = optionButtons[optionIndex].transform.Find("ABG");
        return badge != null ? badge.GetComponent<Image>() : null;
    }

    private void SetOptionAlpha(int optionIndex, float alpha)
    {
        CanvasGroup group = optionButtons[optionIndex].GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = alpha;
    }

    private void ResetOptionStyle(int optionIndex)
    {
        Image letterBadge = GetLetterBadge(optionIndex);
        if (letterBadge != null)
            letterBadge.color = Color.white;
        SetOptionAlpha(optionIndex, 1f);
    }


    void EndQuiz()
    {
        questionText.text = "Quiz Completed!";
        resultText.text = score + "/" + currentQuestions.Count;
        Debug.Log("Quiz completed. Score: " + score);

        UpdateStars(score);
        Progress.SaveQuizScore(PlayerPrefs.GetString("SelectedCategory", ""), score, currentQuestions.Count);

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
                SceneManager.LoadScene("queue"); // Must match the scene file name (queue.unity)
                break;
            default:
                Debug.LogWarning("Invalid or empty category selected for AR.");
                break;
        }
    }
}
