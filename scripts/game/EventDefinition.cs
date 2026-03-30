using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EventDefinition : Resource
{
	[Export] public string title {get; set;} = "";
	[Export] public string description {get;set;} = "";
	[Export] public Array<EventOutcome> outcome {get;set;} = new();
	[Export] public Array<string> outcomeTitle {get;set;} = new();
	[Export] public float weight {get;set;} = 1f;
	[Export] public bool canFire {get;set;} = true;
	[Export] public bool unique {get;set;} = false;
}
