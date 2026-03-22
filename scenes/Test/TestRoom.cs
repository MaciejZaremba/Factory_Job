using Godot;

public partial class TestRoom : Node
{

	public override void _Ready()
	{
		GD.Print("TestRoom: _Ready() entered.");
		var generator = GetNode<DungeonGenerator>("DungeonGenerator");
		generator.FloorGenerated+=OnFloorGenerated;
		generator.GenerationFailed+=OnGenerationFailed;
		generator.Generate();
	}
	
	public void OnFloorGenerated(int seed)
	{
		GD.Print($"Floor generated successfully. Seed {seed}");
	}
	
	public void OnGenerationFailed()
	{
		GD.PrintErr("Floor generation failed - no valid layout found.");
	}
}
