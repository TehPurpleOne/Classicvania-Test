using Godot;
using System;

public class HUD : Node2D
{
    // Required nodes.
    private Master m;
    private string timeText = "";
    private TextureProgress pLife;
    private TextureProgress bLife;
    private string stage = "";
    private string hearts = "";
    private string lives = "";
    private Sprite wpn;
    private Sprite DblTpl;

    // Variables.
    private int hpDelay = 0;

    public override void _Ready() {
        m = (Master)GetNode("/root/Master");
        pLife = (TextureProgress)GetChild(2);
        bLife = (TextureProgress)GetChild(3);
        wpn = (Sprite)GetChild(10);
        DblTpl = (Sprite)GetChild(11);
    }

    public override void _Process(float delta) {
        wpn.Frame = m.subID;
        DblTpl.Frame = m.DTShot - 1;
    }
}
