namespace MyGame.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        PlayerTurn,
        EnemyTurn,
        CombatPlayerTurn,
        CombatEnemyTurn,
        OpenWorldExploration,
        ClosedWorldExploration,
        GameOver,
        Cutscene,
        InventoryOpen,
        LevelComplete
    }
}