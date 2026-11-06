using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The menu scene's portrait screens (menu, lesson, quiz, result) and the practice puzzles,
// built to the v4 design inside the scene's existing panels so the app's flow stays the same.
public class AppScreens : MonoBehaviour
{
    private static readonly Vector2 Portrait = new Vector2(390, 844);

    private QuizManager quiz;
    private GameObject options, info, quizPanel, result;

    // Menu
    private readonly List<MenuCard> cards = new List<MenuCard>();
    private Button explore;
    private TextMeshProUGUI menuSubtitle;

    // Lesson
    private RectTransform lessonContent;
    private ScrollRect lessonScroll;

    // Quiz
    private TextMeshProUGUI quizTopic, progressBubble, questionText;
    private readonly List<Image> segments = new List<Image>();
    private readonly List<QuizOption> optionViews = new List<QuizOption>();
    private RectTransform explainBlock;
    private TextMeshProUGUI explainText;
    private RectTransform countdown;
    private Button doneButton;
    private Coroutine autoNext;
    private bool answered;

    // Result
    private TextMeshProUGUI resultTopic, resultScore, resultCaption;
    private readonly List<Image> stars = new List<Image>();
    private readonly List<ResultTile> tiles = new List<ResultTile>();

    private class MenuCard
    {
        public string topic;
        public Image outer, fill, iconTile, divider, practiceRing;
        public List<Image> cells = new List<Image>();
        public List<Image> dots = new List<Image>();
        public TextMeshProUGUI title, desc, quizLabel, best, practiceText;
        public RectTransform chevron, progress, practiceCheck;
    }

    private class QuizOption
    {
        public Image outer, fill, badge;
        public TextMeshProUGUI letter, text, note;
        public Button button;
    }

    private class ResultTile
    {
        public Image bg, dot;
        public RectTransform check, cross;
        public TextMeshProUGUI label;
    }

    public void Init(QuizManager manager)
    {
        quiz = manager;
        options = quiz.optionPanel;
        quizPanel = quiz.quizPanel;
        result = quiz.resultPanel;
        info = options.transform.parent.Find("Info").gameObject;

        BuildMenu(options.transform);
        BuildLesson(info.transform);
        BuildQuiz(quizPanel.transform);
        BuildResult(result.transform);

        foreach (string topic in QuizManager.Topics)
        {
            GameObject panel = quiz.PracticePanel(topic);
            if (panel == null)
                continue;
            switch (topic)
            {
                case "Array": panel.AddComponent<ArrayPuzzle>(); break;
                case "Linked List": panel.AddComponent<LinkedListPuzzle>(); break;
                case "Stack": panel.AddComponent<StackPuzzle>(); break;
                case "Queue": panel.AddComponent<QueuePuzzle>(); break;
            }
        }

        // The menu refreshes whenever it appears (e.g. from the intro's Next button)
        options.AddComponent<OnEnableHook>().action = RefreshMenu;
        RefreshMenu();
    }

    private void Show(GameObject panel)
    {
        foreach (GameObject p in new[] { options, info, quizPanel, result })
            p.SetActive(p == panel);
    }

    public void ShowMenu()
    {
        Show(options);
        RefreshMenu();
    }

    public void ShowLesson()
    {
        string topic = quiz.Topic;
        DK.ClearChildren(lessonContent);
        DK.Text(lessonContent, topic, 32, DK.Indigo900, true, TextAlignmentOptions.TopLeft, "Title");
        TextAsset lesson = LessonBuilder.Load(topic);
        if (lesson != null)
            LessonBuilder.Build(lessonContent, lesson.text);
        Show(info);
        lessonScroll.verticalNormalizedPosition = 1f;
    }

    public void ShowQuiz()
    {
        Show(quizPanel);
        RefreshQuiz();
    }

    public void ShowResult()
    {
        Show(result);
        RefreshResult();
    }

    // ---------------- Menu ----------------

