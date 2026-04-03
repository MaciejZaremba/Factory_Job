using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CardDefinition : Resource
{
	[Export] public string name {get;set;}
	[Export] public string description {get;set;}
	[Export] public CardCategory category {get;set;}
	[Export] public Dictionary<CardEffect, int> effects {get;set;} = new Dictionary<CardEffect, int>();
}
