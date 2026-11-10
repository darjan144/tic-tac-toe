using UnityEngine;

public class SaveManager : SingletonMonoBehaviour<SaveManager>
{
    public bool IsBGMEnabled
    {
        get => PlayerPrefs.GetInt("BGMEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("BGMEnabled", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public bool IsSFXEnabled
    {
        get => PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
        set
        {
            PlayerPrefs.SetInt("SFXEnabled", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>User consent for personalised audio ads (GDPR). Off until the player opts in.</summary>
    public bool AdsConsent
    {
        get => PlayerPrefs.GetInt("AdsConsent", 0) == 1;
        set
        {
            PlayerPrefs.SetInt("AdsConsent", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public bool IsThemeUnlocked(int themeIndex) => PlayerPrefs.GetInt($"ThemeUnlocked_{themeIndex}", 0) == 1;

    public void UnlockTheme(int themeIndex)
    {
        PlayerPrefs.SetInt($"ThemeUnlocked_{themeIndex}", 1);
        PlayerPrefs.Save();
        Debug.Log($"[Save] Theme {themeIndex} unlocked");
    }

    public int TotalGames
    {
        get => PlayerPrefs.GetInt("TotalGames", 0);
        private set => PlayerPrefs.SetInt("TotalGames", value);
    }

    public int P1Wins
    {
        get => PlayerPrefs.GetInt("P1Wins", 0);
        private set => PlayerPrefs.SetInt("P1Wins", value);
    }

    public int P2Wins
    {
        get => PlayerPrefs.GetInt("P2Wins", 0);
        private set => PlayerPrefs.SetInt("P2Wins", value);
    }

    public int Draws
    {
        get => PlayerPrefs.GetInt("Draws", 0);
        private set => PlayerPrefs.SetInt("Draws", value);
    }

    public float CumulativeDuration
    {
        get => PlayerPrefs.GetFloat("CumulativeDuration", 0f);
        private set => PlayerPrefs.SetFloat("CumulativeDuration", value);
    }

    public float AverageDuration => TotalGames > 0 ? CumulativeDuration / TotalGames : 0f;

    public void RecordGameResult(Player winner, float duration)
    {
        TotalGames++;
        CumulativeDuration += duration;

        switch (winner)
        {
            case Player.P1: P1Wins++; break;
            case Player.P2: P2Wins++; break;
            default: Draws++; break;
        }

        PlayerPrefs.Save();
    }
}
