using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrapTheCat.Core
{
    [CreateAssetMenu(fileName = "BoardShapeList", menuName = "Trap The Cat/Board Shape List")]
    public sealed class BoardShapeList : ScriptableObject
    {
        [SerializeField] private List<BoardShape> _shapes = new List<BoardShape>();

        public IReadOnlyList<BoardShape> Shapes
        {
            get { return _shapes; }
        }

        public BoardShape FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            foreach (BoardShape shape in _shapes)
            {
                if (shape != null && string.Equals(shape.Id, id, StringComparison.OrdinalIgnoreCase))
                    return shape;
            }
            return null;
        }
    }
}
