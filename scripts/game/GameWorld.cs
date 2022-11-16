using Godot;
using System;

public class GameWorld : Node
{
    // Required nodes.
    public TileMap collision;
    public TileMap stairs;
    private Control stairObjs;

    public override void _Ready() {
        collision = (TileMap)GetChild(1).GetChild(0);
        stairs = (TileMap)GetChild(1).GetChild(1);
        stairObjs = (Control)GetChild(1).GetChild(2);
        
        spawnStairs();
    }

    private void spawnStairs() {
        foreach(Vector2 s in stairs.GetUsedCells()) {
            Vector2 pos = stairs.MapToWorld(s);
            PackedScene st = (PackedScene)ResourceLoader.Load("res://scenes/game/objects/StairTrigger.tscn");
            Stairs newSt = (Stairs)st.Instance();
            stairObjs.AddChild(newSt);
            newSt.Position = pos + (stairs.CellSize);
            st.Dispose();
        }

        stairs.Hide();
    }
}
