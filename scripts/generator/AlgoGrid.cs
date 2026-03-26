using Godot;
using System.Collections.Generic;
using System.Linq;

public class AlgoGrid 
{
	public int width {get;}
	public int height {get;}
	
	private AlgoCell[,] _groundCells;
	private AlgoCell[,] _upperCells;
	private List<TileVariant> _allVariants;
	private System.Random _rand;
	
	private static readonly Dictionary<ConnectorDirection, Vector2I> DirectionOffsets = new()
	{
		{ConnectorDirection.North, new Vector2I (0, -1)},
		{ConnectorDirection.South, new Vector2I (0, 1)},
		{ConnectorDirection.East, new Vector2I (1, 0)},
		{ConnectorDirection.West, new Vector2I (-1, 0)}
	};
	
	private static readonly Dictionary<ConnectorDirection, ConnectorDirection> Opposite = new()
	{
		{ConnectorDirection.North, ConnectorDirection.South},
		{ConnectorDirection.South, ConnectorDirection.North},
		{ConnectorDirection.East, ConnectorDirection.West},
		{ConnectorDirection.West, ConnectorDirection.East}
	};
	
	public AlgoGrid(int width, int height, int seed, List<TileVariant> allVariants)
	{
		this.width = width;
		this.height = height;
		this._allVariants = allVariants;
		this._rand = new System.Random(seed);
		InitialiseCells();
		PreCollapseBorders();
	}
	
	//creates the grid
	private void InitialiseCells()
	{
		_groundCells = new AlgoCell[width, height];
		_upperCells = new AlgoCell[width, height];
		
		var groundVariants = _allVariants
			.Where(vant => vant.definition.tileId == "empty" ||
				(!vant.definition.tileId.StartsWith("walkway") &&
				(vant.HasConnectors(ConnectorDirection.North,ConnectorLevel.Ground) || 
				vant.HasConnectors(ConnectorDirection.South,ConnectorLevel.Ground) || 
				vant.HasConnectors(ConnectorDirection.West,ConnectorLevel.Ground) || 
				vant.HasConnectors(ConnectorDirection.East,ConnectorLevel.Ground)))
			).ToList();
			
		var upperVariants = _allVariants
			.Where(vant => vant.definition.tileId == "empty" ||
				(vant.definition.tileId != "stairs" &&
				(vant.HasConnectors(ConnectorDirection.North,ConnectorLevel.Upper) || 
				vant.HasConnectors(ConnectorDirection.South,ConnectorLevel.Upper) || 
				vant.HasConnectors(ConnectorDirection.West,ConnectorLevel.Upper) || 
				vant.HasConnectors(ConnectorDirection.East,ConnectorLevel.Upper)))
			).ToList();
		GD.Print("Upper layer variants:");
		foreach (var v in upperVariants)
			GD.Print($"  {v.definition.tileId} @ {v.rotation}°");
	
		for(int i = 0; i<width; i++)
		{
			for(int j = 0; j<height; j++)
			{
				_groundCells[i,j] = new AlgoCell(new Vector2I(i,j), groundVariants);
				_upperCells[i,j] = new AlgoCell(new Vector2I(i,j), upperVariants);
			}
		}
	}
	
	//creates borders
	private void PreCollapseBorders()
	{
		var emptyVariant = TileRegistry.Instance.GetEmptyVariant();
		
		for(int i = 0; i<width; i++)
		{
			_groundCells[i,0].ForceCollapse(emptyVariant);
			_groundCells[i,height-1].ForceCollapse(emptyVariant);
			_upperCells[i,0].ForceCollapse(emptyVariant);
			_upperCells[i,height-1].ForceCollapse(emptyVariant);
		}
		for(int j = 0; j<height; j++)
		{
			_groundCells[0,j].ForceCollapse(emptyVariant);
			_groundCells[width-1,j].ForceCollapse(emptyVariant);
			_upperCells[0,j].ForceCollapse(emptyVariant);
			_upperCells[width-1,j].ForceCollapse(emptyVariant);
		}
	}
	
