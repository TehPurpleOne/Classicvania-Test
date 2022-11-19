using Godot;
using System;
using System.Collections.Generic;
using Array = Godot.Collections.Array;

public class Player : KinematicBody2D
{
    // Required nodes.
    private InputManager i; // Grabs the input data.
    private AudioManager a;
    private Master m; // Used to send data back and forth to global values.
    private GameWorld w; // Used to send/receive data from the Game World.
    private CollisionShape2D box;  // Collision box for the player's feet.
    private Sprite simon; // Player sprite.
    private Sprite whip; // Whip sprite
    private AnimationPlayer anim;
    private Area2D sideCheck;
    private CollisionShape2D footbox;
    private CollisionShape2D whipBox;

    // Variables.
    private const float WALKX = 60; // How many pixels per second do we want Simon to move.
    private const float JUMP = -300; // Starting upward velocity for jumping.
    private const float STAIRXY = 30; // Velocity on stairs.
    private const float GRAVITY = 25; // Gravity force.
    private const float FALLCAP = 480; // Terminal velocity for the player.
    private float xSpd = 0; // Base x velocity.
    private int jumpMod = 9; // Freeze the player in midair for a moment;
    private int freeze = 0; // Temporarily freezes the player for certain actions.
    private int iFrames = 90; // Invulnerability frames for the player.
    private int fall = 0; // Counter used to force player to crouch after a long fall.
    private bool noJump = false; // Checks to see if Simon is under a 2 block high space. If so, he cannot jump.
    private bool xMove = true; // Stops X movement when a block is detected.
    private bool whpThrw = false; // True is using a whip, false if using a thrown item.
    private int lastWhip = 0;
    private string currAnim = "";
    private string lastAnim = "";
    public List<Stairs> stairObj = new List<Stairs>();
    private Stairs SObjA = null;
    private Stairs SObjB = null;
    private Vector2 stairTarget = Vector2.Zero;
    private Vector2 velocity = Vector2.Zero; // Player's overall speed and angle.
    public enum state {WALKIDLE, CROUCH, WALK, JUMP, FALL, WALKOFF, FINDSTAIR, STAIRS, WHIP, THROW, STUN, KNOCKBACK, DEAD};
    public state current = state.WALKIDLE;
    public state last = state.WALKIDLE;

    public override void _Ready() { // Initialize the player scene.
        // Assign nodes to their respective variables.
        m = (Master)GetNode("/root/Master");
        i = (InputManager)GetNode("/root/InputManager");
        a = (AudioManager)GetNode("/root/AudioManager");
        w = (GameWorld)GetNode("/root/Master/GameWorld");
        footbox = (CollisionShape2D)GetChild(0);
        whipBox = (CollisionShape2D)GetChild(7).GetChild(m.whipLevel);
        simon = (Sprite)GetChild(1);
        whip = (Sprite)GetChild(2);
        anim = (AnimationPlayer)GetChild(3);
        sideCheck = (Area2D)GetChild(5);
    }

    public override void _PhysicsProcess(float delta) {

        if(lastWhip != m.whipLevel) { // Set the correct hitbox based on which whip is equipped.
            whipBox = (CollisionShape2D)GetChild(7).GetChild(m.whipLevel);
            lastWhip = m.whipLevel;
        }

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
            case state.WHIP:
            case state.THROW:
                if(freeze > 0) {
                    freeze --;
                }
                break;
        }

        switch(current) { // Check to see if the player can attack.
            case state.WALKIDLE:
            case state.CROUCH:
            case state.WALK:
            case state.JUMP:
            case state.FALL:
            case state.WALKOFF:
            case state.STAIRS:
                if(m.sAttack && currAnim == "idleAscend" || m.sAttack && currAnim == "idleDescend") {
                    attackCheck();
                } else {
                    attackCheck();
                }
                break;
        }

        switch(current) { // Apply gravity
            case state.WALKIDLE:
            case state.CROUCH:
            case state.WALK:
            case state.JUMP:
            case state.FALL:
            case state.WALKOFF:
            case state.WHIP:
            case state.THROW:
            case state.KNOCKBACK:
                if(last != state.STAIRS) {
                    yVel();
                }
                break;
        }

        switch(current) { // Apply left/right movement
            case state.WALKIDLE:
            case state.WALK:
            case state.JUMP:
            case state.FALL:
                xVel();
                break;

            case state.WHIP:
            case state.THROW:
                if(last != state.STAIRS) {
                    xVel();
                }
                break;
        }

        if(anim.CurrentAnimation != currAnim) { // Failsafe to prevent the player from getting stuck after whipping on stairs.
            lastAnim = currAnim;
            currAnim = anim.CurrentAnimation;
        }

