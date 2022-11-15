using Godot;
using System;
using System.Collections.Generic;

public class Master : Node
{
    // Normally I would prefer a separate global variable script, but for this example, these values can just be stored in Master.
    public int playerhp = 16; // Player's current HP
    public int lives = 3; // Remaining lives.
    public int subID = 0; // Sub-weapon ID.
    public int hearts = 0; // Remaining hearts.
    public int score = 0; // Current score.
    public int stageID = 0; // Stage ID.
    public readonly List<int> stagetime = new List<int>(); // Stage time storage.

    // Game options
    public bool sAttack = false; // Choose between Castlevania 1/2's way of attacking on stairs or CV 3's.
    public bool jumpCtrl = false; // Allow the player to change direction in midair.

    // Game Scenes
    private PackedScene world = (PackedScene)ResourceLoader.Load("res://scenes/game/GameWorld.tscn");

    public void loadWorld() {
        // This is a simple scene switch between the title screen and the game world. I highly recommend not using this method in production.
        Title title = (Title)GetChild(0);
        title.QueueFree();

        var newWorld = (GameWorld)world.Instance();
        AddChild(newWorld);
    }

}