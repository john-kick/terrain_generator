using System.ComponentModel.DataAnnotations;
using Godot;

public struct Tile
{
    Vector2 _position;

    public Vector2 Position
    {
        get => _position;
        set {
            _position = value;
            MeshInstance.Position = new Vector3(Position.X, 0, Position.Y);
        }
    }
    public MeshInstance3D MeshInstance;
}