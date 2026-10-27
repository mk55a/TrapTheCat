using TrapTheCat.Core;
using UnityEngine;

namespace TrapTheCat.Presentation
{
    public sealed class HexLayout
    {
        private const float ArtWidthPx = 30f;
        private const float ArtHeightPx = 32f;
        private const float RowPitchPx = 24f;

        private readonly float _columnPitch;
        private readonly float _rowPitch;

        public Vector3 Origin { get; private set; }
        public float TileWidth { get; private set; }
        public float TileHeight { get; private set; }

        public HexLayout(float tileWidth, float spacing, Vector3 origin)
        {
            TileWidth = tileWidth;
            TileHeight = tileWidth * ArtHeightPx / ArtWidthPx;
            _columnPitch = tileWidth * spacing;
            _rowPitch = tileWidth * RowPitchPx / ArtWidthPx * spacing;
            Origin = origin;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            float rowOffset = (cell.y & 1) == 1 ? _columnPitch * 0.5f : 0f;
            return new Vector3(
                Origin.x + TileWidth * 0.5f + cell.x * _columnPitch + rowOffset,
                Origin.y + TileHeight * 0.5f + cell.y * _rowPitch,
                Origin.z);
        }

        public bool TryWorldToCell(Vector3 worldPosition, BoardLayout board, out Vector2Int cell)
        {
            cell = Vector2Int.zero;
            float halfWidth = TileWidth * 0.5f;
            float halfHeight = TileHeight * 0.5f;
            int approximateRow = Mathf.RoundToInt((worldPosition.y - Origin.y - halfHeight) / _rowPitch);
            float closestDistance = float.MaxValue;
            bool found = false;

            for (int y = approximateRow - 1; y <= approximateRow + 1; y++)
            {
                float rowOffset = (y & 1) == 1 ? _columnPitch * 0.5f : 0f;
                int approximateColumn = Mathf.RoundToInt(
                    (worldPosition.x - Origin.x - halfWidth - rowOffset) / _columnPitch);

                for (int x = approximateColumn - 1; x <= approximateColumn + 1; x++)
                {
                    Vector2Int candidate = new Vector2Int(x, y);
                    if (!board.Contains(candidate))
                        continue;

                    Vector3 center = CellToWorld(candidate);
                    float dx = Mathf.Abs(worldPosition.x - center.x);
                    float dy = Mathf.Abs(worldPosition.y - center.y);
                    bool insideHex = dx <= halfWidth &&
                                     (dy <= halfHeight * 0.5f ||
                                      dx / halfWidth + 2f * dy / halfHeight <= 2f);
                    if (!insideHex)
                        continue;

                    float distance = dx * dx + dy * dy;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        cell = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        public Vector2 GetBoardSize(int width, int height)
        {
            return new Vector2(
                (width - 1) * _columnPitch + RightmostRowOffset(height) + TileWidth,
                (height - 1) * _rowPitch + TileHeight);
        }

        public Vector2 GetBoardCentreOffset(int width, int height)
        {
            return new Vector2(
                TileWidth * 0.5f + ((width - 1) * _columnPitch + RightmostRowOffset(height)) * 0.5f,
                TileHeight * 0.5f + (height - 1) * _rowPitch * 0.5f);
        }

        private float RightmostRowOffset(int height)
        {
            return height > 1 ? _columnPitch * 0.5f : 0f;
        }
    }
}
