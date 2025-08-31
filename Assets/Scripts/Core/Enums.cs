namespace MyGame.Core
{
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver,
        PlayerTurn,
        EnemyTurn,
        CombatPlayerTurn,
        CombatEnemyTurn,
        OpenWorldExploration,
        ClosedWorldExploration,
        Cutscene,
        InventoryOpen,
        LevelComplete
    }
}