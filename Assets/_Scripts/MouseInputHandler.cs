using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseInputHandler : MonoBehaviour
{
    public static MouseInputHandler Instance;

    [SerializeField] private bool _canClick;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        GameManager.OnTurnChanged += HandleTurnChanged; 
    }

    private void OnDisable()
    {
        GameManager.OnTurnChanged -= HandleTurnChanged;   
    }

    void Update()
    {
        if (_canClick && Input.GetMouseButtonDown(0))
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPosition.z = 0f; 

            GridMapObject gridObject = GridManager.Instance.grid.GetGridObject(worldPosition);
            
            if (gridObject != null)
            {
                if (gridObject.GetType() == GridType.Empty)
                {
                    Debug.Log("Empty Gridobject becoming full");
                    GridManager.Instance.SetGridAsFull(gridObject, worldPosition);
                    ToggleCanClick();
                }
                else if(gridObject.GetType() == GridType.Full)
                {
                    Debug.Log("Select Something else");
                }
                else if (gridObject.GetType() == GridType.Cat)
                {
                    Debug.LogWarning("You can't select the CAT");
                }
                else if(gridObject.GetType() == GridType.Boundary)
                {
                    Debug.Log("You JUST HIT THE BOUNDARY");
                    GridManager.Instance.SetGridAsFull(gridObject, worldPosition);
                    ToggleCanClick();
                }
            }
        }
    }

    private void ToggleCanClick()
    {
        _canClick = !_canClick;
        GameManager.Instance.ChangeTurn();
    }

    private void HandleTurnChanged()
    {
        if (GameManager.Instance.GetCurrentTurn() == Turn.Player)
        {
            _canClick = true;
        }
        else
        {
            _canClick = false;
        }
    }

}
