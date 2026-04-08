using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class TileRegistry : Node
{
	public static TileRegistry Instance {get; private set;}
	private List<TileVariant> _variants = new();
	[Export] public string TileDataPath {get; set;} = "res://data/tiles/";
	
	public override void _Ready()
	{
		GD.Print("TileRegistry: _Ready() entered.");
		Instance = this;
		loadTiles();
	}
	
	private void loadTiles() 
	{
		GD.Print("TileRegistry: Trying to open path...");
		using var dir = DirAccess.Open(TileDataPath);
		
		//check if path is correct
		if(dir == null)
		{
			GD.PrintErr($"TileRegistry: Could not open tile data path: {TileDataPath}");
			return;
		}
		
		dir.ListDirBegin();
		string fileName = dir.GetNext();
		
		//go through approperiate file and register it as a tile
		while(fileName != "")
		{
			//GD.Print($"TileRegistry: Trying to load '{TileDataPath + fileName}...");
			if(fileName.EndsWith(".tres") || fileName.EndsWith(".tres.remap"))
			{
				string cleanPath = TileDataPath + fileName.Replace(".remap", "");
				var definition = GD.Load<TileDefinition>(cleanPath);
				if(definition != null)
				{
					registerDefinition(definition);
				}
			}
			fileName = dir.GetNext();
		}
		
		dir.ListDirEnd();
		GD.Print($"TileRegistry: Registered {_variants.Count} tile variants");
	}
	
	//register all tiles
	private void registerDefinition(TileDefinition definition)
	{
		//GD.Print($"TileRegistry: Registering '{definition.tileId}'...");
		 _variants.Add(new TileVariant(definition, 0));
		
		//if it can't be rotated, end here
		if(!definition.canRotate)
		{
			return;
		}
		
		// rotate 90 degrees and check for duplicate positioning 
		for (int rotation = 90; rotation < 360; rotation += 90)
		{
			var candidate = new TileVariant(definition, rotation);
			if(!IsDuplicateVariant(candidate))
			{
				_variants.Add(candidate);
			}
		}
	}
	
	//Checks duplicates
	public bool IsDuplicateVariant(TileVariant candidate)
	{
		// the '=>' is basically a Java stream, a "simplified" foreach loop
		return _variants.Any(existing =>
			existing.definition.tileId == candidate.definition.tileId &&
			ConnectorSetsMatch(existing.connectors, candidate.connectors));
	}
	
	private bool ConnectorSetsMatch(List<TileConnector> existing, List<TileConnector> candidate)
	{
		if(existing.Count != candidate.Count) return false;
		return existing.All(existing_t => candidate.Any(
			candidate_t => candidate_t.direction == existing_t.direction && candidate_t.level == existing_t.level
		));
	}
	
	//Looks through all variants, chooses ones compatible with current tile
	public List<TileVariant> GetCompatibleVariants(List<TileConnector> required, List<TileConnector> forbidden)
	{
		return _variants.Where(variant =>
			required.All(req => variant.HasConnectors(req.direction, req.level)) &&
			forbidden.All(forb => !variant.HasConnectors(forb.direction, forb.level))
		).ToList();
	}
	
	//self explanatory
	public List<TileVariant> GetAllVariants() => _variants;
	
	//selects an empty tile
	public TileVariant GetEmptyVariant() => _variants.FirstOrDefault(fir => fir.definition.tileId == "empty");
}
