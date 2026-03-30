using Godot;
using Godot.Collections;

public partial class Player : CharacterBody3D
{
	//Base Player Values
	//[Export] declares a runtime attribute - it can be freely changed by outside sources while the program is running, use it often.
	[Export] public float walkSpeed = 3.5f;
	[Export] public float sprintSpeed = 5.5f;
	[Export] public float mouseSensitivity = 0.002f;
	[Export] public Array<string> Inventory = new();
	//Keeps you grounded
	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	
	//How to see
	private Node3D _head;
	
	//Basically a Constructor
	public override void _Ready()
	{
		//How to see part 2
		_head = GetNode<Node3D>("Head");
		
		//Capture the mouse
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
	// What could Input mean, truly a mystery
	public override void _Input(InputEvent @event)
	{
		if(Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			if (@event is InputEventMouseMotion mouseMotion)
			{
				//Rotating the camera, Horizontal rotates the player, Vertical rotates the head
				//RotateY spins AROUND the Y axis (in a 3d space), 
				//Relative.X returns mouse movement on the X axis (in a 2d space)
				RotateY(-mouseMotion.Relative.X * mouseSensitivity);
				_head.RotateX(-mouseMotion.Relative.Y * mouseSensitivity);
				
				//Making sure the player doesn't snap their neck
				Vector3 headRot = _head.Rotation;
				headRot.X = Mathf.Clamp(headRot.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
				_head.Rotation = headRot;
			}
		}
		//Debug tool - release mouse on Esc
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.Escape)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
		}
	}
	// Physics? In my video game?
	public override void _PhysicsProcess(double delta)
	{
		if(GameState.Instance.inEvent) return;
		Vector3 velocity = Velocity;
		// Apply Gravity
		if (!IsOnFloor())
		{
			velocity.Y -= _gravity * (float)delta;
		}
		
		//Determine if the player is sprinting
		float speed = Input.IsActionPressed("sprint") ? sprintSpeed : walkSpeed;
		
		//Order sensitive (learned the hard way)
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		//Movement black magic
		Vector3 moveDir = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		
		//Movement = direction * speed
		if (moveDir != Vector3.Zero)
		{
			velocity.X = moveDir.X * speed;
			velocity.Z = moveDir.Z * speed;
		} else {
			//Smooth stop
			velocity.X = Mathf.MoveToward(velocity.X, 0, speed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, speed);
		}
		Velocity = velocity;
		MoveAndSlide();
	}
}
