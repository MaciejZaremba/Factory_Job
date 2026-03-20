using Godot;
using System.Collections.Generic;
using System.Linq;

public class AlgoCell
{
	public Vector2I gridPosition {get;}
	public List<TileVariant> possibleVariants {get; private set;}
	public bool isCollapsed => possibleVariants.Count == 1;
	public bool isContradiction => possibleVariants.Count == 0;
	
	public TileVariant collapsedVariant => isCollapsed ? possibleVariants[0] : null;

	// collapsing means the map has been successfully built
	//contradiction means the algorithm hasn't collapsed, but cannot perform any legal moves -> ponowna próba
	public AlgoCell(Vector2I gridPosition, List<TileVariant> allVariants)
	{
		this.gridPosition = gridPosition;
		//at the start, everything is possible
		possibleVariants = new List<TileVariant>(allVariants);
	}
	
	//number of remaining options / chance at collapsing
	public float Entropy
	{
		get
		{
			if (isCollapsed || isContradiction) return 0f;
			float totalWeight = possibleVariants.Sum(vant => vant.definition.weight);
			float entropy = 0f;
			foreach(var variant in possibleVariants)
			{
				float probability = variant.definition.weight / totalWeight;
				entropy -= probability * Mathf.Log(probability);
			}
			return entropy;
		}
	}
	
	//Forces the cell to be a specific variant
	public void ForceCollapse(TileVariant variant)
	{
		possibleVariants = new List<TileVariant> {variant};
	}
	
	//removing variants that aren't valid based on the neighboring cells
	public bool Constrain(List<TileVariant> allowedVariants)
	{
		int countBefore = possibleVariants.Count;
		possibleVariants = possibleVariants.Where(vant =>
			allowedVariants.Any(any => any.definition.tileId == vant.definition.tileId &&
			any.rotation == vant.rotation)
			).ToList();
		return possibleVariants.Count < countBefore;
	}
	
	//randomly collapses a cell to one of the remaining variants
	public void CollapseRandom(System.Random rand)
	{
		if(isCollapsed) return;
		
		float totalWeight = possibleVariants.Sum(vant => vant.definition.weight);
		float roll = (float)rand.NextDouble() * totalWeight;
		float count = 0f;
		
		foreach (var variant in possibleVariants)
		{
			count += variant.definition.weight;
			if(roll <= count)
			{
				ForceCollapse(variant);
				return;
			}
		}
		//safeguard, the code should not reach this
		ForceCollapse(possibleVariants[^1]);
	}
}
