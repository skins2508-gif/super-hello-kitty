public static class GameModeSettings
{
    public static int PlayerMaxHealth { get; private set; } = 1;
    public static float BossMaxHealth { get; private set; } = 1000f;
    public static int SelectedCharacter { get; private set; } = -1;
    private static bool restartRequested;

    public static void SelectEasyMode()
    {
        PlayerMaxHealth = 3;
        BossMaxHealth = 500f;
    }

    public static void SelectNormalMode()
    {
        PlayerMaxHealth = 1;
        BossMaxHealth = 1000f;
    }

    public static void RequestRestart()
    {
        restartRequested = true;
    }

    public static void ReturnToMainMenu()
    {
        restartRequested = false;
    }

    public static bool ConsumeRestartRequest()
    {
        if (!restartRequested)
            return false;

        restartRequested = false;
        return true;
    }

    public static void SelectCharacter(int characterIndex)
    {
        SelectedCharacter = characterIndex;
    }
}
