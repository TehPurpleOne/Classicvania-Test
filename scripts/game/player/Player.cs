using Godot;
using System;
using System.Collections.Generic;
using Array = Godot.Collections.Array;

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
    private Area2D sideCheck;
    private CollisionShape2D footbox;

    // Variables.
    private const float WALKX = 60; // How many pixels per second do we want Simon to move.
    private const float JUMP = -300; // Starting upward velocity for jumping.
    private const float STAIRXY = 30; // Velocity on stairs.
    private const float GRAVITY = 25; // Gravity force.
    private const float FALLCAP = 480; // Terminal velocity for the player.
    private float xSpd = 0; // Base x velocity.
    private int jumpMod = 9; // Freeze the player in midair for a moment;
    private int freeze = 0; // Temporarily freezes the player for certain actions.
    private int iFrames = 120; // Invulnerability frames for the player.
    private int fall = 0; // Counter used to force player to crouch after a long fall.
    private bool wAttack = false; // Flag to show the whip sprite during an attack.
    private bool noJump = false; // Checks to see if Simon is under a 2 block high space. If so, he cannot jump.
    private bool xMove = true; // Stops X movement when a block is detected.
    public List<Stairs> stairObj = new List<Stairs>();
    private Stairs SObjA = null;
    private Stairs SObjB = null;
    private Vector2 stairTarget = Vector2.Zero;
    public bool onStairs = false;
    private Vector2 velocity = Vector2.Zero; // Player's overall speed and angle.
    public enum state {WALKIDLE, CROUCH, WALK, JUMP, FALL, FINDSTAIR, STAIRIDLEU, STAIRIDLED, STAIRWALKU, STAIRWALKD, ATTACK, KNOCKBACK, DEAD};
    public state last = state.WALKIDLE;

    public override void _Ready() { // Initialize the player scene.
        // Assign nodes to their respective variables.
        m = (Master)GetNode("/root/Master");
        i = (InputManager)GetNode("/root/InputManager");
        w = (GameWorld)GetNode("/root/Master/GameWorld");
        footbox = (CollisionShape2D)GetChild(0);
        simon = (Sprite)GetChild(1);
        whip = (Sprite)GetChild(2);
        anim = (AnimationPlayer)GetChild(3);
        sideCheck = (Area2D)GetChild(5);
    }

    public override void _PhysicsProcess(float delta) {
        stairCheck();
        stateLogic();

        velocity = MoveAndSlide(velocity, Vector2.Up);
    }

    private void setDir() {
        // Orient the player and wall detector appropriately
        if(i.dirHold.x < 0 && simon.FlipH) {
            sideCheck.Position = new Vector2(-8.5F, sideCheck.Position.y);
            simon.FlipH = false;
        } else if(i.dirHold.x > 0 && !simon.FlipH) {
            sideCheck.Position = new Vector2(8.5F, sideCheck.Position.y);
            simon.FlipH = true;
        }
    }

    private void xVel() {
        switch(last) {
            case state.JUMP:
            case state.FALL:
                if(m.jumpCtrl) {
                    setDir();
                    xSpd = (i.dirHold.x * WALKX);
                }
                break;
            
            case state.WALKIDLE:
            case state.WALK:
                setDir();
                xSpd = (i.dirHold.x * WALKX);
                break;
        }
        
        velocity.x = xSpd * (float)Convert.ToInt32(xMove);
    }

    private void yVel()  {
        velocity.y += 25;

        if(velocity.y > FALLCAP) {
            velocity.y = FALLCAP;
        }
    }

    private void groundCheck() {
        if(!IsOnFloor()) {
            setState(state.WALKIDLE);
            velocity.y = FALLCAP;
        }
    }

    private void stairCheck() {
        if(i.dirHold.y == -1) { // Player pressed up while overlapping stairs.
            for(int u = 0; u < stairObj.Count; u++) {
                float dist = -1;
                // First, grab all stair trigger nodes, this is how we will determine if the staircase is viable or not.
                Array get = GetTree().GetNodesInGroup("StairTrigger");
                for(int ul = 0; ul < get.Count; ul ++) {
                    Stairs s = (Stairs)get[ul];
                    Double degrees = Math.Round((180 /Math.PI) * stairObj[u].GlobalPosition.AngleToPoint(s.GlobalPosition));
                    /* NOTE: Degree positions are as follows:
                    45:     Upper Left
                    -45:    Lower Left
                    135:    Upper Right
                    -135:   Lower Right*/
                    
                    // Sometimes, multiple stair triggers could share the same angle in degrees, to counter this, we pick the cloest one as our target.
                    if(degrees == 45 || degrees == 135) {
                        float d = stairObj[u].GlobalPosition.DistanceTo(s.GlobalPosition); // Get the distance between the stair triggers.
                        if(dist == -1 || d < dist) {
                            dist = d;
                        }
                        SObjA = stairObj[u];
                        SObjB = s;
                        
                        // At this point, the stairs are locked in, proceed to change the player's state.
                        setState(state.FINDSTAIR);
                        onStairs = true;
                    }
                }
            }
        }

        if(i.dirHold.y == 1) { // Player pressed down while overlapping stairs.
            for(int u = 0; u < stairObj.Count; u++) {
                float dist = -1;
                // First, grab all stair trigger nodes, this is how we will determine if the staircase is viable or not.
                Array get = GetTree().GetNodesInGroup("StairTrigger");
                for(int ul = 0; ul < get.Count; ul ++) {
                    Stairs s = (Stairs)get[ul];
                    Double degrees = Math.Round((180 /Math.PI) * stairObj[u].GlobalPosition.AngleToPoint(s.GlobalPosition));
                    /* NOTE: Degree positions are as follows:
                    45:     Upper Left
                    -45:    Lower Left
                    135:    Upper Right
                    -135:   Lower Right*/
                    
                    // Sometimes, multiple stair triggers could share the same angle in degrees, to counter this, we pick the cloest one as our target.
                    if(degrees == -45 || -degrees == 135) {
                        float d = stairObj[u].GlobalPosition.DistanceTo(s.GlobalPosition); // Get the distance between the stair triggers.
                        if(dist == -1 || d < dist) {
                            dist = d;
                        }
                        SObjA = stairObj[u];
                        SObjB = s;
                        
                        // At this point, the stairs are locked in, proceed to change the player's state.
                        setState(state.FINDSTAIR);
                        onStairs = true;
                    }
                }
            }
        }
    }

    private void stateLogic() {
        switch(last) {
            case state.WALKIDLE: // Player isn't moving.
                xVel(); // Apply X and Y velocity
                yVel();
                groundCheck(); // Check to see if the player is on the ground.

                if(i.dirHold.x != 0) { // If left or right is pressed, start walking.
                    setState(state.WALK);
                }

                if(i.dirHold.y == 1) { // Crouch if down is pressed.
                    setState(state.CROUCH);
                }

                if(i.aTap && !noJump) { // Make the player jump/fall.
                    velocity.y = JUMP;
                    setState(state.JUMP);
                }
                break;
            
            case state.WALK: // PLayer is walking
                xVel(); // Apply X and Y velocity
                yVel();
                groundCheck(); // Check to see if the player is on the ground.

                if(i.dirHold.x == 0) { // If left or right is pressed, start walking.
                    setState(state.WALKIDLE);
                }

                if(i.dirHold.y == 1) { // Crouch if down is pressed.
                    setState(state.CROUCH);
                }

                if(i.aTap && !noJump) { // Make the player jump/fall.
                    velocity.y = JUMP;
                    setState(state.JUMP);
                }
                break;
            
            case state.CROUCH: // Player is crouching
                if(velocity.x != 0) {
                    velocity.x = 0;
                }
                yVel(); // Apply Y velocity
                groundCheck(); // Check to see if the player is on the ground.

                if(i.dirHold.y != 1) { // Crouch if down is pressed.
                    setState(state.WALKIDLE);
                }
                break;
            
            case state.JUMP: // Player jumped while on the ground.
                xVel();
                yVel();

                if(velocity.y >= 0) {
                    setState(state.FALL);
                }
                break;

            case state.FALL: // Player's Y velocity is greater than 0.
                xVel();

                if(jumpMod > 0) {
                    jumpMod--;
                }
                
                if(jumpMod == 0) { // Freeze the player in the air for a few frames.
                    yVel();
                }

                if(IsOnFloor()) {
                    if(i.dirHold.x != 0) {
                        setState(state.WALK);
                    } else {
                        setState(state.WALKIDLE);
                    }
                    jumpMod = 9;
                }
                break;
            
            case state.FINDSTAIR:
                footbox.Disabled = true;
                // First, orient the player's sprite appropriately.
                if(SObjA.GlobalPosition.x < GlobalPosition.x) {
                    simon.FlipH = false;
                    velocity.x = -WALKX;
                } else if(SObjA.GlobalPosition.x > GlobalPosition.x) {
                    simon.FlipH = false;
                    velocity.x = WALKX;
                } else { // Reached the center of the object.
                    velocity.x = 0;
                    // Flip the sprite one last time
                    if(SObjB.GlobalPosition.x < GlobalPosition.x) {
                        stairTarget.x = GlobalPosition.x - 8;
                        simon.FlipH = false;
                    } else {
                        stairTarget.x = GlobalPosition.x + 8;
                        simon.FlipH = false;
                    }

                    if(SObjB.GlobalPosition.y > GlobalPosition.y) {
                        stairTarget.y = GlobalPosition.y + 8;
                        setState(state.STAIRWALKD);
                    } else {
                        stairTarget.y = GlobalPosition.y - 8;
                        setState(state.STAIRWALKU);
                    }
                    
                }
                break;
            
            case state.STAIRWALKU:
                if(simon.FlipH) {
                    velocity.x = STAIRXY;
                } else {
                    velocity.x = -STAIRXY;
                }
                velocity.y = -STAIRXY;

                if(GlobalPosition == stairTarget) {
                    velocity = Vector2.Zero;
                    setState(state.STAIRIDLEU);
                }
                break;
            
            case state.STAIRWALKD:
                if(simon.FlipH) {
                    velocity.x = STAIRXY;
                } else {
                    velocity.x = -STAIRXY;
                }
                velocity.y = STAIRXY;

                if(GlobalPosition == stairTarget) {
                    
                    setState(state.STAIRIDLED);
                }
                break;
            
            case state.STAIRIDLEU:

                velocity = Vector2.Zero;
                
                // Handle player input.
                if(i.dirHold.y == -1) {
                    if(simon.FlipH) {
                        stairTarget = GlobalPosition + new Vector2(8, -8);
                    } else {
                        stairTarget = GlobalPosition + new Vector2(-8, -8);
                    }
                    setState(state.STAIRWALKU);
                }

                if(i.dirHold.y == 1) {
                    simon.FlipH = !simon.FlipH;
                    if(simon.FlipH) {
                        stairTarget = GlobalPosition + new Vector2(8, 8);
                    } else {
                        stairTarget = GlobalPosition + new Vector2(-8, 8);
                    }
                    setState(state.STAIRWALKD);
                }
                break;
            
            case state.STAIRIDLED:

                velocity = Vector2.Zero;
                
                // Handle player input.
                if(i.dirHold.y == -1) {
                    simon.FlipH = !simon.FlipH;
                    if(simon.FlipH) {
                        stairTarget = GlobalPosition + new Vector2(8, -8);
                    } else {
                        stairTarget = GlobalPosition + new Vector2(-8, -8);
                    }
                    setState(state.STAIRWALKU);
                }

                if(i.dirHold.y == 1) {
                    if(simon.FlipH) {
                        stairTarget = GlobalPosition + new Vector2(8, 8);
                    } else {
                        stairTarget = GlobalPosition + new Vector2(-8, 8);
                    }
                    setState(state.STAIRWALKD);
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
                anim.Play("fall");
                break;
            case state.FINDSTAIR:
                anim.Play("walk");
                break;
            case state.STAIRIDLEU:
                anim.Play("idleAscend");
                break;
            case state.STAIRIDLED:
                anim.Play("idleDescend");
                break;
            case state.STAIRWALKU:
                anim.Play("ascend");
                break;
            case state.STAIRWALKD:
                anim.Play("descend");
                break;
        }

        last = which;
    }

    private void headEntered(Node node) {
        noJump = true;
    }
    private void headExited(Node node) {
        noJump = false;
    }

    private void bodyEntered(Node node) {
        xMove = false;
    }
    private void bodyExited(Node node) {
        xMove = true;
    }
}
