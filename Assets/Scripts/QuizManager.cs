using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Quiz data, scoring and navigation for the menu scene. The screens themselves are built by AppScreens.
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

    [Header("Panels")]
    public GameObject quizPanel;
    public GameObject resultPanel;
    public GameObject optionPanel;

    [Header("Practice Panels Per Category")]
    public GameObject arrayCompletePanel;
    public GameObject linkedListCompletePanel;
    public GameObject stackCompletePanel;
    public GameObject queueCompletePanel;

    [Header("Answer Feedback")]
    public float explanationSeconds = 6f; // Moves on by itself after this, or sooner with Next

    public static readonly string[] Topics = { "Array", "Linked List", "Stack", "Queue" };

    public string Topic => PlayerPrefs.GetString("SelectedCategory", "");
    public List<QuizQuestion> Questions { get; private set; } = new List<QuizQuestion>();
    public List<bool> Results { get; } = new List<bool>();
    public int Index { get; private set; }
    public bool Finished { get; private set; }
    public int Score => Results.FindAll(r => r).Count;

    private AppScreens screens;

    private void Awake()
    {
        // The old panel scripts are replaced by the new screens; switch them off before they start
        foreach (MonoBehaviour old in new MonoBehaviour[]
                 {
                     FindObjectOfType<DynamicUISpawner>(true), FindObjectOfType<InsertValueManager>(true),
                     FindObjectOfType<QueueQuiz>(true), FindObjectOfType<LinkedListQuiz>(true), FindObjectOfType<StackQuiz>(true)
                 })
        {
            if (old != null)
                old.enabled = false;
        }

        screens = gameObject.AddComponent<AppScreens>();
        screens.Init(this);
    }

    private void Start()
    {
        int returned = PlayerPrefs.GetInt("ReturnedFromAR", 0);
        PlayerPrefs.SetInt("ReturnedFromAR", 0);
        PlayerPrefs.Save();

        // Back from the AR scene's Go to Quiz button, or its exit button
        if (returned == 1 && !string.IsNullOrEmpty(Topic))
        {
            StartQuiz(Topic);
            screens.ShowQuiz();
        }
        else if (returned == 2)
        {
            screens.ShowMenu();
        }
    }

    public void SelectCategory(string categoryName)
    {
        PlayerPrefs.SetString("SelectedCategory", categoryName);
        PlayerPrefs.Save();
    }

    public void StartQuiz(string categoryName)
    {
        QuizCategory category = quizCategories.Find(c => c.categoryName == categoryName);
        if (category == null)
        {
            Debug.LogError("Category not found: " + categoryName);
            return;
        }
        Questions = ShuffledQuestions(category.questions);
        Results.Clear();
        Index = 0;
        Finished = false;
    }

    public void RetryQuiz()
    {
        StartQuiz(Topic);
        screens.ShowQuiz();
    }

    public QuizQuestion Current => Questions[Index];

    // Records the answer for the current question; returns whether it was right
    public bool Answer(int optionIndex)
    {
        bool right = optionIndex == Current.correctAnswerIndex;
        Results.Add(right);
        return right;
    }

    public void NextQuestion()
    {
        if (Index < Questions.Count - 1)
        {
            Index++;
            return;
        }
        Finished = true;
        Progress.SaveQuizScore(Topic, Score, Questions.Count);
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

    public GameObject PracticePanel(string topic)
    {
        switch (topic)
        {
            case "Array": return arrayCompletePanel;
            case "Linked List": return linkedListCompletePanel;
            case "Stack": return stackCompletePanel;
            case "Queue": return queueCompletePanel;
            default: return null;
        }
    }

    // The result screen's Next: the topic's practice puzzle, in landscape, over the menu
    public void OpenPractice()
    {
        foreach (string topic in Topics)
            PracticePanel(topic)?.SetActive(false);
        screens.ShowMenu();
        GameObject panel = PracticePanel(Topic);
        if (panel == null)
        {
            Debug.LogWarning("No practice panel for " + Topic);
            return;
        }
        panel.SetActive(true);
        OrientationController.Instance.SetLandscape();
    }

    public void LoadARScene()
    {
        switch (Topic)
        {
            case "Array": SceneManager.LoadScene("Array"); break;
            case "Linked List": SceneManager.LoadScene("LinkedList"); break;
            case "Stack": SceneManager.LoadScene("Stack"); break;
            case "Queue": SceneManager.LoadScene("queue"); break; // Must match the scene file name (queue.unity)
            default: Debug.LogWarning("Invalid or empty category selected for AR."); break;
        }
    }
}