	//the main loop of the algorithm
	public bool Solve(int stairThreshold, float stairScaling)
	{
		GD.Print("AlgoGrid: Solve() entered.");
		//Solve the upper layer
		if(!SolveLayerSkipEmpty(_upperCells, width*height*100)) return false;
		GD.Print("AlgoGrid: Upper layer first pass complete.");
		//Solve staircases
		ForceStairSpawns(stairThreshold, stairScaling);
		GD.Print("AlgoGrid: Stair seeding complete");
		//Solve ground layer
		if(!SolveLayer(_groundCells, width*height*100)) return false;
		GD.Print("AlgoGrid: Ground layer complete.");
		//Get rid of remaining upper layer cells
		if(!SolveLayerWithDeadends()) return false;
		GD.Print("AlgoGrid: Upper layer second pass complete.");
		return true;
	}
	
	//propagation - explanation on line 61 
	private bool Propagate(AlgoCell startCell, AlgoCell[,] layer)
	{
		var queue = new Queue<AlgoCell>();
		queue.Enqueue(startCell);
		while (queue.Count > 0)
		{
			var cell = queue.Dequeue();
			foreach (var (direction, offset) in DirectionOffsets)
			{
				var neighbourPosition = cell.gridPosition + offset;
				if (!InBounds(neighbourPosition)) continue;
				var neighbour = layer[neighbourPosition.X, neighbourPosition.Y];
				if(neighbour.isCollapsed) continue;
				var allowed = GetAllowedVariants(cell, direction);
				if(neighbour.Constrain(allowed))
				{
					if(neighbour.isContradiction) return false;
					queue.Enqueue(neighbour);
				}
			}
		}
		return true;
	}
	
	//returns a list of tiles that can be placed in the specific cell
	private List<TileVariant> GetAllowedVariants(AlgoCell cell, ConnectorDirection direction)
	{
		var allowed = new HashSet<(string tileId, int rotation)>();
		var oppositeDirection = Opposite[direction];
		foreach(var variant in cell.possibleVariants)
		{
			bool cellHasGroundConnector = variant.HasConnectors(direction, ConnectorLevel.Ground);
			bool cellHasUpperConnector = variant.HasConnectors(direction, ConnectorLevel.Upper);
			
			foreach(var candidate in _allVariants)
			{
				bool neighbourHasGroundConnector = candidate.HasConnectors(oppositeDirection, ConnectorLevel.Ground);
				bool neighbourHasUpperConnector = candidate.HasConnectors(oppositeDirection, ConnectorLevel.Upper);
				
				if(cellHasGroundConnector == neighbourHasGroundConnector && cellHasUpperConnector == neighbourHasUpperConnector)
				{
					bool forbidden = false;
					if(variant.definition.blacklistTags != null)
					{
						foreach(var blacklistTag in variant.definition.blacklistTags)
						{
							if(candidate.definition.tags != null && candidate.definition.tags.Contains(blacklistTag))
							{
								forbidden = true;
								break;
							}
						}
					}
					
					if(!forbidden) allowed.Add((candidate.definition.tileId, candidate.rotation));
				}
			}
		}
		return _allVariants
			.Where(vant => allowed.Contains((vant.definition.tileId, vant.rotation)))
			.ToList();
	}
	
	//finds the cell with the least possible tile options
	private AlgoCell GetLowestEntropyCell(AlgoCell[,] layer)
	{
		AlgoCell lowest = null;
		
		for(int i = 0; i<width; i++)
		{
			for(int j = 0; j<height; j++)
			{
				var cell = layer[i,j];
				if(cell.isCollapsed) continue;
				if(cell.isContradiction) return cell;
				if (lowest == null || cell.Entropy < lowest.Entropy) lowest = cell;
			}
		}
		return lowest;
	}
	
	private AlgoCell GetLowestEntropyCellExcludeEmpty(AlgoCell[,] layer)
	{
		AlgoCell lowest = null;
		for(int i=0; i<width;i++)
		{
			for(int j=0; j<height;j++)
			{
				var cell = layer[i,j];
				if(cell.isCollapsed) continue;
				
				bool hasNonEmpty = cell.possibleVariants.Any(vant => vant.definition.tileId != "empty");
				if(!hasNonEmpty) continue;
				
				if(cell.isContradiction) return cell;
				if(lowest == null || cell.Entropy < lowest.Entropy) lowest = cell;
			}
		}
		return lowest;
	}
	
