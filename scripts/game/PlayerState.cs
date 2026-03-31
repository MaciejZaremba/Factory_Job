using Godot;

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
	
	public override void _Ready()
	{
		Instance = this;
	}
}
