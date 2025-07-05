using System.Numerics;
public struct Node(Vector2 coordinates, int id)
{
    public int id = id;
    public Vector2 coords = coordinates;
    public float DistTo(Node otherCity)
    {
        return (coords - otherCity.coords).Length();
    }
}
