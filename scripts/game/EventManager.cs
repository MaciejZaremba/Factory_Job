using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class EventManager : Node
{
	[Signal] public delegate void PopulateEventEventHandler(EventDefinition eventData);
	public static EventManager Instance {get; private set;}
	private RandomNumberGenerator _rand;
	private CanvasLayer _eventPopup;
	private static string EventDataPath = "res://data/events/";
	private List<EventDefinition> tileEvents = new();
	private List<EventDefinition> walkwayEvents = new();
	private List<EventDefinition> stairEvents = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("EventManager: Ready() entered.");
		Instance = this;
		tileEvents = loadEvents("tile_events");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void FloorGenerated(RandomNumberGenerator seed)
	{
		_rand = seed;
		GD.Print("EventManager: RNG seeded.");
	}
	
	public void TileEntered(CanvasLayer _eventPopup, string _tileType)
	{
		if(GameState.Instance.inEvent) return;
		GD.Print("EventManager: Tile Entered.");
		EventDefinition chosenEvent = null;
		List<EventDefinition> eventList = new();
		this._eventPopup = _eventPopup;
		float eventWeight= 0f;
		switch(_tileType)
		{
			case "tile":
			{
				eventList = tileEvents;
				eventWeight = eventList.Sum(eve => eve.weight);
				break;
			}
			case "walkway":
			{
				eventList = tileEvents;
				eventWeight = eventList.Sum(eve => eve.weight);
				break;
			}
			case "stairs":
			{
				eventList = tileEvents;
				eventWeight = eventList.Sum(eve => eve.weight);
				break;
			}
		}
		if(!GameState.Instance.startEvent)
		{
			chosenEvent = tileEvents.FirstOrDefault(eve => eve.title == "start");
			EmitSignal(SignalName.PopulateEvent, chosenEvent);
			_eventPopup.Visible = true;
			Input.MouseMode = Input.MouseModeEnum.Confined;
			GameState.Instance.inEvent = true;
			GameState.Instance.startEvent = true;
			return;
		}
		var _event = _rand.RandfRange(0f,eventWeight * 1.5f);
		GD.Print($"EventManager: Dice roll: {_event}");
		foreach(var eve in eventList)
		{
			_event -= eve.weight;
			if(_event <= 0)
			{
				chosenEvent = eve;
				break;
			}
		}
		if(chosenEvent == null) 
		{
			GD.Print("EventManager: Event skipped.");
			return;
		}
		GD.Print($"EventManager: Event chosen: {chosenEvent.title}");
		EmitSignal(SignalName.PopulateEvent, chosenEvent);
		_eventPopup.Visible = true;
		Input.MouseMode = Input.MouseModeEnum.Confined;
		GameState.Instance.inEvent = true;
	}
	
	public void EventResolution(EventOutcome option)
	{
		GD.Print($"EventResolution: Trying to resolve event...");
		_eventPopup.Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		GameState.Instance.inEvent = false;
		GD.Print($"EventResolution: Event resolved with option: {option}.");
	}
	
	private List<EventDefinition> loadEvents(string directory)
	{
		GD.Print("EventManager: Trying to open path...");
		var dataPath = EventDataPath + "/" + directory + "/";
		using var dir = DirAccess.Open(dataPath);
		if(dir == null)
		{
			GD.PrintErr($"EventManager: Could not open tile data path: {dataPath}");
			return null;
		}
		
		dir.ListDirBegin();
		List<EventDefinition> eventList = new();
		string fileName = dir.GetNext();
		while(fileName != "")
		{
			if(fileName.EndsWith(".tres"))
			{
				var definition = GD.Load<EventDefinition>(dataPath + fileName);
				if(definition != null) eventList.Add(definition);
			}
			fileName = dir.GetNext();
		}
		
		dir.ListDirEnd();
		GD.Print($"EventManager: Registered {eventList.Count} events.");
		return eventList;
	}
}
