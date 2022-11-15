using Godot;
using System;

public class Title : Node2D
{
    // Required nodes
    private Master m;
    private InputManager i;
    private Label stair;
    private Label jump;
    private Sprite cursor;

    // Variables
    private float cYPosStart = 0;
    private int menuPos = 0;
    private int lastPos = 0;

    public override void _Ready() {
        // Assign the nodes to the necessary variables.
        m = (Master)GetNode("/root/Master");
        i = (InputManager)GetNode("/root/InputManager");
        stair = (Label)GetChild(2);
        jump = (Label)GetChild(3);
        cursor = (Sprite)GetChild(4);

        // Save the starting Y position of the cursor.
        cYPosStart = cursor.GlobalPosition.y;
    }

    public override void _PhysicsProcess(float delta) {
        // Set the position of the cursor for the options.
        menuPos += (int)i.dirTap.y;
        menuPos = Mathf.Wrap(menuPos, 0, 2);

        // Place the cursor accordingly.
        if(lastPos != menuPos) {
            cursor.GlobalPosition = new Vector2(cursor.GlobalPosition.x, cYPosStart + (menuPos * 8));
            lastPos = menuPos;
        }

        // Adjust the settings.
        if(i.dirTap.x != 0) {
            switch(menuPos) {
                case 0:
                    int a = Convert.ToInt32(m.sAttack);
                    a = a + (int)i.dirTap.x;
                    a = Mathf.Wrap(a, 0, 2);
                    m.sAttack = Convert.ToBoolean(a);

                    switch(m.sAttack) {
                        case false:
                            stair.Text = "CV1";
                            break;
                        case true:
                            stair.Text = "CV3";
                            break;
                    }

                    break;
                case 1:
                    int b = Convert.ToInt32(m.jumpCtrl);
                    b = b + (int)i.dirTap.x;
                    b = Mathf.Wrap(b, 0, 2);
                    m.jumpCtrl = Convert.ToBoolean(b);

                    switch(m.jumpCtrl) {
                        case false:
                            jump.Text = "OFF";
                            break;
                        case true:
                            jump.Text = "ON";
                            break;
                    }
                    break;
            }
        }

        // Swap to the game world.
        if(i.startTap) {
            m.loadWorld();
        }
    }
}
