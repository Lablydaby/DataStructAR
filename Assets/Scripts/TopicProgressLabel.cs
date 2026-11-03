using TMPro;
using UnityEngine;

// Shows the saved best quiz score and practice status on a topic button in the menu
public class TopicProgressLabel : MonoBehaviour
{
    public string topic;

    private TextMeshProUGUI label;

    private void OnEnable()
    {
        if (label == null)
        {
            label = UiKit.Text(transform, "", "Poppins-Regular SDF", 34, UiKit.Brand, TextAlignmentOptions.Right);
            label.name = "Progress";
            label.lineSpacing = -10;
            UiKit.Stretch(label.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(0.93f, 0.88f));
        }
        Refresh();
    }

    public void Refresh()
    {
        string quiz = Progress.HasQuizScore(topic)
            ? $"Best <b>{Progress.BestScore(topic)}/{Progress.QuestionCount(topic)}</b>"
            : "Not started";
        string practice = Progress.PracticeDone(topic) ? "\n<size=80%>Practice done</size>" : "";
        label.text = quiz + practice;
    }
}
