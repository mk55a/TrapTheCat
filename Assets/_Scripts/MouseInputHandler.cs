using UnityEngine;
using UnityEngine.EventSystems;

public class MouseInputHandler : MonoBehaviour
{
    public static MouseInputHandler Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver ||
            GameManager.Instance.GetCurrentTurn() != Turn.Player ||
            !Input.GetMouseButtonDown(0) ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            return;

        Camera mainCamera = Camera.main;
        GridManager gridManager = GridManager.Instance;
        if (mainCamera == null || gridManager == null || gridManager.grid == null)
            return;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0f;
        GridMapObject cell = gridManager.grid.GetGridObject(worldPosition);
        if (cell != null && gridManager.SetGridAsFull(cell, worldPosition))
            GameManager.Instance.RegisterPlayerMove();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
