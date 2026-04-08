using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DungeonGenerator : Node
{
	[Export] public GenerationParameters Parameters {get; set;}
	// creats a Node3D object that holds the tiles during generation
	[Export] public NodePath FloorContainerPath {get; set;}
	//seed
	[Export] public ulong Seed {get; private set;}
	//emits when generation succeeds - used to extract the seed
	[Signal] public delegate void FloorGeneratedEventHandler(RandomNumberGenerator seed);
	//emits when generation fails
	[Signal] public delegate void GenerationFailedEventHandler();
	
	private AlgoGrid _lastGrid {get; set;}
	
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
		var _rand = new RandomNumberGenerator();
		if(Parameters.seed == 0)
		{
			_rand.Randomize();
			Seed = _rand.Seed;
		} else {
			_rand.Seed = Parameters.seed;
			Seed = _rand.Seed;
		}
		
		for(int attempt=0; attempt < Parameters.maxRetries; attempt++)
		{
			GD.Print($"DungeonGenerator: Attempt {attempt + 1} with Seed {Seed}.");
			//every attempt creats a grid
			var grid = new AlgoGrid(Parameters.gridWidth, Parameters.gridHeight, _rand, Parameters.stairMaxThreshold, TileRegistry.Instance.GetAllVariants());
			
			//if the grid algorithm succeeds, instance it.
			if(grid.Solve(Parameters.stairMinThreshold, Parameters.stairScaling, Parameters.maxWalkwayNetworks))
			{
				ClearFloor();
				_lastGrid = grid;
				InstanceTiles(grid, Seed);
				EmitSignal(SignalName.FloorGenerated, _rand);
				EventManager.Instance.FloorGenerated(_rand);
				UIManager.Instance.FloorGenerated(_rand);
				return;
			}
			
			_rand.Seed = Seed++;
		}
		
		GD.PrintErr($"DungeonGenerator: Failed after {Parameters.maxRetries} attempts.");
		EmitSignal(SignalName.GenerationFailed);
	}
	
	public Vector3 GetSpawnPosition()
	{
		if(_lastGrid == null)
		{
			GD.PrintErr("DungeonGenerator: GetSpawnPosition Called before generation.");
			return Vector3.Zero;
		}
		
		
		for(int i = 1; i<Parameters.gridWidth-1; i++)
		{
			for(int j = 1; j < Parameters.gridHeight-1; j++)
			{
				var cell = _lastGrid.GetGroundCell(i,j);
				if(!cell.isCollapsed) continue;
				var variant = cell.collapsedVariant;
				if(variant.definition.tileId == "empty") continue;
				if(variant.definition.tileId == "stairs") continue;
				if(variant.definition.tileId.StartsWith("walkway")) continue;
				
				return new Vector3(i*4f,1f,j*4f);
			}
		}
		return new Vector3(Parameters.gridWidth*2f,1f,Parameters.gridHeight*2f);
		//return new Vector3(4f,4f,4f);
	}
	
	// i dont know how to spell or say 'Instantiate'
	private void InstanceTiles(AlgoGrid grid, ulong Seed)
	{
		int instanced = 0;
		
		for(int i = 0; i < Parameters.gridWidth; i++)
		{
			for(int j = 0; j < Parameters.gridHeight; j++)
			{
				instanced += InstanceCell(grid.GetGroundCell(i,j),i,j);
				instanced += InstanceCell(grid.GetUpperCell(i,j),i,j);
			}
		}
		GD.Print($"InstanceTiles: instanced={instanced}");
	}
	
	private int InstanceCell(AlgoCell cell, int i, int j)
	{
		if(!cell.isCollapsed) return 0;
		var variant = cell.collapsedVariant;
		
		if(variant.definition.tileId == "empty") return 0;
		if(variant.definition.tileId == "stairs_top") return 0;
		if(variant.definition.scene == null) return 0;
		
		var instance = variant.definition.scene.Instantiate<Node3D>();
		
		instance.Position = new Vector3(i*4f, 0f, j*4f);
		instance.RotationDegrees = new Vector3(0f, -variant.rotation, 0f);
		
		_floorContainer.AddChild(instance);
		return 1;
	}
	
	private void ClearFloor()
	{
		foreach(Node child in _floorContainer.GetChildren())
		{
			child.QueueFree();
		}
	}
}
