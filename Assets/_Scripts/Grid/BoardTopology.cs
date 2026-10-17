using System.Collections.Generic;
using UnityEngine;

public static class BoardTopology
{
    public static bool[,] BuildMask(int width, int height, BoardShape shape)
    {
        bool[,] mask = new bool[width, height];
        float centerX = (width - 1) * 0.5f;
        float centerY = (height - 1) * 0.5f;
        float squareRadius = (Mathf.Min(width, height) - 1) * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                switch (shape)
                {
                    case BoardShape.Square:
                        mask[x, y] = Mathf.Abs(x - centerX) <= squareRadius &&
                                     Mathf.Abs(y - centerY) <= squareRadius;
                        break;
                    case BoardShape.Circle:
                        float halfWidth = Mathf.Max(0.5f, (width - 1) * 0.5f);
                        float halfHeight = Mathf.Max(0.5f, (height - 1) * 0.5f);
                        float dx = (x - centerX) / halfWidth;
                        float dy = (y - centerY) / halfHeight;
                        mask[x, y] = dx * dx + dy * dy <= 1f;
                        break;
                    case BoardShape.Hexagon:
                        float centerAxialX = centerX - Mathf.Floor(centerY / 2f);
                        float axialX = x - (y - (y & 1)) / 2;
                        float radius = Mathf.Max(1f, squareRadius);
                        float q = Mathf.Abs(axialX - centerAxialX);
                        float r = Mathf.Abs(y - centerY);
                        float diagonal = Mathf.Abs((axialX + y) - (centerAxialX + centerY));
                        mask[x, y] = Mathf.Max(q, Mathf.Max(r, diagonal)) <= radius;
                        break;
                    default:
                        mask[x, y] = true;
                        break;
                }
            }
        }

        return mask;
    }

    public static bool IsActive(int width, int height, BoardShape shape, int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return false;

        return BuildMask(width, height, shape)[x, y];
    }

    public static List<Vector2Int> GetNeighbors(bool[,] activeCells, int x, int y)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>(6);
        int height = activeCells.GetLength(1);
        bool oddRow = (y & 1) == 1;
        AddIfActive(neighbors, activeCells, x - 1, y);
        AddIfActive(neighbors, activeCells, x + 1, y);
        AddIfActive(neighbors, activeCells, x + (oddRow ? 0 : -1), y - 1);
        AddIfActive(neighbors, activeCells, x + (oddRow ? 1 : 0), y - 1);
        AddIfActive(neighbors, activeCells, x + (oddRow ? 0 : -1), y + 1);
        AddIfActive(neighbors, activeCells, x + (oddRow ? 1 : 0), y + 1);
        return neighbors;
    }

    public static bool IsBoundary(bool[,] activeCells, int x, int y)
    {
        return activeCells[x, y] && GetNeighbors(activeCells, x, y).Count < 6;
    }

    private static void AddIfActive(
        List<Vector2Int> neighbors,
        bool[,] activeCells,
        int x,
        int y)
    {
        if (x >= 0 && y >= 0 &&
            x < activeCells.GetLength(0) &&
            y < activeCells.GetLength(1) &&
            activeCells[x, y])
            neighbors.Add(new Vector2Int(x, y));
    }
}
