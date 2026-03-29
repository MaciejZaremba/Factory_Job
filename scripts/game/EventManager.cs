using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class EventManager : Node
{
	public static EventManager Instance {get; private set;}
	private RandomNumberGenerator _rand;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("EventManager: Ready() entered.");
		Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void FloorGenerated(RandomNumberGenerator seed)
	{
		_rand = seed;
		GD.Print("EventManager: RNG seeded.");
	}
	
	public void TileEntered()
	{
		GD.Print("EventManager: Tile Entered.");
		GD.Print($"EventManager: Random number: {_rand.RandiRange(0,100)}");
	}
	
}
