using Godot;
using System.Collections.Generic;

public partial class DungeonGenerator : Node
{
	[Export] public GenerationParameters Parameters {get; set;}
	// creats a Node3D object that holds the tiles during generation
	[Export] public NodePath FloorContainerPath {get; set;}
	//emits when generation succeeds - used to extract the seed
	[Signal] public delegate void FloorGeneratedEventHandler(int seed);
	//emits when generation fails
	[Signal] public delegate void GenerationFailedEventHandler();
	
	private Node3D _floorContainer;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("DungeonGenerator: _Ready() entered.");
		_floorContainer = GetNode<Node3D>(FloorContainerPath);
		if (_floorContainer == null) GD.PrintErr("DungeonGenerator: FloorContainer not found!");
	}
	
	//the actual generator
	public void Generate()
	{
		
		GD.Print("DungeonGenerator: Generate() called.");
		if (Parameters == null)
		{
			GD.PrintErr("DungeonGenerator: No GenerationParameters assigned.");
		}
		
		ApplyEmptyTileWeight();
		
		int seed = Parameters.seed != 0 ? Parameters.seed : (int)Time.GetTicksMsec();
		
		for(int attempt=0; attempt < Parameters.maxRetries; attempt++)
		{
			GD.Print($"DungeonGenerator: Attempt {attempt + 1} with seed {seed}.");
			//every attempt creats a grid
			var grid = new AlgoGrid(Parameters.gridWidth, Parameters.gridHeight, seed, TileRegistry.Instance.GetAllVariants());
			
			//if the grid algorithm succeeds, instance it.
			if(grid.Solve())
			{
				ClearFloor();
				InstanceTiles(grid, seed);
				EmitSignal(SignalName.FloorGenerated, seed);
				return;
			}
			
			seed++;
		}
		
		GD.PrintErr($"DungeonGenerator: Failed after {Parameters.maxRetries} attempts.");
		EmitSignal(SignalName.GenerationFailed);
	}
	
	// i dont know how to spell or say 'Instantiate'
	private void InstanceTiles(AlgoGrid grid, int seed)
	{
		int collapsed = 0;
		int empty = 0;
		int nullScene = 0;
		int instanced = 0;
		
		for(int i = 0; i < Parameters.gridWidth; i++)
		{
			for(int j = 0; j < Parameters.gridHeight; j++)
			{
				var cell = grid.GetCell(i,j);
				if(!cell.isCollapsed) continue;
				collapsed++;
				var variant = cell.collapsedVariant;
				
				if(variant.definition.tileId == "empty") {empty++; continue;}
				if(variant.definition.scene == null) {nullScene++; continue;}
				
				var instance = variant.definition.scene.Instantiate<Node3D>();
				
				instance.Position = new Vector3(i*4f, 0f, j*4f);
				instance.RotationDegrees = new Vector3(0f, variant.rotation, 0f);
				
				_floorContainer.AddChild(instance);
				instanced++;
			}
		}
		GD.Print($"InstanceTiles: collapsed={collapsed}, empty={empty}, nullScene={nullScene}, instanced={instanced}");
	}
	
	private void ClearFloor()
	{
		foreach(Node child in _floorContainer.GetChildren())
		{
			child.QueueFree();
		}
	}
	
	private void ApplyEmptyTileWeight()
	{
		var emptyVariant = TileRegistry.Instance.GetEmptyVariant();
		emptyVariant.definition.weight = Parameters.emptyTileWeight;
	}
	
}
