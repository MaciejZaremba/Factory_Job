using Godot;

public partial class TestRoom : Node
{
	private DungeonGenerator _generator;
	private CharacterBody3D _player;
	public override void _Ready()
	{
		GD.Print("TestRoom: _Ready() entered.");
		_generator = GetNode<DungeonGenerator>("DungeonGenerator");
		_player = GetNode<CharacterBody3D>("CharacterBody3D");
		_generator.FloorGenerated+=OnFloorGenerated;
		_generator.GenerationFailed+=OnGenerationFailed;
		_generator.Generate();
	}
	
	public void OnFloorGenerated(RandomNumberGenerator seed)
	{
		GD.Print($"Floor generated successfully. Seed {seed.Seed}");
		var spawnPos = _generator.GetSpawnPosition();
		_player.GlobalPosition=spawnPos;
	}
	
	public void OnGenerationFailed()
	{
		GD.PrintErr("Floor generation failed - no valid layout found.");
	}
}
