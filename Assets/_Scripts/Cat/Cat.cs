using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cat : MonoBehaviour
{
    public static Cat Instance;

    public bool canMove = false;

    private GridManager _gridManager;
    private Vector3 _targetPosition;

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

    private void OnEnable()
    {
        GameManager.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        GameManager.OnTurnChanged -= HandleTurnChanged;
    }

    private void Start()
    {
        _gridManager = GridManager.Instance; 
    }

    public void ToggleCanMove()
    {
        canMove = !canMove;
        GameManager.Instance.ChangeTurn(); 
    }

    private void HandleTurnChanged()
    {
        Debug.Log("HANDILING CATS TURNCHANGED");
        if(GameManager.Instance.GetCurrentTurn() == Turn.Cat)
        {
            canMove = true; 
        }
        else
        {
            canMove = false;
        }
    }



}

