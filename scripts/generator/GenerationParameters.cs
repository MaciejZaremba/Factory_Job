using Godot;

[GlobalClass]
public partial class GenerationParameters : Resource
{
	[Export] public int gridWidth {get; set;} = 12;
	[Export] public int gridHeight {get; set;} = 12;
	[Export] public int seed {get; set;} = 0;
	[Export] public int maxRetries {get; set;} = 100;
	[Export] public float emptyTileWeight {get; set;} = 2f;
}
