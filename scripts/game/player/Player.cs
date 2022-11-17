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
    private bool whpThrw = false; // True is using a whip, false if using a thrown item.
    public List<Stairs> stairObj = new List<Stairs>();
    private Stairs SObjA = null;
    private Stairs SObjB = null;
    private Vector2 stairTarget = Vector2.Zero;
    public bool onStairs = false;
    private Vector2 velocity = Vector2.Zero; // Player's overall speed and angle.
    public enum state {WALKIDLE, CROUCH, WALK, JUMP, FALL, WALKOFF, FINDSTAIR, STAIRS, WHIP, THROW, STUN, KNOCKBACK, DEAD};
    public state current = state.WALKIDLE;
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

        switch(current) { // Check to see if the play has activated a stair object
            case state.WALKIDLE:
            case state.WALK:
                stairCheck();
                break;
        }

        switch(current) { // Check to see if the player walked off an edge or lost the ground they were standing on.
            case state.WALKIDLE:
            case state.WALK:
            case state.CROUCH:
                groundCheck();
                break;
        }

        switch(current) { // Tick down the freeze timer if active.
            case state.CROUCH:
            case state.STUN:
                if(freeze > 0) {
                    freeze --;
                }
                break;
        }

        switch(current) { // Check to see if the player can attack.

        }

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
        switch(current) {
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
            fall++;
            velocity.y = FALLCAP;
        }
    }

    private void groundCheck() {
        if(!IsOnFloor() && fall == 0) {
            setState(state.WALKOFF);
            velocity.y = FALLCAP;
        }
    }

    private void attackCheck() {
        if(i.dirHold.y != -1 && i.bTap) { // Activate the whip.
            setState(state.WHIP);
        }

        if(i.dirHold.y != -1 && i.bTap) { // Throw an item.
            if(m.subID != 0) {
                setState(state.THROW);
            }
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
                    if(degrees == -45 || degrees == -135) {
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
        switch(current) {
            case state.WALKIDLE: // Player isn't moving.
                xVel(); // Apply X and Y velocity
                yVel();

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

                if(i.dirHold.y != 1 && freeze == 0) { // Crouch if down is pressed.
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
            
            case state.WALKOFF:
                yVel();
                if(IsOnFloor()) {
                    if(fall < 7) {
                        if(i.dirHold.x != 0) {
                            setState(state.WALK);
                        } else {
                            setState(state.WALKIDLE);
                        }
                    } else {
                        freeze = 30;
                        setState(state.CROUCH);
                    }
                    fall = 0;
                    jumpMod = 9;
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
                    if(fall < 7) {
                        if(i.dirHold.x != 0) {
                            setState(state.WALK);
                        } else {
                            setState(state.WALKIDLE);
                        }
                    } else {
                        freeze = 30;
                        setState(state.CROUCH);
                    }
                    fall = 0;
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
                    simon.FlipH = true;
                    velocity.x = WALKX;
                } else { // Reached the center of the object.
                    velocity.x = 0;
                    // Flip the sprite one current time
                    if(SObjB.GlobalPosition.x < GlobalPosition.x) {
                        stairTarget.x = GlobalPosition.x - 8;
                        simon.FlipH = false;
                    } else {
                        stairTarget.x = GlobalPosition.x + 8;
                        simon.FlipH = false;
                    }

                    if(SObjB.GlobalPosition.y > GlobalPosition.y) {
                        stairTarget.y = GlobalPosition.y + 8;
                    } else {
                        stairTarget.y = GlobalPosition.y - 8;
                    }

                    setState(state.STAIRS);
                    
                }
                break;
            
            case state.STAIRS:
                Vector2 move = Vector2.Zero;
                bool up = false;
                bool down = false;

                // Check to see if the player has reached the target vector.
                if(GlobalPosition == stairTarget) {
                    //setState(state.STAIRIDLE);
                    // Set the appropriate idle animation.
                    if(anim.CurrentAnimation == "ascend") {
                        anim.Play("idleAscend");
                    } else if(anim.CurrentAnimation == "descend") {
                        anim.Play("idleDescend");
                    }

                    // Check to see if the player has reached the next stair trigger object.
                    if(GlobalPosition.x == SObjA.GlobalPosition.x || GlobalPosition.x == SObjB.GlobalPosition.x) { // Resume normal movement and clear stair objects.
                        if(i.dirHold.x != 0) {
                            setState(state.WALK);
                        } else {
                            setState(state.WALKIDLE);
                        }
                        footbox.Disabled = false;
                        SObjA = null;
                        SObjB = null;
                    } else {
                        // My attempt to simplify the controls.
                        if(i.dirHold.y == -1) {
                            up = true;
                        }
                        if(i.dirHold.y == 1) {
                            down = true;
                        }
                        if(i.dirHold.x == -1) {
                            // Determine which stair trigger has a lower X coordinate.
                            if(SObjA.GlobalPosition.x < SObjB.GlobalPosition.x) {
                                // Determine which object has a lower Y coordinate.
                                if(SObjA.GlobalPosition.y < SObjB.GlobalPosition.y) {
                                    up = true;
                                } else {
                                    down = true;
                                }
                            } else {
                                if(SObjB.GlobalPosition.y < SObjA.GlobalPosition.y) {
                                    up = true;
                                } else {
                                    down = true;
                                }
                            }
                        }
                        if(i.dirHold.x == 1) {
                            // Determine which stair trigger has a higher X coordinate.
                            if(SObjA.GlobalPosition.x > SObjB.GlobalPosition.x) {
                                // Determine which object has a lower Y coordinate.
                                if(SObjA.GlobalPosition.y < SObjB.GlobalPosition.y) {
                                    up = true;
                                } else {
                                    down = true;
                                }
                            } else {
                                if(SObjB.GlobalPosition.y < SObjA.GlobalPosition.y) {
                                    up = true;
                                } else {
                                    down = true;
                                }
                            }
                        }
                        // Set target vector.
                        if(up) { // Player pressed up.
                            stairTarget.y = GlobalPosition.y - 8;

                            // Determine which stair trigger is higher, then compare that object's X position to the player.
                            if(SObjA.GlobalPosition.y < SObjB.GlobalPosition.y) {
                                if(SObjA.GlobalPosition.x < GlobalPosition.x) {
                                    stairTarget.x = GlobalPosition.x - 8;
                                }
                                if(SObjA.GlobalPosition.x > GlobalPosition.x) {
                                    stairTarget.x = GlobalPosition.x + 8;
                                }
                            } else {
                                if(SObjB.GlobalPosition.x < GlobalPosition.x) {
                                    stairTarget.x = GlobalPosition.x - 8;
                                }
                                if(SObjB.GlobalPosition.x > GlobalPosition.x) {
                                    stairTarget.x = GlobalPosition.x + 8;
                                }
                            }
                        }

                        if(down) { // Player pressed Down.
                            stairTarget.y = GlobalPosition.y + 8;

                            // Determine which stair trigger is higher, then compare that object's X position to the player.
                            if(SObjA.GlobalPosition.y > SObjB.GlobalPosition.y) {
                                if(SObjA.GlobalPosition.x < GlobalPosition.x) {
                                    stairTarget.x = GlobalPosition.x - 8;
                                }
                                if(SObjA.GlobalPosition.x > GlobalPosition.x) {
                                    stairTarget.x = GlobalPosition.x + 8;
                                }
                            } else {
                                if(SObjB.GlobalPosition.x < GlobalPosition.x) {
                                    stairTarget.x = GlobalPosition.x - 8;
                                }
                                if(SObjB.GlobalPosition.x > GlobalPosition.x) {
                                    stairTarget.x = GlobalPosition.x + 8;
                                }
                            }
                        }
                    }
                } else {
                    // Orient the sprite in the correct direction and set the animation.
                    if(stairTarget.x < GlobalPosition.x) {
                        simon.FlipH = false;
                    } else if(stairTarget.x > GlobalPosition.x) {
                        simon.FlipH = true;
                    }

                    // Assign the appropriate animation based on the target.
                    if(stairTarget.y < GlobalPosition.y && anim.CurrentAnimation != "descend") {
                        anim.Play("ascend");
                    } else if(stairTarget.y > GlobalPosition.y && anim.CurrentAnimation != "ascend") {
                        anim.Play("descend");
                    }

                    // Move the player.
                    if(stairTarget.x < GlobalPosition.x) {
                        move.x = -0.5F;
                    } else {
                        move.x = 0.5F;
                    }

                    if(stairTarget.y < GlobalPosition.y) {
                        move.y = -0.5F;
                    } else {
                        move.y = 0.5F;
                    }

                    GlobalPosition += move;
                }
                break;
            
            case state.WHIP:
                switch(last) {
                    case state.WALKIDLE:
                    case state.CROUCH:
                    case state.WALK:
                    case state.JUMP:
                    case state.FALL:
                        yVel();
                        break;
                }
                break;
        }
    }

    public void setState(state which) {
        switch(which) {
            case state.WALKIDLE:
            case state.WALKOFF:
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
            // Stair animations are handed in State Logic under Stairs.
            case state.WHIP:
                fall = 0;
                whpThrw = true;
                switch(current) {
                    case state.WALKIDLE:
                    case state.WALK:
                        velocity.x = 0;
                        anim.Play("whipStand");
                        break;
                    case state.JUMP:
                    case state.FALL:
                        anim.Play("whipStand");
                        break;
                    case state.CROUCH:
                        anim.Play("whipCrouch");
                        break;
                    case state.STAIRS:
                        switch(anim.CurrentAnimation) {
                            case "ascend":
                            case "idleAscend":
                                anim.Play("whipAscend");
                                break;
                            case "descend":
                            case "idleDescend":
                                anim.Play("whipDescend");
                                break;
                        }
                        break;
                }
                break;
            case state.THROW:
                fall = 0;
                switch(current) {
                    case state.WALKIDLE:
                    case state.WALK:
                        velocity.x = 0;
                        anim.Play("whipStand");
                        break;
                    case state.JUMP:
                    case state.FALL:
                        anim.Play("whipStand");
                        break;
                    case state.CROUCH:
                        anim.Play("whipCrouch");
                        break;
                    case state.STAIRS:
                        switch(anim.CurrentAnimation) {
                            case "ascend":
                            case "idleAscend":
                                anim.Play("whipAscend");
                                break;
                            case "descend":
                            case "idleDescend":
                                anim.Play("whipDescend");
                                break;
                        }
                        break;
                }
                break;
        }
        last = current;
        current = which;
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

    private void animDone(string which) {
        switch(which) {
            case "whipStand":
            case "whipCrouch":
            case "whipAscend":
            case "whipDescend":
                setState(last);
                break;
        }
    }
}
