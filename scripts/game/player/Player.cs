using Godot;
using System;

public class Player : KinematicBody2D
{
    // Required nodes.
    private InputManager i; // Grabs the input data.
    private Master m; // Used to send data back and forth to global values.
    private GameWorld w; // Used to send/receive data from the Game World.
    private CollisionShape2D box;  // Collision box for the player's feet.
    private Sprite simon; // Player sprite.
    private Sprite whip; // Whip sprite
    private AnimationPlayer anim; 
    private Position2D top; // Block detector - top of sprite
    private Position2D bottom; // Block detector - middle of sprite

    // Variables.
    private const float WALKX = 60; // How many pixels per second do we want Simon to move.
    private const float JUMP = -360; // Starting upward velocity for jumping.
    private const float STAIRXY = 30; // Velocity on stairs.
    //private const float GRAVITY = 20; // Gravity force.
    private const float FALLCAP = 360; // Terminal velocity for the player.
    private int jumpMod = 3; // Jump modifier counter. See yVel();
    private int freeze = 0; // Temporarily freezes the player for certain actions.
    private int iFrames = 120; // Invulnerability frames for the player.
    private int fall = 0; // Counter used to force player to crouch after a long fall.
    private bool wAttack = false; // Flag to show the whip sprite during an attack.
    private int topTileID = -1; // These will check to see if the player is right next to a block, this is done to simulate the NES games and so Simon doesn't bang his head on the ceiling.
    private int lastTopTileID = -1; // ''
    private int bottomTileID = -1; // ''
    private int lastBottomTileID = -1; // ''
    private bool xMove = true; // Stops X movement when a block is detected.
    private Vector2 velocity = Vector2.Zero; // Player's overall speed and angle.
    public enum state {WALKIDLE, CROUCH, WALK, JUMP, FALL, STAIRIDLEUP, STAIRIDLEDOWN, STAIRWALK, ATTACK, KNOCKBACK, DEAD};
    public state last = state.WALKIDLE;

    public override void _Ready() { // Initialize the player scene.
        // Assign nodes to their respective variables.
        m = (Master)GetNode("/root/Master");
        i = (InputManager)GetNode("/root/InputManager");
        w = (GameWorld)GetNode("/root/Master/GameWorld");
        simon = (Sprite)GetChild(1);
        whip = (Sprite)GetChild(2);
        anim = (AnimationPlayer)GetChild(3);
        top = (Position2D)GetChild(5);
        bottom = (Position2D)GetChild(6);
    }

    public override void _PhysicsProcess(float delta) {
        stateLogic();

        velocity = MoveAndSlide(velocity, Vector2.Up);
        GD.Print(velocity);
    }

    private void xVel() {
        velocity.x = (i.dirHold.x * WALKX) * Convert.ToInt32(xMove);
    }

    private void yVel()  {
        velocity.y += 30;

        if(velocity.y > FALLCAP) {
            velocity.y = FALLCAP;
        }
        /* velocity.y += GRAVITY; // Use GRAVITY to pull the player down.

        if(velocity.y > FALLCAP) { // Cap the downward velocity so the player doesn't accidentally fall through platforms.
            velocity.y = FALLCAP;
        } */
    }

    private void stateLogic() {
        switch(last) {
            case state.WALKIDLE:
                xVel(); // Apply X and Y velocity
                yVel();

                if(i.dirHold.x != 0) { // If left or right is pressed, start walking.
                    setState(state.WALK);
                }

                if(i.dirHold.y == 1) { // Crouch if down is pressed.
                    setState(state.CROUCH);
                }

                if(i.aTap && IsOnFloor()) { // Make the player jump/fall.
                    velocity.y = JUMP;
                    setState(state.JUMP);
                }

                /* if(!IsOnFloor()) {
                    setState(state.FALL);
                } */
                break;
            
            case state.WALK:
                xVel(); // Apply X and Y velocity
                yVel();

                if(i.dirHold.x == 0) { // If left or right is pressed, start walking.
                    setState(state.WALKIDLE);
                }

                if(i.dirHold.y == 1) { // Crouch if down is pressed.
                    setState(state.CROUCH);
                }

                if(i.aTap && IsOnFloor()) { // Make the player jump/fall.
                    velocity.y = JUMP;
                    setState(state.JUMP);
                }

                /* if(!IsOnFloor()) {
                    setState(state.FALL);
                } */
                break;
            
            case state.CROUCH:
                yVel(); // Apply Y velocity

                if(i.dirHold.y != 1) { // Crouch if down is pressed.
                    setState(state.WALKIDLE);
                }

                /* if(!IsOnFloor()) { // Make the player fall if no longer on the floor.
                    setState(state.FALL);
                } */
                break;
            
            case state.JUMP:
                if(m.jumpCtrl) { // Allow direction changing if the jump control flag is true.
                    xVel();
                }
                yVel();

                if(velocity.y > 60) {
                    setState(state.FALL);
                }
                break;

            case state.FALL:
                if(m.jumpCtrl) { // Allow direction changing if the jump control flag is true.
                    xVel();
                }
                yVel();

                if(IsOnFloor()) {
                    if(i.dirHold.x != 0) {
                        setState(state.WALK);
                    } else {
                        setState(state.WALKIDLE);
                    }
                }
                break;
        }
    }

    public void setState(state which) {
        switch(which) {
            case state.WALKIDLE:
                anim.Play("idleStand");
                break;
            case state.CROUCH:
                anim.Play("crouch");
                break;
            case state.WALK:
                anim.Play("walk");
                break;
            case state.JUMP:
                anim.Play("jump");
                break;
            case state.FALL:
                anim.Play("idleStand");
                break;
        }

        last = which;
    }
}
