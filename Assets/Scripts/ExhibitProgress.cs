using UnityEngine;

// Badge and visit progress, kept in PlayerPrefs so it survives between sessions.
// Keys are per aircraft name (PlaneSO.Name), which is stable across builds.
public static class ExhibitProgress
{
    public const int QuestionsPerQuiz = 5;
    public const int PassMark = 4;

    static string VisitedKey(string plane) => $"Visited_{plane}";
    static string BestKey(string plane) => $"QuizBest_{plane}";

    public static bool HasVisited(string plane) => PlayerPrefs.GetInt(VisitedKey(plane), 0) == 1;

    public static void MarkVisited(string plane)
    {
        if (string.IsNullOrEmpty(plane) || HasVisited(plane))
        {
            return;
        }

        PlayerPrefs.SetInt(VisitedKey(plane), 1);
        PlayerPrefs.Save();
    }

    public static int BestScore(string plane) => PlayerPrefs.GetInt(BestKey(plane), 0);

    public static bool HasBadge(string plane) => BestScore(plane) >= PassMark;

    // Returns true when this attempt earned the badge for the first time.
    public static bool RecordScore(string plane, int score)
    {
        bool hadBadge = HasBadge(plane);
        if (score > BestScore(plane))
        {
            PlayerPrefs.SetInt(BestKey(plane), score);
            PlayerPrefs.Save();
        }
        return !hadBadge && HasBadge(plane);
    }
}
