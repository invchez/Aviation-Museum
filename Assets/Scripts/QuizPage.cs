using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Main menu page: a badge for every aircraft in the collection, each earned by passing a
// short quiz about that aircraft. Questions are generated from the exhibit plaques
// (PlaneSO) and ExhibitFacts, with the other aircraft's answers as the wrong choices,
// so every attempt draws a different mix of questions and options.
public class QuizPage : MonoBehaviour
{
    public TMP_FontAsset font;
    public Sprite buttonSprite;
    public Sprite panelSprite;
    public List<PlaneSO> planes = new();

    // Clear of the iPhone Dynamic Island in landscape.
    const float SideInset = 64f;
    const float HeaderHeight = 74f;
    const float BadgeSize = 84f;
    const float BadgeInset = 12f;

    class Question
    {
        public string Prompt;
        public string Quote;
        public List<string> Options;
        public int Answer;
    }

    UiKit ui;
    RectTransform content;

    PlaneSO currentPlane;
    List<Question> questions;
    int questionIndex;
    int score;

    void Awake()
    {
        ui = new UiKit(font, buttonSprite, panelSprite);
    }

    // Wired to the main menu's Quiz & Badges button.
    public void Open()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        ShowBadges();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    RectTransform ResetContent()
    {
        if (content != null)
        {
            if (Application.isPlaying) Destroy(content.gameObject);
            else DestroyImmediate(content.gameObject);
        }

        content = ui.Rect("Content", transform);
        UiKit.Stretch(content);
        return content;
    }

    // ----------------------------------------------------------------- badges

    void ShowBadges()
    {
        RectTransform root = ResetContent();
        int earned = planes.Count(p => ExhibitProgress.HasBadge(p.Name));
        bool master = planes.Count > 0 && earned == planes.Count;

        Header(root, "Exhibit Badges",
            master ? "Hangar Master — every badge collected!" : $"Score {ExhibitProgress.PassMark} of {ExhibitProgress.QuestionsPerQuiz} on an aircraft's quiz to earn its badge.",
            $"{earned} of {planes.Count} badges", "Close", Close);

        RectTransform grid = ui.Rect("Badge Grid", root);
        grid.anchorMin = Vector2.zero;
        grid.anchorMax = Vector2.one;
        grid.offsetMin = new Vector2(SideInset - 6, 12);
        grid.offsetMax = new Vector2(-SideInset + 6, -HeaderHeight - 4);

        const int columns = 3;
        int rows = Mathf.CeilToInt(planes.Count / (float)columns);
        for (int i = 0; i < planes.Count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            RectTransform tile = BadgeTile(grid, planes[i]);
            UiKit.Region(tile,
                new Vector2(column / (float)columns, 1 - (row + 1) / (float)rows),
                new Vector2((column + 1) / (float)columns, 1 - row / (float)rows), 6);
        }
    }

