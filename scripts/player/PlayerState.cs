using Godot;
using Godot.Collections;

public partial class PlayerState : Node
{
	public static PlayerState Instance {get;private set;}
	[Export] public int maxHP {get;set;}
	[Export] public int currentHP {get;set;}
	[Export] public int cardDraw {get;set;}
	[Export] public int cardRetention {get;set;}
	[Export] public float movementSpeed {get;set;}
	[Export] public float consumableRetention {get;set;}
	[Export] public int cardAttackStrength {get;set;}
	[Export] public int cardDefenseStrength {get;set;}
	[Export] public int cardUtilityStrength {get;set;}
	[Export] public Array<CardDefinition> deck {get;set;} = new();
	[Signal] public delegate void StatsChangedEventHandler();
	
	public override void _Ready()
	{
		Instance = this;
	}
	
	public void emitStatsChanged()
	{
		EmitSignal(SignalName.StatsChanged);
	}
}
