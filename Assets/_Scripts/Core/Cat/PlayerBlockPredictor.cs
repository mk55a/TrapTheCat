using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class PlayerBlockPredictor
    {
        private const int OffRouteSlack = 99;
        private const int CellBits = 12;
        private const int CellMask = (1 << CellBits) - 1;

        private readonly CatSearchBoard _board;
        private readonly CatEscapeRouteFinder _routes;
        private readonly int[] _distanceFromCat;
        private readonly int[] _distanceToExit;
        private readonly int[] _queue;

        public PlayerBlockPredictor(CatEscapeRouteFinder routes)
        {
            _routes = routes;
            _board = routes.Board;
            _distanceFromCat = new int[_board.Count];
            _distanceToExit = new int[_board.Count];
            _queue = new int[_board.Count];
        }

        public List<int> LikelyBlocks(int cat, int max)
        {
            List<int> result = new List<int>();
            _routes.SearchFrom(cat);
            for (int i = 0; i < _board.Count; i++)
                _distanceFromCat[i] = _routes.Distance(i);

            MeasureDistanceToExits();
            int shortest = _distanceToExit[cat];
            if (shortest <= 0)
                return result;

            List<int> keys = new List<int>();
            for (int i = 0; i < _board.Count; i++)
            {
                if (i == cat || _board.IsBlocked(i) || _distanceFromCat[i] < 0)
                    continue;

                int slack = _distanceToExit[i] < 0
                    ? OffRouteSlack
                    : _distanceFromCat[i] + _distanceToExit[i] - shortest;
                if (slack > 1 && _distanceFromCat[i] > 2)
                    continue;

                int tier = slack == 0 ? 0 : (slack == 1 ? 1 : 2);
                keys.Add(((tier * 16 + Mathf.Min(_distanceFromCat[i], 15)) << CellBits) | i);
            }

            keys.Sort();
            for (int i = 0; i < keys.Count && result.Count < max; i++)
                result.Add(keys[i] & CellMask);
            return result;
        }

        private void MeasureDistanceToExits()
        {
            int head = 0;
            int tail = 0;
            for (int i = 0; i < _board.Count; i++)
            {
                _distanceToExit[i] = -1;
                if (_board.IsExit(i) && _distanceFromCat[i] > 0)
                {
                    _distanceToExit[i] = 0;
                    _queue[tail++] = i;
                }
            }

            while (head < tail)
            {
                int current = _queue[head++];
                int[] neighbours = _board.Neighbours(current);
                for (int i = 0; i < neighbours.Length; i++)
                {
                    int next = neighbours[i];
                    if (_distanceToExit[next] >= 0 || _board.IsBlocked(next) || _board.IsExit(next))
                        continue;

                    _distanceToExit[next] = _distanceToExit[current] + 1;
                    _queue[tail++] = next;
                }
            }
        }
    }
}
