using Godot;
using System;

public class Cross : SubWeapons
{
    private int speed = 120;
    public override void _Ready() {
        w = (GameWorld)GetNode("/root/Master/GameWorld");
        simon = w.simon;
        sprite = (AnimatedSprite)GetChild(0);
    }

    public override void _PhysicsProcess(float delta) {
        
        switch(current) {
            case state.INACTIVE:
                if(GlobalPosition != new Vector2(-32, -32)) {
                    Hide();
                    GlobalPosition = new Vector2(-32, -32);
                }
                break;
            case state.THROWN:
                ticker++;
                if(ticker == 56) {
                    sprite.FlipH = !sprite.FlipH;
                }

                switch(sprite.FlipH) {
                    case true:
                        velocity.x = speed;
                        break;
                    case false:
                        velocity.x = -speed;
                        break;
                }

                if(GlobalPosition.x < w.cam.GetCameraScreenCenter().x - 136 || GlobalPosition.x > w.cam.GetCameraScreenCenter().x + 136 || GlobalPosition.y < w.cam.GetCameraScreenCenter().y - 128 || GlobalPosition.y > w.cam.GetCameraScreenCenter().y + 128) {
                    ticker = 0;
                    setState(state.INACTIVE);
                }
                break;
        }

        velocity = MoveAndSlide(velocity, Vector2.Up);
    }
}
