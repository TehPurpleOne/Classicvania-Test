using Godot;
using System;

public class GameWorld : Node
{
    // Required nodes.
    public TileMap collision;

    public override void _Ready() {
        collision = (TileMap)GetChild(1).GetChild(0);
    }
}