	private TileVariant GetMatchingStairsTop(TileVariant stairVariant)
	{
		var match = _allVariants.FirstOrDefault(vant => vant.definition.tileId == "stairs_top" && vant.rotation == stairVariant.rotation);
		GD.Print($"GetMatchingStairsTop: stair rotation {stairVariant.rotation}°, " +
			 $"stair upper connector: {stairVariant.connectors.FirstOrDefault(c => c.level == ConnectorLevel.Upper)?.direction}, " +
			 $"matched top rotation: {match?.rotation}°, " +
			 $"top connectors: {string.Join(", ", match?.connectors.Select(c => $"{c.level}_{c.direction}") ?? new List<string>{"none"})}");	
		return match;
	}
	
	private bool SolveLayer(AlgoCell[,] layer, int stepLimit)
	{
		int steps = 0;
		while(true)
		{
			if(steps++ > stepLimit)
			{
				GD.PrintErr("AlgoGrid: Step limit exceeded.");
				return false;
			}
			
			var target = GetLowestEntropyCell(layer);
			if(target == null) return true;
			if(target.isContradiction) return false;
			target.CollapseRandom(_rand);
			if(!Propagate(target,layer)) return false;
		}
	}
	
	private bool SolveLayerSkipEmpty(AlgoCell[,] layer, int stepLimit)
	{
		GD.Print("SolveLayerSkipEmpty: entered.");
		
		for(int i =0; i<width;i++)
		{
			if(!Propagate(layer[i,0], layer)) return false;
			if(!Propagate(layer[i,height-1], layer)) return false;
		}
		for(int j =0; j<height;j++)
		{
			if(!Propagate(layer[0,j], layer)) return false;
			if(!Propagate(layer[width-1,j], layer)) return false;
		}
		
		int initialNonEmpty = 0;
		for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
				if (layer[x,y].possibleVariants.Any(v => v.definition.tileId != "empty"))
					initialNonEmpty++;
		
		GD.Print($"SolveLayerSkipEmpty: {initialNonEmpty} cells have non-empty options.");
		int steps = 0;
		while(true)
		{
			if(steps++ > stepLimit)
			{
				GD.PrintErr("AlgoGrid: Step limit exceeded in upper first pass.");
				return false;
			}
			var target = GetLowestEntropyCellExcludeEmpty(layer);
			if(target == null) 
			{
				int collapsedWalkway = 0;
				int collapsedEmpty = 0;
				int uncollapsed = 0;

				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						var cell = layer[x, y];
						if (!cell.isCollapsed) { uncollapsed++; continue; }
						if (cell.collapsedVariant.definition.tileId == "empty") collapsedEmpty++;
						else collapsedWalkway++;
					}
				}

				GD.Print($"SolveLayerSkipEmpty: walkway={collapsedWalkway}, empty={collapsedEmpty}, uncollapsed={uncollapsed}");
				return true;
			}
			if(target.isContradiction) return false;
			