    private void BuildMenu(Transform panel)
    {
        RectTransform root = DK.Screen(panel, Portrait, false, DK.AppBg);
        DK.Decorate(root);

        // Heading, subtitle and topic cards stack from the top, so cards with a progress row push the rest down.
        // They scroll in the space above the Explore button, so on short screens they never run under it.
        RectTransform viewport = UiKit.Create("Scroll", root);
        DK.Stretch(viewport, 0, 0, 0, 32 + 58 + 12);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = DK.Clear; // Catches drags between the cards

        RectTransform column = UiKit.Create("Column", viewport);
        DK.Row(column, 28, 28, 0, 0);
        DK.Column(column.gameObject, 8, new RectOffset(0, 0, 72, 12));
        column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = column;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        TextMeshProUGUI title = DK.Text(column, "Are you ready to learn Data Structures?", 28, DK.Indigo900, true);
        title.lineSpacing = -12;
        menuSubtitle = DK.Text(column, "", 15, DK.Indigo500);
        DK.Size(UiKit.Create("Gap", column), -1, 20);

        RectTransform list = UiKit.Create("Topics", column);
        DK.Column(list.gameObject, 12);

        string[] dirs = { "row", "row", "column", "row" };
        int[] counts = { 4, 3, 3, 3 };
        Vector2[] cellSizes = { new Vector2(8, 14), new Vector2(10, 10), new Vector2(24, 6), new Vector2(7, 18) };
        for (int i = 0; i < QuizManager.Topics.Length; i++)
        {
            string topic = QuizManager.Topics[i];
            MenuCard card = new MenuCard { topic = topic };
            card.outer = DK.Outlined(list, DK.White, DK.Lavender300, 16, 1, "Card-" + topic);
            card.fill = DK.Fill(card.outer);
            DK.Column(card.outer.gameObject, 12, new RectOffset(14, 16, 14, 14));
            DK.MakeButton(card.outer, () => SelectTopic(topic));

            // Icon tile, name and one-line description, chevron
            RectTransform head = UiKit.Create("Head", card.outer.transform);
            DK.Line(head.gameObject, 14);
            DK.Size(head, -1, 48);

            card.iconTile = DK.Box(head, DK.Lavender100, 12, "IconTile");
            DK.Size(card.iconTile, 48, 48);
            RectTransform cellRow = UiKit.Create("Cells", card.iconTile.transform);
            UiKit.Stretch(cellRow, Vector2.zero, Vector2.one);
            if (dirs[i] == "row") DK.Line(cellRow.gameObject, 3, TextAnchor.MiddleCenter);
            else DK.Column(cellRow.gameObject, 3, null, TextAnchor.MiddleCenter).childForceExpandWidth = false;
            for (int c = 0; c < counts[i]; c++)
            {
                Image cell = DK.Box(cellRow, DK.Lavender400, 2, "Cell");
                DK.Size(cell, cellSizes[i].x, cellSizes[i].y);
                card.cells.Add(cell);
            }

            RectTransform words = UiKit.Create("Words", head);
            DK.Column(words.gameObject, 2, null, TextAnchor.MiddleLeft);
            DK.Size(words, -1, -1, 1);
            card.title = DK.Text(words, topic, 18, DK.Indigo900, true);
            card.title.enableWordWrapping = false;
            card.desc = DK.Text(words, Description(topic), 13, DK.Lavender500);
            card.desc.enableWordWrapping = false;

            RectTransform chevronCell = UiKit.Create("Chevron", head);
            DK.Size(chevronCell, 20, 20);
            card.chevron = DK.Icon(chevronCell, "chevron-right", 20, DK.Lavender400);
            DK.Anchor(card.chevron, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));

            // Progress, once the topic is started: best quiz score as bars, and whether the puzzle is solved
            card.progress = UiKit.Create("Progress", card.outer.transform);
            HorizontalLayoutGroup row = DK.Line(card.progress.gameObject, 10);
            row.padding = new RectOffset(0, 0, 12, 0);
            card.divider = DK.Box(card.progress, DK.Lavender100, 0, "Divider");
            card.divider.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            RectTransform line = card.divider.rectTransform;
            line.anchorMin = new Vector2(0, 1);
            line.anchorMax = new Vector2(1, 1);
            line.pivot = new Vector2(0.5f, 1);
            line.offsetMin = new Vector2(0, -1);
            line.offsetMax = Vector2.zero;

            card.quizLabel = DK.Caps(card.progress, "QUIZ", 10, DK.Lavender500);
            RectTransform dots = UiKit.Create("Dots", card.progress);
            DK.Line(dots.gameObject, 3);
            for (int d = 0; d < Progress.QuestionCount(topic); d++)
            {
                Image dot = DK.Box(dots, DK.Hex(0xE4E2EE), 3, "Dot");
                DK.Size(dot, 14, 6);
                card.dots.Add(dot);
            }
            card.best = DK.Text(card.progress, "", 12, DK.Indigo900, true);
            card.best.enableWordWrapping = false;
            DK.Size(UiKit.Create("Space", card.progress), 0, -1, 1);

            RectTransform practice = UiKit.Create("Practice", card.progress);
            DK.Line(practice.gameObject, 6);
            card.practiceRing = DK.Outlined(practice, DK.White, DK.Dash, 8, 1.5f, "Ring");
            DK.Size(card.practiceRing, 16, 16);
            card.practiceCheck = DK.Icon(card.practiceRing.transform, "check", 11, DK.White);
            DK.Anchor(card.practiceCheck, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(11, 11));
            card.practiceText = DK.Text(practice, "Practice", 12, DK.Lavender500, true);
            card.practiceText.enableWordWrapping = false;

            cards.Add(card);
        }

