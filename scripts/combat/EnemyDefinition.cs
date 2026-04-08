using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EnemyDefinition : Resource
{
	[Export] public string name {get;set;} = "";
	[Export] public int maxHP {get;set;}
	[Export] public int cardDraw {get;set;}
	[Export] public int cardAttackStrength {get;set;}
	[Export] public int cardDefenseStrength {get;set;}
	[Export] public int cardUtilityStrength {get;set;}
	[Export] public Array<CardDefinition> cards {get;set;} = new();
	[Export] public Array<string> tags {get;set;} = new();
}
