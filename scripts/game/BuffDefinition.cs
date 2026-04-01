using Godot;

[GlobalClass]
public partial class BuffDefinition : Resource
{
	[Export] public string name {get;set;}
	[Export] public BuffCategory type {get;set;}
	[Export] public float strength {get;set;}
}
