using UnityEngine;

// Saved progress per topic ("Array", "Linked List", "Stack", "Queue"), kept between app launches
public static class Progress
{
    private static string BestKey(string topic) => "BestScore_" + topic;
    private static string TotalKey(string topic) => "QuestionCount_" + topic;
    private static string PracticeKey(string topic) => "PracticeDone_" + topic;

    public static bool HasQuizScore(string topic) => PlayerPrefs.HasKey(BestKey(topic));
    public static int BestScore(string topic) => PlayerPrefs.GetInt(BestKey(topic), 0);
    public static int QuestionCount(string topic) => PlayerPrefs.GetInt(TotalKey(topic), 0);
    public static bool PracticeDone(string topic) => PlayerPrefs.GetInt(PracticeKey(topic), 0) == 1;

    // Returns true when this is a new best score
    public static bool SaveQuizScore(string topic, int score, int total)
    {
        if (string.IsNullOrEmpty(topic) || (HasQuizScore(topic) && score <= BestScore(topic)))
            return false;

        PlayerPrefs.SetInt(BestKey(topic), score);
        PlayerPrefs.SetInt(TotalKey(topic), total);
        PlayerPrefs.Save();
        return true;
    }

    public static void MarkPracticeDone(string topic)
    {
        PlayerPrefs.SetInt(PracticeKey(topic), 1);
        PlayerPrefs.Save();
    }
}
