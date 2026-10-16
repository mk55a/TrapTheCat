using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GridTile : MonoBehaviour
{
    public GridMapObject gridMapObject;
    private SpriteRenderer _spriteRenderer;

    [SerializeField] Color _tileColor; 

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        ChangeGridTileSpriteColor();
    }

    private void ChangeGridTileSpriteColor()
    {
        GridMapObject gridObject = GridManager.Instance.grid.GetGridObject(transform.position);
        if (gridObject != null)
        {
            if(gridObject.GetType() == GridType.Full)
            {
                _spriteRenderer.color = Color.black;
            }
            if(gridObject.GetType() == GridType.Empty)
            {
                _spriteRenderer.color = _tileColor;
            }
        }
    }
}
