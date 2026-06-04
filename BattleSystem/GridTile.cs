using Godot;
using System;

public partial class GridTile : Node3D
{
    public GridPawn Inhabitant { get; set; }
    public Vector2I TilePosition { get; set; }
    public Label3D Label { get; set; }

    public override void _Ready()
    {
        Label = (Label3D)GetNode("Label3D");
    }
}