    RectTransform BadgeTile(Transform parent, PlaneSO plane)
    {
        ExhibitFacts facts = ExhibitFacts.For(plane.Name);
        bool hasBadge = ExhibitProgress.HasBadge(plane.Name);
        bool visited = ExhibitProgress.HasVisited(plane.Name);
        int best = ExhibitProgress.BestScore(plane.Name);

        Button button = ui.Button(parent, $"Badge ({plane.Name})", "", 16, () => StartQuiz(plane), out TMP_Text unused);
        DestroyImmediate(unused.gameObject);
        button.targetGraphic.color = UiKit.Card;
        RectTransform tile = (RectTransform)button.transform;

        // Sized for the shortest tile, on a 6.9" iPhone (canvas ~470 units high).
        RectTransform badge = ui.Rect("Badge", tile);
        UiKit.Anchor(badge, new Vector2(0, 0.5f), new Vector2(BadgeInset, 0), new Vector2(BadgeSize, BadgeSize));

        Image disc = ui.Disc(badge, "Disc", hasBadge ? UiKit.Gold : new Color(0.22f, 0.26f, 0.34f, 1));
        UiKit.Stretch(disc.rectTransform, 5, 5);
        Image ring = ui.Ring(badge, "Ring", hasBadge ? UiKit.Gold : visited ? UiKit.Cyan : new Color(1, 1, 1, 0.25f));
        UiKit.Stretch(ring.rectTransform);
        TMP_Text code = ui.Text(badge, "Code", 18, TextAlignmentOptions.Center, facts?.ShortCode ?? "?");
        code.fontStyle = FontStyles.Bold;
        code.enableAutoSizing = true;
        code.fontSizeMin = 10;
        code.fontSizeMax = 20;
        code.textWrappingMode = TextWrappingModes.NoWrap;
        code.color = hasBadge ? new Color(0.1f, 0.12f, 0.2f) : UiKit.Muted;
        UiKit.Stretch(code.rectTransform, 12, 0);

        RectTransform info = ui.Rect("Info", tile);
        info.anchorMin = new Vector2(0, 0);
        info.anchorMax = new Vector2(1, 1);
        info.offsetMin = new Vector2(BadgeInset + BadgeSize + 12, 8);
        info.offsetMax = new Vector2(-10, -8);

        TMP_Text name = ui.Text(info, "Name", 18, TextAlignmentOptions.BottomLeft, DisplayName(plane));
        name.fontStyle = FontStyles.Bold;
        name.enableAutoSizing = true;
        name.fontSizeMin = 12;
        name.fontSizeMax = 18;
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.rectTransform.anchorMin = new Vector2(0, 0.52f);
        name.rectTransform.anchorMax = new Vector2(1, 1);
        name.rectTransform.offsetMin = Vector2.zero;
        name.rectTransform.offsetMax = Vector2.zero;

        string status = hasBadge ? $"<color=#FFC84D>Badge earned</color> · best {best}/{ExhibitProgress.QuestionsPerQuiz}"
            : best > 0 ? $"Best {best}/{ExhibitProgress.QuestionsPerQuiz} · tap to retry"
            : "Tap to take the quiz";
        string visit = visited ? "<color=#73E6FF>Visited in the museum</color>" : "Not visited yet";
        TMP_Text line = ui.Text(info, "Status", 13, TextAlignmentOptions.TopLeft, $"{status}\n{visit}");
        line.color = new Color(1, 1, 1, 0.75f);
        line.rectTransform.anchorMin = new Vector2(0, 0);
        line.rectTransform.anchorMax = new Vector2(1, 0.5f);
        line.rectTransform.offsetMin = Vector2.zero;
        line.rectTransform.offsetMax = Vector2.zero;

        return tile;
    }

    // ------------------------------------------------------------------- quiz

    void StartQuiz(PlaneSO plane)
    {
        currentPlane = plane;
        questions = BuildQuiz(plane);
        questionIndex = 0;
        score = 0;
        ShowQuestion();
    }

    void ShowQuestion()
    {
        RectTransform root = ResetContent();
        Question question = questions[questionIndex];

        Header(root, DisplayName(currentPlane), "Badge quiz",
            $"Question {questionIndex + 1} of {questions.Count}", "Badges", ShowBadges);

        RectTransform body = ui.Rect("Question", root);
        body.anchorMin = new Vector2(0, 0.5f);
        body.anchorMax = new Vector2(1, 1);
        body.offsetMin = new Vector2(SideInset, 8);
        body.offsetMax = new Vector2(-SideInset, -HeaderHeight - 6);

        string prompt = question.Prompt;
        if (!string.IsNullOrEmpty(question.Quote))
        {
            prompt += $"\n<size=80%><color=#FFFFFFCC><i>\"{question.Quote}\"</i></color></size>";
        }
        TMP_Text promptText = ui.Text(body, "Prompt", 24, TextAlignmentOptions.MidlineLeft, prompt);
        promptText.enableAutoSizing = true;
        promptText.fontSizeMin = 14;
        promptText.fontSizeMax = 24;
        UiKit.Stretch(promptText.rectTransform);

        RectTransform options = ui.Rect("Options", root);
        options.anchorMin = new Vector2(0, 0);
        options.anchorMax = new Vector2(1, 0.5f);
        options.offsetMin = new Vector2(SideInset - 6, 62);
        options.offsetMax = new Vector2(-SideInset + 6, 0);

        TMP_Text feedback = ui.Text(root, "Feedback", 18, TextAlignmentOptions.MidlineLeft, "");
        UiKit.Anchor(feedback.rectTransform, new Vector2(0, 0), new Vector2(SideInset, 12), new Vector2(520, 44));

        Button next = ui.Button(root, "Next Button",
            questionIndex + 1 < questions.Count ? "Next" : "See result", 18, null);
        UiKit.Anchor((RectTransform)next.transform, new Vector2(1, 0), new Vector2(-SideInset, 12), new Vector2(170, 44));
        next.gameObject.SetActive(false);
        next.onClick.AddListener(() =>
        {
            questionIndex++;
            if (questionIndex < questions.Count) ShowQuestion();
            else ShowResult();
        });

        List<Button> buttons = new();
        for (int i = 0; i < question.Options.Count; i++)
        {
            int choice = i;
            Button option = ui.Button(options, $"Option {i + 1}", question.Options[i], 17, null, out TMP_Text label);
            label.enableAutoSizing = true;
            label.fontSizeMin = 11;
            label.fontSizeMax = 17;
            UiKit.Stretch(label.rectTransform, 16, 4);
            UiKit.Region((RectTransform)option.transform,
                new Vector2(i % 2 * 0.5f, i < 2 ? 0.5f : 0),
                new Vector2(i % 2 * 0.5f + 0.5f, i < 2 ? 1 : 0.5f), 6);
            buttons.Add(option);
            option.onClick.AddListener(() => Answer(question, choice, buttons, feedback, next));
        }
    }

