using Godot;
using System;

public partial class TileEnd : Node3D
{
	private bool _hasTriggered;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_hasTriggered = false;
	}
	
	public void OnEndEntered(Node3D body)
	{
		if(body.IsInGroup("playerGroup") && _hasTriggered == false) 
		{
			GD.Print("Tile: Player Entered Tile.");
			_hasTriggered = true;
			EventManager.Instance.TileEntered(GetParent().GetParent().GetNode<CanvasLayer>("EventPopup"), "end");
		}
	}
}
