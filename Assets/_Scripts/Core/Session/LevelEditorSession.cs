using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    public sealed class LevelEditorSession
    {
        public const int MinSize = 6;
        public const int MaxSize = 16;
        public const int DefaultSize = 10;

        private struct EditAction
        {
            public bool IsCat;
            public Vector2Int Cell;
            public bool HadCat;
            public Vector2Int PreviousCat;
        }

        private readonly IReadOnlyList<BoardShape> _shapes;
        private readonly System.Random _random;
        private readonly HashSet<Vector2Int> _blocks = new HashSet<Vector2Int>();
        private readonly Stack<EditAction> _history = new Stack<EditAction>();
        private int _shapeIndex;

        public LevelData Draft { get; private set; }
        public BoardState Board { get; private set; }
        public int Size { get; private set; }
        public EditorTool Tool { get; set; }
        public bool IsLocked { get; private set; }
        public bool IsValidated { get; private set; }

        public BoardShape Shape
        {
            get { return _shapes[_shapeIndex]; }
        }

        public CatDifficulty Difficulty
        {
            get { return Draft.catDifficulty; }
        }

        public bool CanUndo
        {
            get { return _history.Count > 0; }
        }

        public event Action<bool> BoardRebuilt;
        public event Action Changed;

        public LevelEditorSession(IReadOnlyList<BoardShape> shapes, System.Random random)
        {
            if (shapes == null || shapes.Count == 0)
                throw new ArgumentException("The level editor needs at least one board shape.", nameof(shapes));

            _shapes = shapes;
            _random = random;
        }

        public void StartNewDraft()
        {
            _shapeIndex = 0;
            Size = DefaultSize;
            IsLocked = false;
            Tool = EditorTool.Blocks;
            Draft = new LevelData
            {
                levelId = "custom-level-1",
                displayName = "Level 1",
                customLevelNumber = 1,
                hasCatStart = false,
                catDifficulty = CatDifficulty.Normal,
                blockedCells = new List<Vector2Int>(),
                tutorialSteps = new List<TutorialStep>()
            };
            ClearLayout();
            Rebuild(true);
        }

        public void ChangeShape(int direction)
        {
            if (IsLocked)
                return;

            _shapeIndex = (_shapeIndex + direction + _shapes.Count) % _shapes.Count;
            ClearLayout();
            Rebuild(true);
        }

        public void SetSize(int size)
        {
            size = Mathf.Clamp(size, MinSize, MaxSize);
            if (IsLocked || size == Size)
                return;

            Size = size;
            ClearLayout();
            Rebuild(true);
        }

        public void ToggleLock()
        {
            IsLocked = !IsLocked;
            IsValidated = false;
            Changed?.Invoke();
        }

        public void CycleDifficulty()
        {
            int count = Enum.GetValues(typeof(CatDifficulty)).Length;
            Draft.catDifficulty = (CatDifficulty)(((int)Draft.catDifficulty + 1) % count);
            IsValidated = false;
            Changed?.Invoke();
        }

        public bool EditCell(Vector2Int cell)
        {
            if (!IsLocked || !Board.Layout.Contains(cell))
                return false;

            EditAction action = new EditAction
            {
                Cell = cell,
                HadCat = Draft.hasCatStart,
                PreviousCat = Draft.catStart
            };
            if (Tool == EditorTool.Cat)
            {
                if (Board.Layout.IsExit(cell) || _blocks.Contains(cell))
                    return false;

                action.IsCat = true;
                Draft.hasCatStart = true;
                Draft.catStart = cell;
            }
            else
            {
                if (Draft.hasCatStart && Draft.catStart == cell)
                    return false;

                ToggleBlock(cell);
            }

            _history.Push(action);
            Rebuild(false);
            return true;
        }

        public void Undo()
        {
            if (_history.Count == 0)
                return;

            EditAction action = _history.Pop();
            if (action.IsCat)
            {
                Draft.hasCatStart = action.HadCat;
                Draft.catStart = action.PreviousCat;
            }
            else
            {
                ToggleBlock(action.Cell);
            }
            Rebuild(false);
        }

        public void Clear()
        {
            ClearLayout();
            Rebuild(false);
        }

        public LevelValidationResult Validate()
        {
            LevelValidationResult result = LevelValidator.ValidateWinnable(Draft);
            IsValidated = result.IsValid;
            Changed?.Invoke();
            return result;
        }

        public LevelLayoutGenerator BeginGeneration()
        {
            _shapeIndex = _random.Next(_shapes.Count);
            IsLocked = false;
            ClearLayout();
            Rebuild(false);
            IsLocked = true;
            Changed?.Invoke();
            return LevelLayoutGenerator.Create(Draft, Board.Layout, _random);
        }

        public void ApplyGeneratedLayout(LevelLayoutGenerator generator)
        {
            ClearLayout();
            foreach (Vector2Int cell in generator.Blocks)
                _blocks.Add(cell);
            Draft.hasCatStart = true;
            Draft.catStart = generator.CatCell;
            Rebuild(false);
            IsValidated = true;
            Changed?.Invoke();
        }

        private void ToggleBlock(Vector2Int cell)
        {
            if (!_blocks.Add(cell))
                _blocks.Remove(cell);
        }

        private void ClearLayout()
        {
            _blocks.Clear();
            _history.Clear();
            Draft.hasCatStart = false;
        }

        private void Rebuild(bool animate)
        {
            Vector2Int boardSize = Shape.GetBoardSize(Size);
            Draft.boardShape = Shape;
            Draft.width = boardSize.x;
            Draft.height = boardSize.y;
            Draft.blockedCells = new List<Vector2Int>(_blocks);
            Board = BoardState.FromLevel(Draft);
            if (!Board.HasCat)
                Draft.hasCatStart = false;

            IsValidated = false;
            BoardRebuilt?.Invoke(animate);
            Changed?.Invoke();
        }
    }
}
