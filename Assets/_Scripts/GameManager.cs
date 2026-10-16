using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI; 
 
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static event Action OnTurnChanged;

    private Turn _currentTurn;
    private GridManager gridManager;

    [SerializeField] private GameObject _cat;
    [SerializeField] private Button _restartButton;
    [SerializeField] private GameObject _catTrappedScreen;
    [SerializeField] private Button _catTrappedReplayButton;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        gridManager = GridManager.Instance;
        PlaceCatAtCenter();
        _currentTurn = Turn.Player;
        OnTurnChanged?.Invoke();
        
    }

    private void OnEnable()
    {
        _restartButton.onClick.AddListener(RestartGame);
        _catTrappedReplayButton.onClick.AddListener(RestartGame);
        CatMovement.OnCatTrapped += OnCatTrapped;
    }

    private void OnDisable()
    {
        _restartButton.onClick.RemoveListener(RestartGame);
        _catTrappedReplayButton.onClick.RemoveListener(RestartGame);
        CatMovement.OnCatTrapped -= OnCatTrapped;
    }

    public void ChangeTurn()
    {
        if (_currentTurn == Turn.Player)
        {
            _currentTurn = Turn.Cat;
        }
        else
        {
            _currentTurn = Turn.Player;
        }

        OnTurnChanged?.Invoke();
    }

    public Turn GetCurrentTurn()
    {
        return _currentTurn;
    }

    public void OnCatTrapped()
    {
        _catTrappedScreen.SetActive(true);
    }

    private void PlaceCatAtCenter()
    {
        if(_cat !=null && gridManager != null)
        {
            Vector3 centerGridPosition = gridManager.GetCenterGridPosition();
            _cat.transform.position = centerGridPosition; 
        }
        else
        {
            Debug.LogError("Cat or GridManager is not assigned");
        }
    }

    private void RestartGame()
    {
        _catTrappedScreen.SetActive(false);
        gridManager.DestroyGrid();

        // Generate a new grid
        gridManager.InitiateGrid();

        // Place the cat at the center of the new grid
        PlaceCatAtCenter();
        _cat.GetComponent<CatMovement>().pathVectorList = null;

        // Reset the turn to Player's turn
        _currentTurn = Turn.Player;
        OnTurnChanged?.Invoke();
    }

}
