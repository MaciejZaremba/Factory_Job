using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TileDefinition : Resource
{
	[Export] public string tileId {get; set;} = "";
	[Export] public PackedScene scene {get; set;}
	[Export] public float weight {get; set;} = 1.0f;
	[Export] public bool canRotate {get; set;} = true;
	[Export] public Array<TileConnector> connectors {get; set;} = new();
	[Export] public Array<string> tags {get;set;} = new();
	[Export] public Array<string> blacklistTags {get;set;} = new();
}
