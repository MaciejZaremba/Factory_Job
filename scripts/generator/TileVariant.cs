using Godot;
using System.Collections.Generic;
using System.Linq;

public class TileVariant
{
	public TileDefinition  definition {get;}
	public int rotation {get;} //in multiples of 90
	public List<TileConnector> connectors {get;}
	
	public TileVariant(TileDefinition definition, int rotation)
	{
		this.definition = definition;
		this.rotation = rotation;
		this.connectors = DeriveConnectors(definition, rotation);
	}
	
	//checks avaliable connectors
	public bool HasConnectors(ConnectorDirection direction, ConnectorLevel level)
	{
		return connectors.Any(con => con.direction == direction && con.level == level);
	}
	
	// get all connectors at set rotation
	private static List<TileConnector> DeriveConnectors(TileDefinition definition, int rotation)
	{
		int steps = (rotation/90) % 4;
		var result = new List<TileConnector>();
		
		foreach(var connector in definition.connectors)
		{
			result.Add(new TileConnector 
			{
				direction = RotateDirection(connector.direction, steps),
				level = connector.level
			});
		}
		return result;
	}
	
	//self explanatory
	private static ConnectorDirection RotateDirection(ConnectorDirection direction, int steps)
	{
		ConnectorDirection[] clockwise = 
		{
			ConnectorDirection.North,
			ConnectorDirection.East,
			ConnectorDirection.South,
			ConnectorDirection.West,
		};
		
		int currentIndex = System.Array.IndexOf(clockwise, direction);
		int rotatedIndex = (currentIndex + steps) % 4;
		return clockwise[rotatedIndex];
	}

}