			target.CollapseRandom(_rand);
			if(!Propagate(target,layer)) return false;
		}
	}
	
	private bool SolveLayerWithDeadends()
	{
		if(!SolveLayer(_upperCells, width*height*100)) return false;
		
		for(int i = 0; i<width;i++)
		{
			for(int j=0; j<height;j++)
			{
				var upperCell = _upperCells[i,j];
				if(!upperCell.isCollapsed) continue;
				var variant = upperCell.collapsedVariant;
				if(variant.definition.tileId == "empty") continue;
				if(variant.definition.tileId == "stairs_top") continue;
				
				foreach(var (direction,offset) in DirectionOffsets)
				{
					if(!variant.HasConnectors(direction, ConnectorLevel.Upper)) continue;
					
					var neighbour = new Vector2I(i,j) + offset;
					if(!InBounds(neighbour)) continue;
					
					var upperNeighbour = _upperCells[neighbour.X, neighbour.Y];
					if(upperNeighbour.isCollapsed) continue;
					if(upperNeighbour.collapsedVariant.definition.tileId != "empty") continue;
					
					var groundNeighbour = _groundCells[neighbour.X,neighbour.Y];
					if(!groundNeighbour.isCollapsed) continue;
					
					var groundVariant = groundNeighbour.collapsedVariant.definition.tileId;
					if(groundVariant == "empty") continue;
					
					TryPlaceDeadend(new Vector2I(i,j), direction);
				}
			}
		}
		return true;
	}
	
	private List<Vector2I> FloodFillWalkwayNetworks(Vector2I start, HashSet<Vector2I> globalVis)
	{
		var network = new List<Vector2I>();
		var queue = new Queue<Vector2I>();
		var localVis = new HashSet<Vector2I>();
		
		queue.Enqueue(start);
		localVis.Add(start);
		
		while(queue.Count > 0)
		{
			var current = queue.Dequeue();
			network.Add(current);
			globalVis.Add(current);
			var currentCell = _upperCells[current.X, current.Y];
			if(!currentCell.isCollapsed) continue;
			
			var variant = currentCell.collapsedVariant;
			
			foreach(var (direction,offset) in DirectionOffsets)
			{
				if(!variant.HasConnectors(direction, ConnectorLevel.Upper)) continue;
				var neighbour = current + offset;
				if(!InBounds(neighbour)) continue;
				if(localVis.Contains(neighbour)) continue;
				
				var neighbourCell = _upperCells[neighbour.X, neighbour.Y];
				if(!neighbourCell.isCollapsed) continue;
				
				var neighbourVariant = neighbourCell.collapsedVariant.definition.tileId;
				if(neighbourVariant == "empty") continue;
				if(neighbourVariant == "stairs_top") continue;
				localVis.Add(neighbour);
				queue.Enqueue(neighbour);
			}
		}
		return network;
	}
	
	private void ProcessWalkwayNetwork(List<Vector2I> network, int stairThreshold, float stairScaling)
	{
		GD.Print($"ProcessWalkwayNetwork: network size={network.Count}");
		var openEdges = new List<(Vector2I pos, ConnectorDirection dir)>();
		foreach(var pos in network)
		{
			var cell = _upperCells[pos.X, pos.Y];
			if(!cell.isCollapsed) continue;
			var variant = cell.collapsedVariant;
			
			foreach(var (direction, offset) in DirectionOffsets)
			{
				if(!variant.HasConnectors(direction, ConnectorLevel.Upper)) continue;
				var neighbour = pos+offset;
				if(!InBounds(neighbour)) continue;
				if(neighbour.X == 0 || neighbour.X == 0 || neighbour.X == width-1 || neighbour.Y == height-1) continue;
				var neighbourCell = _upperCells[neighbour.X, neighbour.Y];
				if(neighbourCell.isCollapsed)
				{
					var neighbourVariant = neighbourCell.collapsedVariant.definition.tileId;
					if(neighbourVariant == "empty") continue;
				}
				openEdges.Add((pos,direction));
			}
		}
		GD.Print($"ProcessWalkwayNetwork: found {openEdges.Count} open edges.");
		Shuffle(openEdges);
		int stairCount = 0;
		var placedStairPositions = new HashSet<Vector2I>();
		foreach(var (pos,direction) in openEdges)
		{
			var offset = DirectionOffsets[direction];
			var groundPos = pos+offset;
			if(placedStairPositions.Contains(groundPos)) continue;
			
			bool placeStair = ShouldPlaceStair(stairCount, stairThreshold, stairScaling);
			GD.Print($"ProcessWalkwayNetwork: edge at {pos} dir={direction}, " +
				 $"groundPos={groundPos}, placeStair={placeStair}, stairCount={stairCount}");
			if(placeStair && TryForceStair(groundPos, direction)) 
			{
				GD.Print($"ProcessWalkwayNetwork: stair placed at {groundPos}.");
				placedStairPositions.Add(groundPos);
				stairCount++;
			} else GD.Print($"ProcessWalkwayNetwork: stair skipped or failed at {groundPos}.");
		}
		 GD.Print($"ProcessWalkwayNetwork: finished, placed {stairCount} stairs.");
	}
	
	private void ForceStairSpawns(int stairThreshold, float stairScaling)
	{
		GD.Print("ForceStairsAtWalkwayEdges: entered.");
	
		int walkwayCells = 0;
		for (int x = 0; x < width; x++)
			for (int y = 0; y < height; y++)
			{
				var cell = _upperCells[x, y];
				if (!cell.isCollapsed) continue;
				var id = cell.collapsedVariant.definition.tileId;
				if (id != "empty" && id != "stairs_top") walkwayCells++;
			}
		
		GD.Print($"ForceStairsAtWalkwayEdges: {walkwayCells} collapsed walkway cells found.");
		
		
		
		var visited = new HashSet<Vector2I>();
		for(int i = 0; i<width;i++)
		{
			for(int j=0; j<height;j++)
			{
				var pos = new Vector2I(i,j);
				if(visited.Contains(pos)) continue;
				var cell = _upperCells[i,j];
				if(!cell.isCollapsed) continue;
				if(cell.collapsedVariant.definition.tileId == "empty") continue;
				if(cell.collapsedVariant.definition.tileId == "stairs_top") continue;
				var network = FloodFillWalkwayNetworks(pos,visited);
				ProcessWalkwayNetwork(network, stairThreshold, stairScaling);
			}
		}
	}
	
	private bool ShouldPlaceStair(int stairCount, int threshold, float scaling)
	{
		if(stairCount < threshold) return true;
		
		int excess = stairCount - threshold;
		float chance = excess / (excess + scaling);
		return (float) _rand.NextDouble() > chance;
	}
	
	private bool TryForceStair(Vector2I groundPos, ConnectorDirection walkwayEdgeDirection)
	{
		GD.Print($"TryForceStair: entered for groundPos={groundPos}, direction={walkwayEdgeDirection}");
		if(!InBounds(groundPos)) return false;
		var groundCell = _groundCells[groundPos.X, groundPos.Y];
		if(groundCell.isCollapsed)
		{
			
			if(groundCell.collapsedVariant.definition.tileId != "stairs") return false;
		}
		
		var requiredUpperDirection = Opposite[walkwayEdgeDirection];
		var stairVariant = _allVariants.FirstOrDefault(vant => vant.definition.tileId == "stairs" && 
							vant.HasConnectors(requiredUpperDirection,ConnectorLevel.Upper));
		GD.Print($"TryForceStair: pos={groundPos}, walkwayEdge={walkwayEdgeDirection}, " +
			 $"requiredUpper={requiredUpperDirection}, " +
			 $"stairVariant={(stairVariant == null ? "NULL" : $"{stairVariant.definition.tileId}@{stairVariant.rotation}")}");

		if(stairVariant == null) return false;
		groundCell.ForceCollapse(stairVariant);
		
		var stairsTop = GetMatchingStairsTop(stairVariant);
		GD.Print($"TryForceStair: stairsTop={(stairsTop == null ? "NULL" : $"{stairsTop.definition.tileId}@{stairsTop.rotation}")}");
		if(stairsTop != null)
		{
			_upperCells[groundPos.X,groundPos.Y].ForceCollapse(stairsTop);
			Propagate(_upperCells[groundPos.X,groundPos.Y], _upperCells);
		}
		Propagate(groundCell, _groundCells);
		return true;
	}
	
	private void TryPlaceDeadend(Vector2I pos, ConnectorDirection direction)
	{
		var oppositeDir = Opposite[direction];
		var deadendVariant = _allVariants.FirstOrDefault(vant => vant.definition.tileId == "walkway_deadend" &&
								vant.HasConnectors(oppositeDir,ConnectorLevel.Upper));
		if(deadendVariant == null) return;
		
		var offset = DirectionOffsets[direction];
		var target = pos+offset;
		if(!InBounds(target)) return;
		
		var targetCell = _upperCells[target.X,target.Y];
		targetCell.ForceCollapse(deadendVariant);
	}
	
	private void Shuffle<T>(List<T> list)
	{
		for(int i = list.Count - 1; i>0; i--)
		{
			int j = _rand.Next(i+1);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
	
	private bool InBounds(Vector2I position) => position.X>=0 && position.X<width 
												&& position.Y>=0 && position.Y<height;
												
	public AlgoCell GetGroundCell(int x, int y) => _groundCells[x,y];
	public AlgoCell GetUpperCell(int x, int y) => _upperCells[x,y];
}