        iFrame();

        stateLogic();

        whipAnim();
        throwWpn();

        velocity = MoveAndSlide(velocity, Vector2.Up);
        if(current == state.WALK) {
            GlobalPosition = new Vector2((float)Math.Round(GlobalPosition.x), (float)Math.Round(GlobalPosition.y)); // Hopefully this will prevent any issues with positioning on stairs.
        }
        GlobalPosition = new Vector2(Mathf.Clamp(GlobalPosition.x, w.cam.LimitLeft + 8, w.cam.LimitRight - 8), GlobalPosition.y);
        

        // This is just for testing.
        if(Input.IsActionJustPressed("hurt") && iFrames == 90) {
            a.playSound("hurt");
            m.playerhp -= 4;
            setState(state.STUN);
        }
    }

    private void setDir() {
        // Orient the player and wall detector appropriately
        if(i.dirHold.x < 0 && simon.FlipH) {
            sideCheck.Position = new Vector2(-sideCheck.Position.x, sideCheck.Position.y);
            for(int w = 0; w < 3; w++) {
                CollisionShape2D wBoxes = (CollisionShape2D)GetChild(7).GetChild(w);
                wBoxes.Position = new Vector2(-wBoxes.Position.x, wBoxes.Position.y);
            }
            simon.FlipH = false;
        } else if(i.dirHold.x > 0 && !simon.FlipH) {
            sideCheck.Position = new Vector2(-sideCheck.Position.x, sideCheck.Position.y);
            for(int w = 0; w < 3; w++) {
                CollisionShape2D wBoxes = (CollisionShape2D)GetChild(7).GetChild(w);
                wBoxes.Position = new Vector2(-wBoxes.Position.x, wBoxes.Position.y);
            }
            simon.FlipH = true;
        }
        whip.FlipH = simon.FlipH;
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
            
            case state.WHIP:
            case state.THROW:
                if(m.jumpCtrl && !IsOnFloor()) {
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
        if(velocity.y < 0) {
            jumpMod = 9;
            velocity.y += GRAVITY;
        }

        if(velocity.y == 0 && jumpMod > 0) {
            jumpMod--;
        }

        if(velocity.y >= 0 && jumpMod == 0) {
            velocity.y += GRAVITY;
        }

        if(velocity.y > FALLCAP) {
            fall++;
            velocity.y = FALLCAP;
        }
    }

    private void groundCheck() {
        if(!IsOnFloor()) {
            setState(state.WALKOFF);
            velocity.y = FALLCAP;
        }
    }

    private void attackCheck() {

        if(i.bTap && !m.sAttack || i.bTap && m.sAttack && currAnim != "idleAscend" && currAnim == "idleDescend" && current != state.STAIRS || i.bHold && m.sAttack && currAnim == "idleAscend" || i.bHold && m.sAttack && currAnim == "idleDescend") {
            if(i.dirHold.y == -1 && m.subID != 0 && w.swCount < m.DTShot) {
                setState(state.THROW);
            } else {
                a.playSound("whip");
                setState(state.WHIP);
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
                    }
                }
            }
        }
    }

    private void iFrame() {
        if(current != state.KNOCKBACK && iFrames < 90) {
            iFrames ++;
            if(iFrames%2 == 0) {
                simon.Visible = !simon.Visible;
            }
        }

        if(iFrames == 90 && !simon.Visible) {
            simon.Show();
        }
    }

    private void throwWpn() {
        if(anim.CurrentAnimationPosition >= 0.16 && anim.CurrentAnimationPosition < 0.176 && whipBox.Disabled && !whpThrw && current == state.THROW) {
            w.getWpn(GlobalPosition + (Vector2.Up * 8), simon.FlipH);
        }
    }

    private void whipAnim() {
        if(whpThrw && current == state.WHIP) { // Animate the show the whip.

            switch(currAnim){
                case "whipStand":
                case "whipCrouch":
                case "whipAscend":
                case "whipDescend":
                    if(simon.Frame >= 13) {
                        whip.Frame = ((simon.Frame - 13) * 3) + m.whipLevel;
                    }

                    if(!whip.Visible && whip.Frame == ((simon.Frame - 13) * 3) + m.whipLevel) {
                        whip.Show();
                    }

                    if(anim.CurrentAnimationPosition >= 0.256 && anim.CurrentAnimationPosition < 0.272 && whipBox.Disabled) {
                        whipBox.Disabled = false;
                    } else {
                        whipBox.Disabled = true;
                    }
                    break;
            }  
        }

        if(!whpThrw && whip.Visible) { // Failsafe to prevent the whip from getting stuck on screen or the infamous Critical Hit bug from the original NES game.
            whipBox.Disabled = true;
            whip.Hide();
        }
    }

    private void stateLogic() {
        switch(current) {
            case state.WALKIDLE: // Player isn't moving.

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

                if(i.dirHold.y != 1 && freeze == 0) { // Crouch if down is pressed.
                    setState(state.WALKIDLE);
                }
                break;
            
            case state.JUMP: // Player jumped while on the ground.

                if(velocity.y >= 0) {
                    setState(state.FALL);
                }
                break;
            
            case state.WALKOFF:
            case state.FALL:
                if(IsOnFloor()) {
                    if(fall < 3) {
                        if(i.dirHold.x != 0) {
                            setState(state.WALK);
                        } else {
                            setState(state.WALKIDLE);
                        }
                    } else {
                        freeze = 30;
                        a.playSound("footstep");
                        setState(state.CROUCH);
                    }
                    fall = 0;
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
                        for(int w = 0; w < 3; w++) {
                            CollisionShape2D wBoxes = (CollisionShape2D)GetChild(7).GetChild(w);
                            float x = Math.Abs(wBoxes.Position.x);
                            wBoxes.Position = new Vector2(-x, wBoxes.Position.y);
                        }
                        simon.FlipH = false;
                    } else {
                        stairTarget.x = GlobalPosition.x + 8;
                        for(int w = 0; w < 3; w++) {
                            CollisionShape2D wBoxes = (CollisionShape2D)GetChild(7).GetChild(w);
                            float x = Math.Abs(wBoxes.Position.x);
                            wBoxes.Position = new Vector2(x, wBoxes.Position.y);
                        }
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
                        if(!m.sAttack || m.sAttack && !i.bHold) {
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
                    if(stairTarget.x < GlobalPosition.x && simon.FlipH) {
                        sideCheck.Position = new Vector2(-8.5F, sideCheck.Position.y);
                        for(int w = 0; w < 3; w++) {
                            CollisionShape2D wBoxes = (CollisionShape2D)GetChild(7).GetChild(w);
                            float x = Math.Abs(wBoxes.Position.x);
                            wBoxes.Position = new Vector2(-x, wBoxes.Position.y);
                        }
                        simon.FlipH = false;
                    } else if(stairTarget.x > GlobalPosition.x && !simon.FlipH) {
                        sideCheck.Position = new Vector2(8.5F, sideCheck.Position.y);
                        for(int w = 0; w < 3; w++) {
                            CollisionShape2D wBoxes = (CollisionShape2D)GetChild(7).GetChild(w);
                            float x = Math.Abs(wBoxes.Position.x);
                            wBoxes.Position = new Vector2(x, wBoxes.Position.y);
                        }
                        simon.FlipH = true;
                    }

                    whip.FlipH = simon.FlipH;

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
            case state.THROW:
                if(IsOnFloor()) { // Prevent from being put in a crouch state immediately following the whip state.
                    fall = 0;
                    velocity.x = 0;
                }
                break;
            
            case state.STUN:
                if(velocity != Vector2.Zero) {
                    velocity = Vector2.Zero;
                }
                if(freeze == 0 && last != state.STAIRS || freeze == 0 && last == state.STAIRS && m.playerhp == 0) {
                    velocity.y = JUMP * 0.75F;
                    setState(state.KNOCKBACK);
                }
                if(freeze == 0 && last == state.STAIRS && m.playerhp > 0) { // We don't want players flying off the stairs when hit. Not good for gameplay.
                    iFrames = 0;
                    setState(last);
                }
                break;

            case state.KNOCKBACK:
                switch(simon.FlipH) {
                    case true:
                        velocity.x = -WALKX;
                        break;
                    case false:
                        velocity.x = WALKX;
                        break;
                }
                if(IsOnFloor() && m.playerhp > 0) {
                    freeze = 30;
                    iFrames = 0;
                    setState(state.CROUCH);
                } else if(IsOnFloor() && m.playerhp <= 0) {
                    a.playMusic("dead");
                    setState(state.DEAD);
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
            case state.STUN:
                velocity = Vector2.Zero;
                whpThrw = false;
                freeze = 5;
                break;
            case state.KNOCKBACK:
                footbox.Disabled = false;
                fall = 0;
                anim.Play("hurt");
                break;
            case state.DEAD:
                velocity = Vector2.Zero;
                anim.Play("dead");
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
                whpThrw = false;
                whip.Hide();
                anim.Play(lastAnim);
                setState(last);
                break;
        }
    }
}
