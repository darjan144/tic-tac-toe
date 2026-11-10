/// <summary>
/// Placement ids registered for this game. In a real integration these match placements
/// created in the ad network's dashboard.
/// </summary>
public static class AdPlacements
{
    /// <summary>Standard (mutable) audio ad played when the player hits Retry.</summary>
    public const string BetweenGames = "standard_between_games";

    /// <summary>Rewarded ad that permanently unlocks the premium XO theme.</summary>
    public const string ThemeUnlock = "rewarded_theme_unlock";

    /// <summary>Rewarded ad that grants one extra undo after the free undos run out.</summary>
    public const string ExtraUndo = "rewarded_extra_undo";
}
