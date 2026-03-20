using Godot;
using System.Collections.Generic;
using System.Linq;

public class AlgoGrid 
{
	public int width {get;}
	public int height {get;}
	
	private AlgoCell[,] _cells;
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
		_cells = new AlgoCell[width, height];
		for(int i = 0; i<width; i++)
		{
			for(int j = 0; j<height; j++)
			{
				_cells[i,j] = new AlgoCell(new Vector2I(i,j), _allVariants);
			}
		}
	}
	
	//creates borders
	private void PreCollapseBorders()
	{
		var emptyVariant = TileRegistry.Instance.GetEmptyVariant();
		
		for(int i = 0; i<width; i++)
		{
			_cells[i,0].ForceCollapse(emptyVariant);
			_cells[i,height-1].ForceCollapse(emptyVariant);
		}
		for(int j = 0; j<width; j++)
		{
			_cells[0,j].ForceCollapse(emptyVariant);
			_cells[width-1,j].ForceCollapse(emptyVariant);
		}
		
		//propagate communicates the consequences of a collapse to neighbouring cells
		for(int i = 0; i<width; i++)
		{
			Propagate(_cells[i,0]);
			Propagate(_cells[i,height-1]);
		}
		for(int j = 0; j<width; j++)
		{
			Propagate(_cells[0,j]);
			Propagate(_cells[width-1,j]);
		}
	}
	
	//the main loop of the algorithm
	public bool Solve()
	{
		while(true)
		{
			var target = GetLowestEntropyCell();
			
			//if no target = success
			if(target == null) return true;
			
			//if contradiction = restart
			if(target.isContradiction) return false;
			
			target.CollapseRandom(_rand);
			
			//if cant propagate = restart
			if(!Propagate(target)) return false;
		}
	}
	
	//propagation - explanation on line 61 
	private bool Propagate(AlgoCell startCell)
	{
		var queue = new Queue<AlgoCell>();
		queue.Enqueue(startCell);
		while (queue.Count > 0);
		{
			var cell = queue.Dequeue();
			foreach (var (direction, offset) in DirectionOffsets)
			{
				var neighbourPosition = cell.gridPosition + offset;
				if (!InBounds(neighbourPosition)) continue;
				var neighbour = _cells[neighbourPosition.X, neighbourPosition.Y];
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
			bool cellHasConnector = variant.HasConnectors(direction, ConnectorLevel.Ground) 
									|| variant.HasConnectors(direction, ConnectorLevel.Upper);
			
			foreach(var candidate in _allVariants)
			{
				bool neighbourHasConnector = candidate.HasConnectors(oppositeDirection, ConnectorLevel.Ground)
											|| candidate.HasConnectors(oppositeDirection, ConnectorLevel.Upper);
				
				bool compatible = cellHasConnector == neighbourHasConnector;
				
				if(compatible && cellHasConnector)
				{
					bool groundMatch = variant.HasConnectors(direction, ConnectorLevel.Ground)
									&& candidate.HasConnectors(oppositeDirection, ConnectorLevel.Ground);
					bool upperMatch = variant.HasConnectors(direction, ConnectorLevel.Upper)
									&& candidate.HasConnectors(oppositeDirection, ConnectorLevel.Upper);
					compatible = groundMatch || upperMatch;
				}
				if (compatible)
				{
					allowed.Add((candidate.definition.tileId, candidate.rotation));
				}
			}
		}
		return _allVariants
			.Where(vant => allowed.Contains((vant.definition.tileId, vant.rotation)))
			.ToList();
	}
	
	//finds the cell with the least possible tile options
	private AlgoCell GetLowestEntropyCell()
	{
		AlgoCell lowest = null;
		
		for(int i = 0; i<width; i++)
		{
			for(int j = 0; j<height; j++)
			{
				var cell = _cells[i,j];
				if(cell.isCollapsed) continue;
				if(cell.isContradiction) return cell;
				if (lowest == null || cell.Entropy < lowest.Entropy) lowest = cell;
			}
		}
		return lowest;
	}
	
	private bool InBounds(Vector2I position) => position.X>=0 && position.X<width 
												&& position.Y>=0 && position.Y<height;
												
	public AlgoCell GetCell(int x, int y) => _cells[x,y];
}
