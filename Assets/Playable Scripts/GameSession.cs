using System;

public interface IGameSessionController
{
    bool IsGameOver { get; }
    bool IsEditorMode { get; }
    Turn GetCurrentTurn();
    void RegisterPlayerMove();
    void ChangeTurn();
}

public static class GameSession
{
    public static IGameSessionController Current { get; private set; }
    public static event Action TurnChanged;

    public static void Attach(IGameSessionController session)
    {
        Current = session;
    }

    public static void Detach(IGameSessionController session)
    {
        if (Current == session)
            Current = null;
    }

    public static void NotifyTurnChanged()
    {
        TurnChanged?.Invoke();
    }
}
