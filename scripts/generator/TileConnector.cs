using Godot;

[GlobalClass]
public partial class TileConnector : Resource
{
	[Export] public ConnectorDirection direction {get; set;}
	[Export] public ConnectorLevel level {get; set;}
}
