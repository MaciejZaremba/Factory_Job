using Godot;

public partial class GameState : Node
{
	public static GameState Instance {get; private set;}
	[Export] public bool inEvent;
	[Export] public bool startEvent;
	
	public override void _Ready()
	{
		Instance = this;
		inEvent = false;
		startEvent = false;
	}
}
