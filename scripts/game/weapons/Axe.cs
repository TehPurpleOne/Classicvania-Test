using Godot;
using System;

public class Axe : SubWeapons
{
    public override void _Ready() {
        w = (GameWorld)GetNode("/root/Master/GameWorld");
        simon = w.simon;
        sprite = (AnimatedSprite)GetChild(0);

        velocity.y = -400;
    }

    public override void _PhysicsProcess(float delta) {
        ticker++;
        ticker = Mathf.Wrap(ticker, 0, 5);

        if(ticker == 0) {
            if(sprite.FlipH) {
                sprite.RotationDegrees += 90;
            } else {
                sprite.RotationDegrees -= 90;
            }
            
        }

        switch(current) {
            case state.INACTIVE:
                if(GlobalPosition != new Vector2(-32, -32)) {
                    Hide();
                    GlobalPosition = new Vector2(-32, -32);
                }
                break;
            case state.THROWN:
                switch(sprite.FlipH) {
                    case true:
                        velocity.x = 120;
                        break;
                    case false:
                        velocity.x = -120;
                        break;
                }

                velocity.y += 25;

                if(GlobalPosition.x < w.cam.GetCameraScreenCenter().x - 136 || GlobalPosition.x > w.cam.GetCameraScreenCenter().x + 136 || GlobalPosition.y < w.cam.GetCameraScreenCenter().y - 128 || GlobalPosition.y > w.cam.GetCameraScreenCenter().y + 128) {
                    velocity.y = -400;
                    setState(state.INACTIVE);
                }
                break;
        }

        velocity = MoveAndSlide(velocity, Vector2.Up);
    }
}
