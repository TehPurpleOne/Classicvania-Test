using Godot;
using System;

public class SubWeapons : KinematicBody2D
{
    // Required nodes.
    public AnimatedSprite sprite;
    public GameWorld w;
    public Player simon;

    // Variables
    public int damage = 0;
    public int ticker = 0;
    public Vector2 velocity = Vector2.Zero;
    public enum state {INACTIVE, THROWN, BOOM}
    public state current = state.INACTIVE;

    public void setState(state which) {
        current = which;
    }

    public void init(Vector2 startPos, bool flip) {
        GlobalPosition = startPos;
        sprite.FlipH = flip;
        Show();
        setState(state.THROWN);
    }
}
