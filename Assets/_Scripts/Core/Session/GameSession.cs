using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class GameSession
    {
        private readonly CatBrain _brain;

        public BoardState Board { get; private set; }
        public GameResult Result { get; private set; }
        public int PlayerMoves { get; private set; }

        public bool IsOver
        {
            get { return Result != GameResult.Playing; }
        }

        public GameSession(BoardState board, CatDifficulty difficulty)
        {
            Board = board;
            _brain = new CatBrain(difficulty);
            Result = GameResult.Playing;
        }

        public bool CanPlaceBlock(Vector2Int cell)
        {
            return !IsOver && Board.CanPlaceBlock(cell);
        }

        public CatTurn PlaceBlock(Vector2Int cell)
        {
            if (!CanPlaceBlock(cell))
                return null;

            Board.PlaceBlock(cell);
            PlayerMoves++;
            return PlayCatTurn();
        }

        private CatTurn PlayCatTurn()
        {
            CatSearchBoard catBoard = CatSearchBoard.FromBoard(Board);
            int from = catBoard.Index(Board.CatCell);
            int next = _brain.ChooseMove(catBoard, from);
            if (next < 0)
            {
                Result = GameResult.Won;
                return CatTurn.WasTrapped();
            }

            Vector2Int to = catBoard.Cell(next);
            List<Vector2Int> route = new List<Vector2Int>();
            foreach (int index in PlannedRoute(catBoard, next))
                route.Add(catBoard.Cell(index));
            if (route.Count == 0)
                route.Add(to);

            Board.MoveCat(to);
            bool escaped = Board.Layout.IsExit(to);
            if (escaped)
                Result = GameResult.Lost;
            return CatTurn.MovedTo(to, escaped, route);
        }

        private List<int> PlannedRoute(CatSearchBoard catBoard, int from)
        {
            CatEscapeRouteFinder routes = new CatEscapeRouteFinder(catBoard);
            List<int> route = routes.RouteTo(from, _brain.Target);
            return route.Count > 0 ? route : routes.RouteToNearestExit(from);
        }
    }
}
