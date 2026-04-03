using Godot;
using Godot.Collections;

public partial class GameState : Node
{
	public static GameState Instance {get; private set;}
	[Export] public bool inEvent {get;set;}
	[Export] public bool inGame {get;set;}
	[Export] public bool inCombat {get;set;}
	[Export] public bool inInventory {get;set;}
	[Export] public bool startEvent {get;set;}
	[Export] public Dictionary<string, float> defaultStats {get;private set;} = new();
	[Export] public Array<ItemDefinition> startingItems {get;private set;} = new();
	
	public override void _Ready()
	{
		Instance = this;
		inEvent = false;
		inCombat = false;
		inInventory = false;
		startEvent = false;
		_setDefaultStats();
		_setStartingItems();
	}
	
	private void _setDefaultStats()
	{
		defaultStats.Add("maxHP", 55f);
		defaultStats.Add("currentHP", 55f);
		defaultStats.Add("cardDraw", 4f);
		defaultStats.Add("cardRetention", 0f);
		defaultStats.Add("movementSpeed", 0f);
		defaultStats.Add("consumableRetention", 0f);
		defaultStats.Add("cardAttackStrength", 0f);
		defaultStats.Add("cardDefenseStrength", 0f);
		defaultStats.Add("cardUtilityStrength", 0f);
	}
	
	private void _setStartingItems()
	{
		startingItems.Add(GD.Load<ItemDefinition>("res://data/items/cracked_amulet.tres"));
		startingItems.Add(GD.Load<ItemDefinition>("res://data/items/dart_gun.tres"));
		startingItems.Add(GD.Load<ItemDefinition>("res://data/items/iron_pipe.tres"));
		startingItems.Add(GD.Load<ItemDefinition>("res://data/items/leather_vest.tres"));
	}
}
