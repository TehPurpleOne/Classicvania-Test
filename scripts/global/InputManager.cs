using Godot;
using System;

public class InputManager : Node
{
    // This is a simple input manager for the game. It'll record button inputs that are then interpretted by the game's logic.
    
    // Variables.
    public Vector2 dirTap = Vector2.Zero;
    public Vector2 dirHold = Vector2.Zero;
    public bool bTap = false;
    public bool bHold = false;
    public bool aTap = false;
    public bool aHold = false;
    public bool selectTap = false;
    public bool startTap = false;

    public override void _PhysicsProcess(float delta)
    {
        // We run the code on the physics side to prevent slowdown.

        // Directional input are mapped to a vector for both tapping the button and holding.
        dirTap.x = Convert.ToInt32(Input.IsActionJustPressed("right")) - Convert.ToInt32(Input.IsActionJustPressed("left"));
        dirTap.y = Convert.ToInt32(Input.IsActionJustPressed("down")) - Convert.ToInt32(Input.IsActionJustPressed("up"));
        dirHold.x = Convert.ToInt32(Input.IsActionPressed("right")) - Convert.ToInt32(Input.IsActionPressed("left"));
        dirHold.y = Convert.ToInt32(Input.IsActionPressed("down")) - Convert.ToInt32(Input.IsActionPressed("up"));

        // Finally, match the other flags to the appropriate input. It's not necessary to have hold values for Start and Select.
        bTap = Input.IsActionJustPressed("b");
        bHold = Input.IsActionPressed("b");
        aTap = Input.IsActionJustPressed("a");
        aHold = Input.IsActionPressed("a");
        selectTap = Input.IsActionJustPressed("select");
        startTap = Input.IsActionJustPressed("start");
    }

}
