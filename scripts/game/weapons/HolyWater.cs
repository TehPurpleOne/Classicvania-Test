using Godot;
using System;

public class HolyWater : SubWeapons
{
    private AudioManager a;
    public override void _Ready() {
        a = (AudioManager)GetNode("/root/AudioManager");
        w = (GameWorld)GetNode("/root/Master/GameWorld");
        simon = w.simon;
        sprite = (AnimatedSprite)GetChild(0);

        velocity.y = -120;
    }

    public override void _PhysicsProcess(float delta) {

        switch(current) {
            case state.INACTIVE:
                if(sprite.Animation != "default") {
                    sprite.Play("default");
                }
                
                if(GlobalPosition != new Vector2(-32, -32)) {
                    Hide();
                    GlobalPosition = new Vector2(-32, -32);
                }
                break;
            case state.THROWN:
                switch(sprite.FlipH) {
                    case true:
                        velocity.x = 80;
                        break;
                    case false:
                        velocity.x = -80;
                        break;
                }

                velocity.y += 10;

                if(IsOnFloor()) {
                    sprite.Play("explode");
                    a.playSound("holywater");
                    setState(state.BOOM);
                }

                if(GlobalPosition.x < w.cam.GetCameraScreenCenter().x - 136 || GlobalPosition.x > w.cam.GetCameraScreenCenter().x + 136 || GlobalPosition.y < w.cam.GetCameraScreenCenter().y - 128 || GlobalPosition.y > w.cam.GetCameraScreenCenter().y + 128) {
                    velocity.y = -120;
                    setState(state.INACTIVE);
                }
                break;
            case state.BOOM:
                if(velocity != Vector2.Zero) {
                    velocity = Vector2.Zero;
                }
                break;
        }

        velocity = MoveAndSlide(velocity, Vector2.Up);
    }

    private void animDone() {
        if(current == state.BOOM) {
            velocity.y = -120;
            setState(state.INACTIVE);
        }
    }
}
