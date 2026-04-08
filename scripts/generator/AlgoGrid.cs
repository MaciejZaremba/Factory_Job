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
	private List<TileVariant> groundVariants;
	private List<TileVariant> upperVariants;
	private RandomNumberGenerator _rand;
	private int totalStairCount = 0;
	private int stairMaxT;
	
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
	
	public AlgoGrid(int width, int height, RandomNumberGenerator seed, int stairMaxT ,List<TileVariant> allVariants)
	{
		this.width = width;
		this.height = height;
		this._rand = seed;
		this.stairMaxT = stairMaxT;
		
		this._allVariants = allVariants ?? new List<TileVariant>();
		if (allVariants == null || allVariants.Count == 0)
		{
			GD.PrintErr("CRITICAL: AlgoGrid received 0 variants. Generation will fail.");
			return; 
		}
		
		InitialiseCells();
		PreCollapseBorders();
	}
	
	//creates the grid
	private void InitialiseCells()
	{
		_groundCells = new AlgoCell[width, height];
		_upperCells = new AlgoCell[width, height];
		
		groundVariants = _allVariants
			.Where(vant => vant.definition.tileId == "empty" ||
				(!vant.definition.tileId.StartsWith("walkway") && 
				!vant.definition.tileId.StartsWith("stairs") &&
				!vant.definition.tileId.StartsWith("end") &&
				(vant.HasConnectors(ConnectorDirection.North,ConnectorLevel.Ground) || 
				vant.HasConnectors(ConnectorDirection.South,ConnectorLevel.Ground) || 
				vant.HasConnectors(ConnectorDirection.West,ConnectorLevel.Ground) || 
				vant.HasConnectors(ConnectorDirection.East,ConnectorLevel.Ground)))
			).ToList();
			
		upperVariants = _allVariants
			.Where(vant => vant.definition.tileId == "empty" ||
				(!vant.definition.tileId.StartsWith("stairs") &&
				(vant.HasConnectors(ConnectorDirection.North,ConnectorLevel.Upper) || 
				vant.HasConnectors(ConnectorDirection.South,ConnectorLevel.Upper) || 
				vant.HasConnectors(ConnectorDirection.West,ConnectorLevel.Upper) || 
				vant.HasConnectors(ConnectorDirection.East,ConnectorLevel.Upper)))
			).ToList();
				
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
		if (emptyVariant == null)
		{
			GD.PrintErr("AlgoGrid: Could not find 'empty' variant in Registry. Check your tile IDs!");
			return; // Stop here to prevent the line 161 crash
		}
		
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
	public bool Solve(int stairMinT, float stairScaling, int maxNetworks)
	{
		GD.Print("AlgoGrid: Solve() entered.");
		//Create spawn point -> also helps kickstart upper layer
		CreateSpawnPoint();
		//Solve the upper layer
		if(!SolveLayerSkipEmpty(_upperCells, width*height*100, ConnectorLevel.Upper)) return false;
		GD.Print("AlgoGrid: Upper layer first pass complete.");
		//get rid of small networks
		PruneSmallNetworks(maxNetworks);
		GD.Print("AlgoGrid: Walkway networks pruned.");
		//Solve staircases
		ForceStairSpawns(stairMinT ,stairScaling);
		GD.Print("AlgoGrid: Stair seeding complete");
		//Solve ground layer
		if(!SolveLayer(_groundCells, width*height*100, ConnectorLevel.Ground)) return false;
		GD.Print("AlgoGrid: Ground layer complete.");
		//Create an End Cell
		CreateEndPoint();
		GD.Print("AlgoGrid: End Point Created.");
		//Get rid of remaining upper layer cells
		if(!SolveLayerWithDeadends(ConnectorLevel.Upper)) return false;
		GD.Print("AlgoGrid: Upper layer second pass complete.");
		return true;
	}
	
	//propagation - explanation on line 61 
	private bool Propagate(AlgoCell startCell, AlgoCell[,] layer, ConnectorLevel level)
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
				var allowed = GetAllowedVariants(cell, direction, level);
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
	private List<TileVariant> GetAllowedVariants(AlgoCell cell, ConnectorDirection direction, ConnectorLevel level)
	{
		var allowed = new HashSet<(string tileId, int rotation)>();
		var oppositeDirection = Opposite[direction];
		foreach(var variant in cell.possibleVariants)
		{
			bool cellHasConnector = variant.HasConnectors(direction, level);
			
			foreach(var candidate in _allVariants)
			{
				bool neighbourHasConnector = candidate.HasConnectors(oppositeDirection,level);

				if(cellHasConnector == neighbourHasConnector)
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
		
		if(level == ConnectorLevel.Ground)
		{
			return groundVariants
				.Where(vant => allowed.Contains((vant.definition.tileId, vant.rotation)))
				.ToList();
		} else if(level == ConnectorLevel.Upper)
		{
			return upperVariants
			.Where(vant => allowed.Contains((vant.definition.tileId, vant.rotation)))
			.ToList();
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
	
	private bool SolveLayer(AlgoCell[,] layer, int stepLimit, ConnectorLevel level)
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
			if (target.isContradiction)
			{
				GD.PrintErr($"AlgoGrid: Contradiction at {target.gridPosition}, " +
							$"possible variants: {target.possibleVariants.Count}");
				return false;
			}
			target.CollapseRandom(_rand);
			if (!Propagate(target, layer, level))
			{
				GD.PrintErr($"AlgoGrid: Propagation failed from {target.gridPosition}, " +
							$"collapsed to '{target.collapsedVariant?.definition.tileId}@{target.collapsedVariant?.rotation}'");
				return false;
			}
		}
	}
	
	private bool SolveLayerSkipEmpty(AlgoCell[,] layer, int stepLimit, ConnectorLevel level)
	{
		GD.Print("SolveLayerSkipEmpty: entered.");
		
		for(int i =0; i<width;i++)
		{
			if(!Propagate(layer[i,0], layer, level)) return false;
			if(!Propagate(layer[i,height-1], layer, level)) return false;
		}
		for(int j =0; j<height;j++)
		{
			if(!Propagate(layer[0,j], layer, level)) return false;
			if(!Propagate(layer[width-1,j], layer, level)) return false;
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
			if(!Propagate(target,layer,level)) return false;
		}
	}
	
	private bool SolveLayerWithDeadends(ConnectorLevel level)
	{
		if(!SolveLayer(_upperCells, width*height*100, level)) return false;
		
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
					if(!upperNeighbour.isCollapsed) continue;
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
	
	private void ProcessWalkwayNetwork(List<Vector2I> network, int stairMinT, float stairScaling)
	{
		GD.Print($"ProcessWalkwayNetwork: network size={network.Count}");
		var openEdges = new List<(Vector2I pos, ConnectorDirection dir)>();
		foreach(var pos in network)
		{
			var cell = _upperCells[pos.X, pos.Y];
			if(!cell.isCollapsed) continue;
			var variant = cell.collapsedVariant;
			
			var upperConnectors = variant.connectors.Where(con => con.level == ConnectorLevel.Upper).ToList();
			
			if (upperConnectors.Count != 1) continue;
			
			var connectorDir = upperConnectors[0].direction;
			var walkwayEdgeDir = Opposite[connectorDir];
			
			if(pos.X <=1 || pos.Y <=1 || pos.X >=width-2 || pos.Y >= height-2) continue;
			openEdges.Add((pos, walkwayEdgeDir));
		}
		
		GD.Print($"ProcessWalkwayNetwork: found {openEdges.Count} dead-end edges.");
		Shuffle(openEdges);
		int stairCount = 0;
		var placedStairPositions = new HashSet<Vector2I>();
		foreach(var (groundPos,direction) in openEdges)
		{
			if(placedStairPositions.Contains(groundPos)) continue;
			
			bool placeStair = ShouldPlaceStair(stairCount, stairMinT ,stairScaling);
			GD.Print($"ProcessWalkwayNetwork: dead-end at {groundPos} dir={direction}, " +
				 $"placeStair={placeStair}, stairCount={stairCount}");
			if(placeStair && TryForceStair(groundPos, direction)) 
			{
				GD.Print($"ProcessWalkwayNetwork: stair placed at {groundPos}.");
				placedStairPositions.Add(groundPos);
				stairCount++;
				totalStairCount++;
			} else GD.Print($"ProcessWalkwayNetwork: stair skipped or failed at {groundPos}.");
		}
		 GD.Print($"ProcessWalkwayNetwork: finished, placed {stairCount} stairs.");
	}
	
	private void ForceStairSpawns(int stairMinT, float stairScaling)
	{
		GD.Print("ForceStairSpawns: entered.");
		
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
				ProcessWalkwayNetwork(network, stairMinT, stairScaling);
			}
		}
	}
	
	private bool ShouldPlaceStair(int stairCount, int stairMinT, float scaling)
	{
		if(stairCount < stairMinT) return true;
		if(totalStairCount >= stairMaxT) return false;
		int excess = stairCount - stairMinT;
		float chance = excess / (excess + scaling); 
		return _rand.Randf() > chance;
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
		foreach(var (_,offset) in DirectionOffsets)
		{
			var adjacent = groundPos + offset;
			if(!InBounds(adjacent)) continue;
			var adjacentCell = _groundCells[adjacent.X,adjacent.Y];
			if(adjacentCell.isCollapsed && adjacentCell.collapsedVariant.definition.tileId == "stairs")
			{
				GD.Print($"TryForceStair: rejected {groundPos} — adjacent stair at {adjacent}");
				return false;
			}
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
			Propagate(_upperCells[groundPos.X,groundPos.Y], _upperCells, ConnectorLevel.Upper);
		}
		Propagate(groundCell, _groundCells, ConnectorLevel.Ground);
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
	
	private void PruneSmallNetworks(int maxNetworks)
	{
		var visited = new HashSet<Vector2I>();
		var networks = new List<List<Vector2I>>();
		
		for(int i = 0; i<width; i++)
		{
			for(int j = 0; j<height; j++)
			{
				var pos = new Vector2I(i,j);
				if(visited.Contains(pos)) continue;
				var cell = _upperCells[i,j];
				if(!cell.isCollapsed) continue;
				var id = cell.collapsedVariant.definition.tileId;
				if(id == "empty" || id == "stairs_top") continue;
				
				var network = FloodFillWalkwayNetworks(pos, visited);
				networks.Add(network);
			}
		}
		GD.Print($"PruneSmallWalkwayNetworks: Found {networks.Count} networks.");
		
		//if(networks.Count <= maxNetworks) return;
		
		networks.Sort((a,b) => b.Count.CompareTo(a.Count()));
		var networksToKeep = new HashSet<Vector2I>();
		int kept = 0;
		foreach(var network in networks)
		{
			if(kept >= maxNetworks) break;
			if(network.Count < 3) continue;
			foreach(var pos in network) networksToKeep.Add(pos);
			kept++;
		}
		
		for(int i=0; i<maxNetworks && i<networks.Count; i++)
		{
			foreach(var pos in networks[i]) networksToKeep.Add(pos);
		}
		var emptyVariant = TileRegistry.Instance.GetEmptyVariant();
		int pruned = 0;
		
		for(int i = 0; i<width; i++)
		{
			for(int j = 0; j<height; j++)
			{
				var pos = new Vector2I(i,j);
				var cell = _upperCells[i,j];
				if(!cell.isCollapsed) continue;
				
				var id = cell.collapsedVariant.definition.tileId;
				if(id == "empty" || id == "stairs_top") continue;
				if(!networksToKeep.Contains(pos))
				{
					cell.ForceCollapse(emptyVariant);
					pruned++;
				}
			}
		}
		GD.Print($"PruneSmallWalkwayNetworks: pruned {pruned} cells from small networks.");
	}
	
	//Tworzy punkt zaczepny dla górnej warstwy, nie spawn point dla gracza
	private void CreateSpawnPoint()
	{
		var spawnVariant = _allVariants.FirstOrDefault(vant => vant.definition.tileId == "walkway_right" && vant.rotation == 0);
		if(spawnVariant != null) _upperCells[1,1].ForceCollapse(spawnVariant);
	}
	

	private void CreateEndPoint()
	{
		var endVariant = _allVariants.FirstOrDefault(vant => vant.definition.tileId == "end");
		Vector2I endSpawn;
		if(endVariant != null) 
		{
			endSpawn = FindOpenTile();
			_groundCells[endSpawn.X, endSpawn.Y].ForceCollapse(endVariant);
		}
	}
	
	//Last minute addition, w pełnej grze zrobiłbym inaczej
	private Vector2I FindOpenTile()
	{
		Vector2I vector = new Vector2I();
		for (int i = width-1; i >0; i--)
		{
			for (int j = height-1; j > 0; j--)
			{
				var cell = _groundCells[i, j];
				if (!cell.isCollapsed) continue;
				var id = cell.collapsedVariant.definition.tileId;
				if (id != "empty" && id != "stairs") 
				{
					vector.X = i;
					vector.Y = j;
					return vector;
				}
			}
		}
		vector.X = width-1;
		vector.Y = height-1;
		return vector;
	}
	
	private void Shuffle<T>(List<T> list)
	{
		for(int i = list.Count - 1; i>0; i--)
		{
			int j = _rand.RandiRange(0, i);
			GD.Print($"CreateSpawnPoint: j = {j}, RandiRange max = {i+1}");
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
	
	private bool InBounds(Vector2I position) => position.X>=0 && position.X<width && position.Y>=0 && position.Y<height;
												
	public AlgoCell GetGroundCell(int x, int y) => _groundCells[x,y];
	public AlgoCell GetUpperCell(int x, int y) => _upperCells[x,y];
}
