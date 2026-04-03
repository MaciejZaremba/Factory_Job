using Godot;
using Godot.Collections;

public partial class Inventory : Node
{
	public static Inventory Instance {get;private set;}
	[Export] public Dictionary<ItemCategory, Array<ItemDefinition>> equipment {get;set;} = new();
	[Export] public Array<ItemDefinition> storage {get;set;} = new();
	
	public override void _Ready()
	{
		Instance = this;
	}
}