    void Answer(Question question, int choice, List<Button> buttons, TMP_Text feedback, Button next)
    {
        bool right = choice == question.Answer;
        if (right) score++;

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].interactable = false;
            // Keep the answer colours at full strength rather than the disabled tint.
            ColorBlock colours = buttons[i].colors;
            colours.disabledColor = Color.white;
            buttons[i].colors = colours;
            Color tint = i == question.Answer ? UiKit.Correct : i == choice ? UiKit.Wrong : new Color(1, 1, 1, 0.35f);
            buttons[i].targetGraphic.color = tint;
        }

        feedback.text = right ? "<color=#5FD884>Correct!</color>"
            : $"<color=#E26A6A>Not quite.</color> The answer is highlighted in green.";
        next.gameObject.SetActive(true);
    }

    void ShowResult()
    {
        RectTransform root = ResetContent();
        bool newBadge = ExhibitProgress.RecordScore(currentPlane.Name, score);
        bool passed = score >= ExhibitProgress.PassMark;
        int earned = planes.Count(p => ExhibitProgress.HasBadge(p.Name));

        Header(root, DisplayName(currentPlane), "Badge quiz", $"{earned} of {planes.Count} badges", "Badges", ShowBadges);

        RectTransform badge = ui.Rect("Badge", root);
        UiKit.Anchor(badge, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(140, 140));
        badge.pivot = new Vector2(0.5f, 0.5f);
        Image disc = ui.Disc(badge, "Disc", passed ? UiKit.Gold : new Color(0.22f, 0.26f, 0.34f, 1));
        UiKit.Stretch(disc.rectTransform, 8, 8);
        Image ring = ui.Ring(badge, "Ring", passed ? UiKit.Gold : new Color(1, 1, 1, 0.3f));
        UiKit.Stretch(ring.rectTransform);
        TMP_Text code = ui.Text(badge, "Code", 30, TextAlignmentOptions.Center, ExhibitFacts.For(currentPlane.Name)?.ShortCode ?? "?");
        code.fontStyle = FontStyles.Bold;
        code.color = passed ? new Color(0.1f, 0.12f, 0.2f) : UiKit.Muted;
        UiKit.Stretch(code.rectTransform, 16, 0);

        string headline = newBadge ? "Badge earned!" : passed ? "Passed — badge already in your collection" : "Not this time";
        string detail = $"You scored {score} of {questions.Count}.";
        if (!passed)
        {
            detail += $" Score {ExhibitProgress.PassMark} or more to earn the badge. Tip: read the exhibit's plaque in the museum first.";
        }
        else if (newBadge && earned == planes.Count)
        {
            detail += " That completes the collection: you are a Hangar Master!";
        }

        TMP_Text text = ui.Text(root, "Result", 26, TextAlignmentOptions.Top, $"<b>{headline}</b>\n<size=70%>{detail}</size>");
        text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.pivot = new Vector2(0.5f, 1);
        text.rectTransform.anchoredPosition = new Vector2(0, -42);
        text.rectTransform.sizeDelta = new Vector2(620, 110);

        Button retry = ui.Button(root, "Retry Button", "Try again", 18, () => StartQuiz(currentPlane));
        UiKit.Anchor((RectTransform)retry.transform, new Vector2(0.5f, 0), new Vector2(-100, 16), new Vector2(180, 44));
        ((RectTransform)retry.transform).pivot = new Vector2(0.5f, 0);
        Button back = ui.Button(root, "All Badges Button", "All badges", 18, ShowBadges);
        UiKit.Anchor((RectTransform)back.transform, new Vector2(0.5f, 0), new Vector2(100, 16), new Vector2(180, 44));
        ((RectTransform)back.transform).pivot = new Vector2(0.5f, 0);
    }

    // ---------------------------------------------------------------- header

    void Header(RectTransform root, string title, string subtitle, string status, string buttonLabel, UnityEngine.Events.UnityAction onButton)
    {
        RectTransform header = ui.Rect("Header", root);
        header.anchorMin = new Vector2(0, 1);
        header.anchorMax = new Vector2(1, 1);
        header.pivot = new Vector2(0.5f, 1);
        header.sizeDelta = new Vector2(0, HeaderHeight);

        TMP_Text titleText = ui.Text(header, "Title", 28, TextAlignmentOptions.BottomLeft, title);
        titleText.fontStyle = FontStyles.Bold;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.rectTransform.anchorMin = new Vector2(0, 0.45f);
        titleText.rectTransform.anchorMax = new Vector2(0.6f, 1);
        titleText.rectTransform.offsetMin = new Vector2(SideInset, 0);
        titleText.rectTransform.offsetMax = new Vector2(0, -8);

        TMP_Text subtitleText = ui.Text(header, "Subtitle", 14, TextAlignmentOptions.TopLeft, subtitle);
        subtitleText.color = new Color(1, 1, 1, 0.7f);
        subtitleText.rectTransform.anchorMin = new Vector2(0, 0);
        subtitleText.rectTransform.anchorMax = new Vector2(0.7f, 0.45f);
        subtitleText.rectTransform.offsetMin = new Vector2(SideInset, 0);
        subtitleText.rectTransform.offsetMax = new Vector2(0, -2);

        Button button = ui.Button(header, "Header Button", buttonLabel, 18, onButton);
        UiKit.Anchor((RectTransform)button.transform, new Vector2(1, 0.5f), new Vector2(-SideInset, 0), new Vector2(120, 42));

        TMP_Text statusText = ui.Text(header, "Status", 16, TextAlignmentOptions.MidlineRight, status);
        statusText.color = UiKit.Cyan;
        statusText.textWrappingMode = TextWrappingModes.NoWrap;
        UiKit.Anchor(statusText.rectTransform, new Vector2(1, 0.5f), new Vector2(-SideInset - 136, 0), new Vector2(220, 42));
    }

    // ------------------------------------------------------------- questions

    List<Question> BuildQuiz(PlaneSO plane)
    {
        ExhibitFacts facts = ExhibitFacts.For(plane.Name);
        List<Question> pool = new();

        void Add(string prompt, string correct, IEnumerable<string> values)
        {
            Question question = MakeQuestion(prompt, correct, values);
            if (question != null) pool.Add(question);
        }

        string name = DisplayName(plane);
        Add($"Which company built the {name}?", plane.Manufacturer, planes.Select(p => p.Manufacturer));
        Add($"In what year did the {name} first fly?", plane.Date, planes.Select(p => p.Date));
        Add($"What is the {name}'s top speed?", plane.TSpeed, planes.Select(p => p.TSpeed));
        Add($"What is the {name}'s wingspan?", plane.WingSpan, planes.Select(p => p.WingSpan));
        if (facts != null)
        {
            Add($"What powers the {name}?", facts.Engines, planes.Select(p => ExhibitFacts.For(p.Name)?.Engines));
            Add($"What gun does the {name} carry?", facts.Gun, planes.Select(p => ExhibitFacts.For(p.Name)?.Gun));
        }

        List<Question> quiz = pool.OrderBy(_ => Random.value).Take(ExhibitProgress.QuestionsPerQuiz - 1).ToList();

        // Always one "which aircraft is this?" question, placed at a random point.
        Question identify = facts != null
            ? MakeQuestion("Which aircraft is this?", name, planes.Select(DisplayName))
            : null;
        if (identify != null)
        {
            identify.Quote = facts.Trivia;
            quiz.Insert(Random.Range(0, quiz.Count + 1), identify);
        }

        return quiz;
    }

    static Question MakeQuestion(string prompt, string correct, IEnumerable<string> values)
    {
        if (string.IsNullOrWhiteSpace(correct))
        {
            return null;
        }

        List<string> wrong = values
            .Where(v => !string.IsNullOrWhiteSpace(v) && v.Trim() != correct.Trim())
            .Distinct()
            .OrderBy(_ => Random.value)
            .Take(3)
            .ToList();
        if (wrong.Count < 3)
        {
            return null;
        }

        List<string> options = wrong.Append(correct).OrderBy(_ => Random.value).ToList();
        return new Question { Prompt = prompt, Options = options, Answer = options.IndexOf(correct) };
    }

    static string DisplayName(PlaneSO plane)
    {
        return ExhibitFacts.For(plane.Name)?.DisplayName ?? plane.Name;
    }
}