        explore = DK.Pill(root, "Explore", DK.Indigo700, DK.White, 14, 17, ShowLesson, null, "arrow-right");
        DK.BottomRow(explore, 28, 28, 32, 58);
    }

    private static string Description(string topic)
    {
        switch (topic)
        {
            case "Array": return "Indexed, fixed-size";
            case "Linked List": return "Nodes joined by pointers";
            case "Stack": return "Last in, first out";
            case "Queue": return "First in, first out";
            default: return "";
        }
    }

    private static void Tint(RectTransform icon, Color color)
    {
        foreach (Graphic graphic in icon.GetComponentsInChildren<Graphic>(true))
            graphic.color = color;
    }

    private static Color Faint(Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

    public void SelectTopic(string topic)
    {
        quiz.SelectCategory(topic);
        RefreshMenu();
    }

    private void RefreshMenu()
    {
        string selected = quiz.Topic;
        bool anyStarted = false;
        foreach (string topic in QuizManager.Topics)
            anyStarted |= Progress.HasQuizScore(topic) || Progress.PracticeDone(topic);
        menuSubtitle.text = anyStarted
            ? "Choose a topic to start its lesson."
            : "Each topic has a lesson, an AR scene, a quiz and a puzzle.";

        foreach (MenuCard card in cards)
        {
            bool on = card.topic == selected;
            card.outer.color = on ? DK.Indigo700 : DK.Lavender300;
            card.fill.color = on ? DK.Indigo700 : DK.White;
            card.iconTile.color = on ? Faint(DK.Lavender50, 0.14f) : DK.Lavender100;
            foreach (Image cell in card.cells)
                cell.color = on ? DK.Lavender50 : DK.Lavender400;
            card.title.color = on ? DK.White : DK.Indigo900;
            card.desc.color = on ? DK.Lavender300 : DK.Lavender500;
            Tint(card.chevron, on ? DK.Lavender50 : DK.Lavender400);

            // Started topics show their best quiz score as bars and whether the puzzle is solved
            bool scored = Progress.HasQuizScore(card.topic);
            bool solved = Progress.PracticeDone(card.topic);
            card.progress.gameObject.SetActive(scored || solved);
            int best = scored ? Progress.BestScore(card.topic) : 0;
            card.divider.color = on ? Faint(DK.Lavender50, 0.16f) : DK.Lavender100;
            card.quizLabel.color = on ? DK.Lavender300 : DK.Lavender500;
            for (int d = 0; d < card.dots.Count; d++)
                card.dots[d].color = d < best ? (on ? DK.Lavender50 : DK.Indigo700)
                    : on ? Faint(DK.Lavender50, 0.22f) : DK.Hex(0xE4E2EE);
            card.best.text = scored ? $"{best}/{card.dots.Count}" : "–";
            card.best.color = on ? DK.White : DK.Indigo900;

            card.practiceRing.color = solved ? DK.Green : on ? Faint(DK.Lavender50, 0.5f) : DK.Dash;
            DK.Fill(card.practiceRing).color = solved ? DK.Green : on ? DK.Indigo700 : DK.White;
            card.practiceCheck.gameObject.SetActive(solved);
            card.practiceText.color = solved ? (on ? DK.White : DK.Green) : on ? DK.Lavender300 : DK.Lavender500;
        }

        bool hasTopic = !string.IsNullOrEmpty(selected);
        explore.interactable = hasTopic;
        SetAlpha(explore.gameObject, hasTopic ? 1f : 0.45f);
        DK.PillLabel(explore).text = hasTopic ? "Explore " + selected : "Explore";
    }

    private static void SetAlpha(GameObject go, float alpha)
    {
        CanvasGroup group = DK.Get<CanvasGroup>(go);
        group.alpha = alpha;
    }

    // ---------------- Lesson ----------------

    private void BuildLesson(Transform panel)
    {
        RectTransform root = DK.Screen(panel, Portrait, false, DK.AppBg);

        Header(root, "LESSON", ShowMenu, true);

        Image bar = DK.Box(root, DK.AppBg, 0, "BottomBar");
        DK.BottomRow(bar, 0, 0, 0, 102);
        Image line = DK.Box(bar.transform, DK.Lavender100, 0, "Line");
        DK.Row(line, 0, 0, 0, 1);
        Button start = DK.Pill(bar.transform, "Start AR", DK.Indigo700, DK.White, 14, 17, quiz.LoadARScene, "scan");
        DK.BottomRow(start, 20, 20, 30, 58);

        // Scrolling lesson between the header and the bottom bar
        RectTransform view = UiKit.Create("Scroll", root);
        DK.Stretch(view, 0, 108, 0, 102);
        lessonScroll = view.gameObject.AddComponent<ScrollRect>();
        lessonScroll.horizontal = false;
        lessonScroll.movementType = ScrollRect.MovementType.Elastic;
        lessonScroll.scrollSensitivity = 30;
        RectTransform viewport = UiKit.Create("Viewport", view);
        UiKit.Stretch(viewport, Vector2.zero, Vector2.one);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image hit = viewport.gameObject.AddComponent<Image>();
        hit.color = DK.Clear;
        lessonContent = UiKit.Create("Content", viewport);
        lessonContent.anchorMin = new Vector2(0, 1);
        lessonContent.anchorMax = new Vector2(1, 1);
        lessonContent.pivot = new Vector2(0.5f, 1);
        lessonContent.sizeDelta = Vector2.zero;
        DK.Column(lessonContent.gameObject, 16, new RectOffset(28, 28, 4, 28));
        lessonContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        lessonScroll.viewport = viewport;
        lessonScroll.content = lessonContent;
    }

    // Back arrow and a small centred caption, as on the lesson, quiz and result screens
    private TextMeshProUGUI Header(RectTransform root, string caption, System.Action back, bool centred)
    {
        Image backHit = DK.Box(root, DK.Clear, 0, "Back");
        DK.At(backHit, centred ? 20 : 16, 56, 44, 44);
        DK.MakeButton(backHit, back);
        RectTransform arrow = DK.Icon(backHit.transform, "arrow-left", 24, DK.ArrowInk);
        DK.Anchor(arrow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));

        TextMeshProUGUI label = DK.Caps(root, caption, 13, DK.Lavender500, centred ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
        if (centred)
            DK.Row(label, 76, 76, 56, 44);
        else
            DK.Row(label, 72, 24, 56, 44);
        label.alignment = centred ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;
        return label;
    }

    // ---------------- Quiz ----------------

    private void BuildQuiz(Transform panel)
    {
        RectTransform root = DK.Screen(panel, Portrait, false, DK.AppBg);
        DK.Decorate(root, false);
        quizTopic = Header(root, "QUIZ", ShowMenu, false);

        RectTransform segRow = UiKit.Create("Progress", root);
        DK.Row(segRow, 24, 24, 110, 5);
        DK.Line(segRow.gameObject, 6).childForceExpandWidth = true;
        for (int i = 0; i < 5; i++)
        {
            Image seg = DK.Box(segRow, DK.Lavender300, 2.5f, "Segment");
            DK.Size(seg, -1, 5, 1);
            segments.Add(seg);
        }

        // Question card, options and (when answered) the explanation, in one column
        RectTransform column = UiKit.Create("Column", root);
        DK.Row(column, 24, 24, 153, 0);
        DK.Column(column.gameObject, 20);
        column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image card = DK.Box(column, DK.Indigo700, 24, "QuestionCard");
        DK.Column(card.gameObject, 0, new RectOffset(22, 22, 42, 26));
        questionText = DK.Text(card.transform, "", 18, DK.White, false, TextAlignmentOptions.Top, "Question");

        Image bubble = DK.Outlined(card.transform, DK.Indigo700, DK.AppBg, 27, 5, "Bubble");
        bubble.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        DK.Anchor(bubble, new Vector2(0.5f, 1), new Vector2(0, 27), new Vector2(86, 54));
        progressBubble = DK.Text(bubble.transform, "1/5", 18, DK.White, true, TextAlignmentOptions.Center);
        UiKit.Stretch(progressBubble.rectTransform, Vector2.zero, Vector2.one);

        RectTransform optionColumn = UiKit.Create("Options", column);
        DK.Column(optionColumn.gameObject, 12);
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            QuizOption view = new QuizOption();
            view.outer = DK.Outlined(optionColumn, DK.White, DK.Lavender300, 20, 1.5f, "Option");
            view.fill = DK.Fill(view.outer);
            HorizontalLayoutGroup row = DK.Line(view.outer.gameObject, 14);
            row.padding = new RectOffset(10, 16, 10, 10);
            DK.Size(view.outer, -1, -1).minHeight = 64;

            view.badge = DK.Box(view.outer.transform, DK.Lavender100, 14, "Badge");
            DK.Size(view.badge, 46, 44);
            view.letter = DK.Text(view.badge.transform, "ABCD"[i].ToString(), 18, DK.Indigo900, true, TextAlignmentOptions.Center);
            UiKit.Stretch(view.letter.rectTransform, Vector2.zero, Vector2.one);

            view.text = DK.Text(view.outer.transform, "", 16, DK.Indigo900, false, TextAlignmentOptions.MidlineLeft);
            DK.Size(view.text, -1, -1, 1);
            view.note = DK.Text(view.outer.transform, "", 13, DK.Indigo900, true, TextAlignmentOptions.MidlineRight);
            view.note.enableWordWrapping = false;
            view.button = DK.MakeButton(view.outer, () => Pick(index));
            optionViews.Add(view);
        }

        // Explanation and Next, pinned above the bottom edge
        explainBlock = UiKit.Create("Explain", root);
        DK.BottomRow(explainBlock, 24, 24, 28, 0);
        explainBlock.pivot = new Vector2(0.5f, 0);
        DK.Column(explainBlock.gameObject, 14);
        explainBlock.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        explainText = DK.Text(explainBlock, "", 14, DK.Indigo900);
        Button next = DK.Pill(explainBlock, "Next question", DK.Indigo700, DK.White, 14, 17, NextQuestion, null, "arrow-right");
        DK.Size(next, -1, 58);
        Image track = DK.Box(next.transform, DK.Clear, 0, "Countdown");
        countdown = track.rectTransform;
        Image barFill = DK.Box(track.transform, DK.Lavender400, 0, "Bar");
        UiKit.Stretch(barFill.rectTransform, Vector2.zero, Vector2.one);
        countdown.anchorMin = new Vector2(0, 0);
        countdown.anchorMax = new Vector2(1, 0);
        countdown.pivot = new Vector2(0, 0);
        countdown.offsetMin = new Vector2(14, 0);
        countdown.offsetMax = new Vector2(-14, 3);

        doneButton = DK.Pill(root, "Done", DK.Indigo700, DK.White, 14, 17, ShowResult);
        DK.BottomRow(doneButton, 24, 24, 28, 58);
    }

    private void RefreshQuiz()
    {
        if (autoNext != null)
            StopCoroutine(autoNext);
        answered = false;
        quizTopic.text = "QUIZ · " + quiz.Topic.ToUpperInvariant();
        int total = quiz.Questions.Count;

        for (int i = 0; i < segments.Count; i++)
        {
            Color c = DK.Lavender300;
            if (i < quiz.Results.Count) c = quiz.Results[i] ? DK.Green : DK.Red;
            else if (i == quiz.Index && !quiz.Finished) c = DK.Indigo700;
            segments[i].color = c;
            segments[i].gameObject.SetActive(i < total);
        }

        explainBlock.gameObject.SetActive(false);
        doneButton.gameObject.SetActive(quiz.Finished);
        progressBubble.text = $"{(quiz.Finished ? total : quiz.Index + 1)}/{total}";

        if (quiz.Finished)
        {
            questionText.text = "<size=26><b>Quiz Completed!</b></size>";
            foreach (QuizOption view in optionViews)
                Style(view, "dim");
            return;
        }

        QuizManager.QuizQuestion q = quiz.Current;
        questionText.text = q.question;
        for (int i = 0; i < optionViews.Count; i++)
        {
            optionViews[i].text.text = q.options[i];
            Style(optionViews[i], "idle");
        }
    }

    private void Style(QuizOption view, string state)
    {
        Color bg = DK.White, ring = DK.Lavender300, badgeBg = DK.Lavender100, badgeFg = DK.Indigo900, fg = DK.Indigo900;
        float ringWidth = 1.5f;
        string note = "";
        switch (state)
        {
            case "right": bg = ring = DK.Indigo700; badgeBg = DK.White; fg = DK.White; note = "Correct"; break;
            case "wrong": ring = DK.WrongRing; ringWidth = 2; badgeBg = DK.WrongTint; badgeFg = fg = DK.WrongRing; note = "Your answer"; break;
            case "dim": ring = DK.Lavender100; ringWidth = 1; badgeBg = DK.Lavender50; badgeFg = DK.Lavender400; fg = DK.Lavender500; break;
        }
        view.outer.color = ring;
        view.fill.color = bg;
        DK.Stretch(view.fill.rectTransform, ringWidth, ringWidth, ringWidth, ringWidth);
        view.badge.color = badgeBg;
        view.letter.color = badgeFg;
        view.text.color = view.note.color = fg;
        view.note.text = note;
        view.button.interactable = state == "idle";
        view.button.transition = Selectable.Transition.None;
    }

    public void Pick(int index)
    {
        if (answered || quiz.Finished)
            return;
        answered = true;
        bool right = quiz.Answer(index);
        QuizManager.QuizQuestion q = quiz.Current;

        for (int i = 0; i < optionViews.Count; i++)
            Style(optionViews[i], i == q.correctAnswerIndex ? "right" : i == index ? "wrong" : "dim");
        segments[quiz.Results.Count - 1].color = right ? DK.Green : DK.Red;

        string title = right ? "Correct!" : $"Not quite. The answer is {q.options[q.correctAnswerIndex]}.";
        explainText.text = $"<b>{title}</b> {q.explanation}";
        explainBlock.gameObject.SetActive(true);
        autoNext = StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        float duration = quiz.explanationSeconds;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            countdown.anchorMax = new Vector2(1f - t / duration, 0);
            yield return null;
        }
        NextQuestion();
    }

    public void NextQuestion()
    {
        if (!answered)
            return;
        quiz.NextQuestion();
        RefreshQuiz();
    }

    // ---------------- Result ----------------

    private void BuildResult(Transform panel)
    {
        RectTransform root = DK.Screen(panel, Portrait, false, DK.AppBg);
        DK.Decorate(root, false);
        Image blob = DK.Box(root, DK.Hex(0xF1F1F4), 120, "DecoBottom");
        DK.Anchor(blob, new Vector2(0, 0), new Vector2(60, -80), new Vector2(240, 240));
        resultTopic = Header(root, "QUIZ", ShowQuizBack, false);

        // Stars and the results card, centred between the header and the buttons
        RectTransform middle = UiKit.Create("Middle", root);
        DK.Stretch(middle, 0, 100, 0, 90);
        RectTransform block = UiKit.Create("Block", middle);
        DK.Anchor(block, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(342, 380));

        float[] lift = { 26, 8, 0, 8, 26 };
        for (int i = 0; i < 5; i++)
        {
            Image star = DK.Box(block, DK.Ochre, 0, "Star");
            star.sprite = DK.Star;
            DK.At(star, 51 + i * 50, lift[i], 40, 40);
            stars.Add(star);
        }

        Image card = DK.Box(block, DK.Indigo700, 28, "ResultCard");
        DK.At(card, 0, 102, 342, 274);
        Image bubble = DK.Outlined(card.transform, DK.Indigo700, DK.AppBg, 25, 5, "Bubble");
        DK.Anchor(bubble, new Vector2(0.5f, 1), new Vector2(0, 25), new Vector2(140, 50));
        TextMeshProUGUI caps = DK.Caps(bubble.transform, "RESULTS", 14, DK.White, TextAlignmentOptions.Center);
        UiKit.Stretch(caps.rectTransform, Vector2.zero, Vector2.one);

        resultScore = DK.Text(card.transform, "", 88, DK.White, true, TextAlignmentOptions.Bottom, "Score");
        DK.Row(resultScore, 0, 0, 34, 104);
        resultScore.enableWordWrapping = false;
        resultCaption = DK.Text(card.transform, "", 16, DK.Lavender50, true, TextAlignmentOptions.Top, "Caption");
        DK.Row(resultCaption, 0, 0, 144, 24);
        resultCaption.alignment = TextAlignmentOptions.Center;

        RectTransform tileRow = UiKit.Create("Tiles", card.transform);
        DK.Row(tileRow, 24, 24, 189, 61);
        DK.Line(tileRow.gameObject, 8).childForceExpandWidth = true;
        for (int i = 0; i < 5; i++)
        {
            ResultTile tile = new ResultTile();
            tile.bg = DK.Box(tileRow, DK.Lavender50, 12, "Tile");
            DK.Size(tile.bg, -1, 61, 1);
            tile.dot = DK.Box(tile.bg.transform, DK.Green, 12, "Dot");
            DK.Anchor(tile.dot, new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(24, 24));
            tile.check = DK.Icon(tile.dot.transform, "check", 14, DK.White);
            tile.cross = DK.Icon(tile.dot.transform, "cross", 14, DK.White);
            DK.Anchor(tile.check, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
            DK.Anchor(tile.cross, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14, 14));
            tile.label = DK.Caps(tile.bg.transform, "Q" + (i + 1), 11, DK.Indigo900, TextAlignmentOptions.Center);
            DK.Anchor(tile.label, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(40, 14));
            tiles.Add(tile);
        }

        Button retry = DK.Pill(root, "Retry", DK.White, DK.Indigo900, 14, 17, quiz.RetryQuiz, "reset", null, DK.Indigo700);
        Button next = DK.Pill(root, "Next", DK.Indigo700, DK.White, 14, 17, quiz.OpenPractice, null, "arrow-right");
        RectTransform r = (RectTransform)retry.transform, n = (RectTransform)next.transform;
        r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0.5f, 0); r.pivot = new Vector2(0.5f, 0);
        r.offsetMin = new Vector2(24, 32); r.offsetMax = new Vector2(-6, 90);
        n.anchorMin = new Vector2(0.5f, 0); n.anchorMax = new Vector2(1, 0); n.pivot = new Vector2(0.5f, 0);
        n.offsetMin = new Vector2(6, 32); n.offsetMax = new Vector2(-24, 90);
    }

    private void ShowQuizBack()
    {
        Show(quizPanel);
        RefreshQuiz();
    }

    private void RefreshResult()
    {
        resultTopic.text = "QUIZ · " + quiz.Topic.ToUpperInvariant();
        int score = quiz.Score, total = quiz.Questions.Count;
        resultScore.text = $"{score}<size=36><color={DK.Html(DK.Lavender300)}>/{total}</color></size>";
        resultCaption.text = $"{score} of {total} correct";
        for (int i = 0; i < 5; i++)
        {
            bool ok = i < quiz.Results.Count && quiz.Results[i];
            stars[i].sprite = ok ? DK.Star : DK.StarOutline;
            stars[i].color = ok ? DK.Ochre : DK.Lavender400;

            ResultTile tile = tiles[i];
            tile.bg.color = ok ? DK.Lavender50 : new Color(0.97f, 0.98f, 1f, 0.08f);
            tile.dot.color = ok ? DK.Green : new Color(0.97f, 0.98f, 1f, 0.16f);
            tile.check.gameObject.SetActive(ok);
            tile.cross.gameObject.SetActive(!ok);
            tile.label.color = ok ? DK.Indigo900 : DK.Lavender200;
        }
    }
}
