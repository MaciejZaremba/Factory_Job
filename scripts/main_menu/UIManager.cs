using Godot;
using System;

public partial class UIManager : Node
{
	private CanvasLayer _inventory;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && GameState.Instance.startEvent == true)
		{
			if (keyEvent.Keycode == Key.Escape)
			{
				_inventory = GetNode<CanvasLayer>("/root/TestRoom/Inventory");
				_inventory.Visible = !_inventory.Visible;
				Input.MouseMode = _inventory.Visible ? Input.MouseModeEnum.Confined : Input.MouseModeEnum.Captured;
			}
		}
	}
}
