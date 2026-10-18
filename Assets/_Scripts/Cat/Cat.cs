using UnityEngine;

public class Cat : MonoBehaviour
{
    public static Cat Instance;
    public bool canMove;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        GameSession.TurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        GameSession.TurnChanged -= HandleTurnChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    private void HandleTurnChanged()
    {
        canMove = GameSession.Current != null &&
                  GameSession.Current.GetCurrentTurn() == Turn.Cat;
    }
}
