
using UnityEngine;

[System.Serializable]
public class Coordinates
{
    [SerializeField]
    public int X
    {
        get; set;
    }

    [SerializeField]
    public int Y
    {
        get; set;
    }

    public Coordinates(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        Coordinates other = (Coordinates)obj;
        return this.X == other.X && this.Y == other.Y;
    }
    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }
}
