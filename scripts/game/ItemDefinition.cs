using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemDefinition : Resource
{
	[Export] public string name {get;set;}
	[Export] public string description {get;set;}
	[Export] public ItemCategory category {get;set;}
	[Export] public Array<CardDefinition> cards {get;set;} = new();
	[Export] public Array<BuffDefinition> buffs {get;set;} = new();
	[Export] public Texture2D icon {get;set;}
}
