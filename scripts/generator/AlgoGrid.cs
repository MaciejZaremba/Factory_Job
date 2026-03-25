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
	public bool Solve()
	{
		GD.Print("AlgoGrid: Solve() entered.");
		////debugging feature
		//int stepLimit = width * height * 200;
		//int steps = 0;
		//
		//while(true)
		//{
			//if(steps++ > stepLimit)
			//{
				//GD.PrintErr("AlgoGrid: Step limit exceeded - possible infinite loop.");
				//return false;
			//}
	//
			//var groundTarget = GetLowestEntropyCell(_groundCells);
			//var upperTarget = GetLowestEntropyCell(_upperCells);
			//
			////if no target = success
			//if(groundTarget == null && upperTarget == null) 
			//{
				//GD.Print("AlgoGrid: All cells collapsed successfully.");
				//return true;
			//}
			//
			////if contradiction = restart
			//if(groundTarget?.isContradiction == true || upperTarget?.isContradiction == true) return false;
			//
			//AlgoCell target;
			//bool isUpper;
			//if(groundTarget == null) {target = upperTarget; isUpper = true;}
			//else if(upperTarget == null) {target = groundTarget; isUpper = false;}
			//else if(upperTarget.Entropy < groundTarget.Entropy) {target = upperTarget; isUpper = true;}
			//else{target = groundTarget; isUpper = false;}
			//
			//
			//target.CollapseRandom(_rand);
			//
			//if(!isUpper && target.collapsedVariant.definition.tileId == "stairs")
			//{
				//var stairVariant = target.collapsedVariant;
				//var stairsTop = GetMatchingStairsTop(stairVariant);
				//if(stairsTop != null)
				//{
					//var upperCell =_upperCells[target.gridPosition.X, target.gridPosition.Y];
					//upperCell.ForceCollapse(stairsTop);
					//if(!Propagate(upperCell,_upperCells)) return false;
				//}
			//}
			//
			////if cant propagate = restart
			//if(!Propagate(target, isUpper ? _upperCells : _groundCells))
			//{
				//GD.PrintErr($"AlgoGrid: Propagation failed from {target.gridPosition}.");
				//return false;
			//}
		//}
		//Solve the upper layer
		if(!SolveLayerSkipEmpty(_upperCells, width*height*100)) return false;
		GD.Print("AlgoGrid: Upper layer first pass complete.");
		//Solve staircases
		ForceStairSpawns(stairThreshold, stairScaling)
		GD.Print("AlgoGrid: Stair seeding complete");
		//Solve ground layer
		if(!SolveLayer(_groundCells, width*height*100)) return false;
		GD.Print("AlgoGrid: Upper layer first pass complete.");
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
		AlgoCell lowest = null
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
	
	private bool SolveLayerSkipEmpty(AlgoCell[,] layer, int stepLimit)
	{
		int steps = 0;
		while(true)
		{
			if(steps++ > stepLimit)
			{
				GD.PrintErr("AlgoGrid: Step limit exceeded in upper first pass.");
				return false;
			}
			var target = GetLowestEntropyCellExcludeEmpty(layer);
			if(target == null) return true;
			if(target.isContradiction) return false;
			
			target.CollapseRandom(_rand);
			if(!Propagate(target,layer)) return false;
		}
	}
	
	priuate void ForceStairSpawns(int stairThreshold, float stairScaling)
	{
		
	}
	
	private bool InBounds(Vector2I position) => position.X>=0 && position.X<width 
												&& position.Y>=0 && position.Y<height;
												
	public AlgoCell GetGroundCell(int x, int y) => _groundCells[x,y];
	public AlgoCell GetUpperCell(int x, int y) => _upperCells[x,y];
}
