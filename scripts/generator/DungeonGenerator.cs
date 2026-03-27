using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DungeonGenerator : Node
{
	[Export] public GenerationParameters Parameters {get; set;}
	// creats a Node3D object that holds the tiles during generation
	[Export] public NodePath FloorContainerPath {get; set;}
	//emits when generation succeeds - used to extract the seed
	[Signal] public delegate void FloorGeneratedEventHandler(int seed);
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
		
		int seed = Parameters.seed != 0 ? Parameters.seed : (int)Time.GetTicksMsec();
		
		for(int attempt=0; attempt < Parameters.maxRetries; attempt++)
		{
			GD.Print($"DungeonGenerator: Attempt {attempt + 1} with seed {seed}.");
			//every attempt creats a grid
			var grid = new AlgoGrid(Parameters.gridWidth, Parameters.gridHeight, seed, Parameters.stairMaxThreshold, TileRegistry.Instance.GetAllVariants());
			
			//if the grid algorithm succeeds, instance it.
			if(grid.Solve(Parameters.stairMinThreshold, Parameters.stairScaling, Parameters.maxWalkwayNetworks))
			{
				ClearFloor();
				_lastGrid = grid;
				//DiagnoseStairs(grid);
				InstanceTiles(grid, seed);
				EmitSignal(SignalName.FloorGenerated, seed);
				return;
			}
			
			seed++;
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
	}
	
	// i dont know how to spell or say 'Instantiate'
	private void InstanceTiles(AlgoGrid grid, int seed)
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
		instance.RotationDegrees = new Vector3(0f, variant.rotation, 0f);
		
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
	
// Add this temporary method to DungeonGenerator
// Call it right after generation succeeds, before InstantiateTiles
//private void DiagnoseStairs(AlgoGrid grid)
//{
	//GD.Print("=== STAIR DIAGNOSIS ===");
//
	//// First print what the registry thinks stairs looks like
	//foreach (var variant in TileRegistry.Instance.GetAllVariants())
	//{
		//if (variant.definition.tileId != "stairs") continue;
		//GD.Print($"Stair variant at {variant.rotation}°:");
		//foreach (var connector in variant.connectors)
			//GD.Print($"  {connector.level}_{connector.direction}");
	//}
//
	//// Then print every placed stair and its neighbours
	//for (int x = 0; x < Parameters.gridWidth; x++)
	//{
		//for (int y = 0; y < Parameters.gridHeight; y++)
		//{
			//var groundCell = grid.GetGroundCell(x, y);
			//var upperSelf = grid.GetUpperCell(x, y);
			//if (upperSelf.isCollapsed)
   				//GD.Print($"  Upper self → {upperSelf.collapsedVariant.definition.tileId}@{upperSelf.collapsedVariant.rotation}°, connectors: {string.Join(", ", upperSelf.collapsedVariant.connectors.Select(c => $"{c.level}_{c.direction}"))}");
			//if (!groundCell.isCollapsed) continue;
			//if (groundCell.collapsedVariant.definition.tileId != "stairs") continue;
//
			//var variant = groundCell.collapsedVariant;
			//GD.Print($"Stair at ({x},{y}) rotation {variant.rotation}°");
//
			//// Print all 4 neighbours on both layers
			//foreach (var (dir, offset) in new System.Collections.Generic.Dictionary
				//<ConnectorDirection, Vector2I>
			//{
				//{ ConnectorDirection.North, new Vector2I(0,-1) },
				//{ ConnectorDirection.South, new Vector2I(0, 1) },
				//{ ConnectorDirection.East,  new Vector2I(1, 0) },
				//{ ConnectorDirection.West,  new Vector2I(-1,0) }
			//})
			//{
				//int nx = x + offset.X;
				//int ny = y + offset.Y;
				//if (nx < 0 || nx >= Parameters.gridWidth ||
					//ny < 0 || ny >= Parameters.gridHeight) continue;
//
				//var groundNeighbour = grid.GetGroundCell(nx, ny);
				//var upperNeighbour = grid.GetUpperCell(nx, ny);
//
				//string groundId = groundNeighbour.isCollapsed
					//? $"{groundNeighbour.collapsedVariant.definition.tileId}@{groundNeighbour.collapsedVariant.rotation}°"
					//: "uncollapsed";
				//string upperId = upperNeighbour.isCollapsed
					//? $"{upperNeighbour.collapsedVariant.definition.tileId}@{upperNeighbour.collapsedVariant.rotation}°"
					//: "uncollapsed";
//
				//GD.Print($"  {dir} → ground: {groundId} | upper: {upperId}");
			//}
		//}
	//}
//
	//GD.Print("=== END STAIR DIAGNOSIS ===");
//}
	//
}
