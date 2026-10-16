using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class  CatMovement : MonoBehaviour
{
    public static event Action OnCatTrapped;

    public List<Vector3> pathVectorList;
    public Pathfinding pathfinding;

    [SerializeField] private float _speed;

    private Cat _cat;
    private GridManager _gridManager;
    private LineRenderer _lineRenderer;

    private Vector3 _lastGridPosition;
    private Vector3 _lastValidPosition;

    private void Awake()
    {
        _cat = GetComponent<Cat>();
        _gridManager = GridManager.Instance;
        pathfinding = new Pathfinding(_gridManager.width, _gridManager.height);
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (_cat.canMove)
        {
            MoveTowardsTarget();
        }
    }

    private void MoveTowardsTarget()
    {
        if (pathVectorList != null && pathVectorList.Count > 0)
        {
            Vector3 targetPosition = pathVectorList[0];
            _gridManager.grid.GetXY(targetPosition, out int targetX, out int targetY);

            if(_gridManager.grid.GetGridObject(targetX, targetY).GetType() != GridType.Full)
            {
                if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
                {
                    Vector3 moveDir = (targetPosition - transform.position).normalized;
                    transform.position += moveDir * _speed * Time.deltaTime;
                }
                else
                {
                    // Reached the target node
                    HandleGridExit();
                    HandleGridEnter();
                    pathVectorList.RemoveAt(0);
                    _gridManager.grid.GetXY(transform.position, out int currentX, out int currentY);
                    Debug.LogWarning($"Cat moved through grid: ({currentX}, {currentY})");
                    
                    if (pathVectorList.Count == 0)
                    {
                        _gridManager.grid.GetXY(transform.position, out int finalX, out int finalY);
                        Debug.LogWarning($"Cat stopped at final grid: ({finalX}, {finalY})");
                        Debug.LogError("GAAAAAMMMMMMMMME OVEEEEEEEEEEER");
                        MoveCatOutOfView();
                        pathVectorList = null;

                        GameManager.Instance.ChangeTurn();
                    }
                    else
                    {
                        HandleGridEnter();
                        GameManager.Instance.ChangeTurn();
                    }
                }
            }
            else
            {
                Debug.LogError($"Encountered blocked node at ({targetX}, {targetY}). Recalculating path.");
                
                pathVectorList = null;
                FindClosestBoundaryAndPath();
            }
        }
        else
        {
            FindClosestBoundaryAndPath();
        }
    }

    private void FindClosestBoundaryAndPath()
    {
        Vector3 catPosition = transform.position;
        _gridManager.grid.GetXY(catPosition, out int catX, out int catY);

        List<PathNode> closestPath = null;
        float closestDistance = float.MaxValue;

        foreach (Coordinates boundaryCoord in _gridManager.GetBoundaryCoordinates())
        {
            var gridObject = _gridManager.grid.GetGridObject(boundaryCoord.X, boundaryCoord.Y);
            if (gridObject.GetType() == GridType.Boundary)
            {
                List<PathNode> pathToBoundary = FindPath(catX, catY, boundaryCoord.X, boundaryCoord.Y);
                if (pathToBoundary != null && pathToBoundary.Count > 0)
                {
                    float distanceToBoundary = pathToBoundary.Count;
                    bool pathIsValid = true;

                    foreach (PathNode node in pathToBoundary)
                    {
                        if (_gridManager.grid.GetGridObject(node.x, node.y).GetType() == GridType.Full)
                        {
                            pathIsValid = false;
                            break;
                        }
                    }

                    if (pathIsValid && distanceToBoundary < closestDistance)
                    {

                        closestDistance = distanceToBoundary;
                        closestPath = pathToBoundary;
                    }
                }
            }
            else
            {
                Debug.Log($"Boundary at ({boundaryCoord.X}, {boundaryCoord.Y}) is not walkable");
            }
        }

        if (closestPath != null && closestPath.Count > 0)
        {
            pathVectorList = new List<Vector3>();
            foreach (PathNode node in closestPath)
            {
                Debug.LogWarning(node.x + " : " + node.y);
                pathVectorList.Add(_gridManager.GetCenterGridPosition(node.x, node.y));
            }
            _lastValidPosition = pathVectorList[pathVectorList.Count - 1];
            HandleGridEnter();

            Vector3 finalTargetPosition = pathVectorList[pathVectorList.Count - 1];
            _gridManager.grid.GetXY(finalTargetPosition, out int finalTargetX, out int finalTargetY);
            Debug.LogWarning($"Cat's final target position: ({finalTargetX}, {finalTargetY})");
        }
        else
        {
            OnCatTrapped?.Invoke();
            Debug.LogError("No path found to boundary ");
        }
    }

    private List<PathNode> FindPath(int startX, int startY, int endX, int endY)
    {
        if (pathfinding != null)
        {
            
            List<PathNode> path = pathfinding.FindPath(startX, startY, endX, endY);
            if (path != null  && path.Count > 1)
            {
                path.RemoveAt(0); // Remove the starting node to avoid moving to same position
            }
            return path;
        }
        else
        {
            Debug.LogError("Pathfinding instance is null");
            return null;
        }
    }

    private void UpdateLineRenderer()
    {
        if (pathVectorList != null && _lineRenderer != null)
        {
            _lineRenderer.positionCount = pathVectorList.Count;
            for (int i = 0; i < pathVectorList.Count; i++)
            {
                _lineRenderer.SetPosition(i, pathVectorList[i]);
            }
        }
    }

    private void HandleGridExit()
    {
        // Change the last grid position to Empty
        if (_lastGridPosition != Vector3.zero)
        {
            _gridManager.grid.GetXY(_lastGridPosition, out int lastX, out int lastY);
            if (_gridManager.grid.GetGridObject(lastX, lastY).GetType() == GridType.Cat)
            {
                _gridManager.grid.GetGridObject(lastX, lastY).SetType(GridType.Empty);
            }
        }
    }

    private void HandleGridEnter()
    {

        _gridManager.grid.GetXY(transform.position, out int currentX, out int currentY);
        _gridManager.grid.GetGridObject(currentX, currentY).SetType(GridType.Cat);


        _lastGridPosition = transform.position;
    }

    private void MoveCatOutOfView()
    {
        Vector3 catPosition = transform.position;
        Camera mainCamera = Camera.main;


        Vector3 screenBottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 screenTopRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));

        float distanceToLeft = Mathf.Abs(catPosition.x - screenBottomLeft.x);
        float distanceToRight = Mathf.Abs(catPosition.x - screenTopRight.x);
        float distanceToBottom = Mathf.Abs(catPosition.y - screenBottomLeft.y);
        float distanceToTop = Mathf.Abs(catPosition.y - screenTopRight.y);

        Vector3 targetPosition = catPosition;

        if (distanceToLeft <= distanceToRight && distanceToLeft <= distanceToBottom && distanceToLeft <= distanceToTop)
        {
            targetPosition.x = screenBottomLeft.x - 1; 
        }
        else if (distanceToRight <= distanceToLeft && distanceToRight <= distanceToBottom && distanceToRight <= distanceToTop)
        {
            targetPosition.x = screenTopRight.x + 1; 
        }
        else if (distanceToBottom <= distanceToLeft && distanceToBottom <= distanceToRight && distanceToBottom <= distanceToTop)
        {
            targetPosition.y = screenBottomLeft.y - 1; 
        }
        else if (distanceToTop <= distanceToLeft && distanceToTop <= distanceToRight && distanceToTop <= distanceToBottom)
        {
            targetPosition.y = screenTopRight.y + 1; 
        }

        StartCoroutine(MoveCatToPosition(targetPosition));
    }

    private IEnumerator MoveCatToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            Vector3 moveDir = (targetPosition - transform.position).normalized;
            transform.position += moveDir * _speed * Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;
    }
}