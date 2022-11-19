using Godot;
using System;
using System.Collections.Generic;
using Array = Godot.Collections.Array;

public class GameWorld : Node
{
    // Required nodes.
    private Master m;
    public TileMap collision;
    public TileMap stairs;
    private Control stairObjs;
    public Player simon;
    public Camera2D cam;

    // Variables.
    private List<SubWeapons> subs = new List<SubWeapons>();
    public int swCount = 0;

    public override void _Ready() {
        m = (Master)GetNode("/root/Master");
        collision = (TileMap)GetChild(1).GetChild(0);
        stairs = (TileMap)GetChild(1).GetChild(1);
        stairObjs = (Control)GetChild(1).GetChild(2);
        simon = (Player)GetChild(3).GetChild(1);
        cam = (Camera2D)simon.GetChild(8);

        for(int x = 0; x< GetTree().GetNodesInGroup("subWpn").Count; x++) {
            subs.Add((SubWeapons)GetTree().GetNodesInGroup("subWpn")[x]);
        }
        
        // Spawn stair objects
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
    public override void _PhysicsProcess(float delta) {
        countSWs();
    }

    private void countSWs() {
        int test = 0;
        for(int i = 0; i < subs.Count; i++) {
            switch(m.subID) {
                case 1:
                    if(subs[i].Name.Contains("Dagger") && subs[i].current != SubWeapons.state.INACTIVE) {
                        test++;
                    }
                    break;
                case 2:
                    if(subs[i].Name.Contains("Axe") && subs[i].current != SubWeapons.state.INACTIVE) {
                        test++;
                    }
                    break;
                case 3:
                    if(subs[i].Name.Contains("HolyWater") && subs[i].current != SubWeapons.state.INACTIVE) {
                        test++;
                    }
                    break;
                case 4:
                    if(subs[i].Name.Contains("Cross") && subs[i].current != SubWeapons.state.INACTIVE) {
                        test++;
                    }
                    break;
            }
        }
        if(test != swCount) {
            swCount = test;
            GD.Print(swCount);
        }
    }

    public void getWpn(Vector2 startPos, bool flip) {
        for(int i = 0; i < subs.Count; i++) {
            switch(m.subID) {
                case 1:
                    if(subs[i].Name.Contains("Dagger") && subs[i].current == SubWeapons.state.INACTIVE) {
                        subs[i].init(startPos, flip);
                        return;
                    }
                    break;
                case 2:
                    if(subs[i].Name.Contains("Axe") && subs[i].current == SubWeapons.state.INACTIVE) {
                        subs[i].init(startPos, flip);
                        return;
                    }
                    break;
                case 3:
                    if(subs[i].Name.Contains("HolyWater") && subs[i].current == SubWeapons.state.INACTIVE) {
                        subs[i].init(startPos, flip);
                        return;
                    }
                    break;
                case 4:
                    if(subs[i].Name.Contains("Cross") && subs[i].current == SubWeapons.state.INACTIVE) {
                        subs[i].init(startPos, flip);
                        return;
                    }
                    break;
            }
        }
    }
}
