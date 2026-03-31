using Godot;
using Godot.Collections;

public partial class GameState : Node
{
	public static GameState Instance {get; private set;}
	[Export] public bool inEvent {get;set;}
	[Export] public bool startEvent {get;set;}
	[Export] public Dictionary<string, float> defaultStats {get;private set;} = new();
	
	public override void _Ready()
	{
		Instance = this;
		inEvent = false;
		startEvent = false;
		_setDefaultStats();
	}
	
	private void _setDefaultStats()
	{
		defaultStats.Add("maxHP", 55f);
		defaultStats.Add("currentHP", 55f);
		defaultStats.Add("cardDraw", 6f);
		defaultStats.Add("cardRetention", 0f);
		defaultStats.Add("movementSpeed", 0f);
		defaultStats.Add("consumableRetention", 0f);
		defaultStats.Add("cardAttackStrength", 0f);
		defaultStats.Add("cardDefenseStrength", 0f);
		defaultStats.Add("cardUtilityStrength", 0f);
	}
}
