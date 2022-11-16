using Godot;
using System;

public class Stairs : Node2D
{
    // Required nodes.
    private Area2D box;

    public override void _Ready() {
        box = (Area2D)GetChild(1);
    }

    private void plyrEnter(Node node) {
        Player p = (Player)node;

        p.stairObj.Add(this);
    }

    private void plyrExit(Node node) {
        Player p = (Player)node;

        p.stairObj.Remove(this);
    }
}
