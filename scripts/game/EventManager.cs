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
	private static string EnemyDataPath = "res://data/enemies/";
	private List<EventDefinition> tileEvents = new();
	private List<EventDefinition> walkwayEvents = new();
	private List<EventDefinition> stairEvents = new();
	private List<EventDefinition> endEvents = new();
	private List<EnemyDefinition> enemies = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("EventManager: Ready() entered.");
		Instance = this;
		tileEvents = loadEvents("tile_events");
		endEvents = loadEvents("end_events");
		enemies = loadEnemies();
	}
	
	public void FloorGenerated(RandomNumberGenerator seed)
	{
		_rand = seed;
		GD.Print("EventManager: RNG seeded.");
	}
	
	public void TileEntered(CanvasLayer _eventPopup, string _tileType)
	{
		if(GameState.Instance.inEvent) return;
		this._eventPopup = _eventPopup;
		GD.Print("EventManager: Tile Entered.");
		EventDefinition chosenEvent = null;
		List<EventDefinition> eventList = new();
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
				eventList = walkwayEvents;
				eventWeight = eventList.Sum(eve => eve.weight);
				break;
			}
			case "stairs":
			{
				eventList = stairEvents;
				eventWeight = eventList.Sum(eve => eve.weight);
				break;
			}
			case "end":
			{
				eventList = endEvents;
				eventWeight = eventList.Sum(eve => eve.weight);
				break;
			}
		}
		if (!GodotObject.IsInstanceValid(_eventPopup))
		{
			GD.PrintErr("EventManager: Popup address is invalid.");
			return;
		}
		if(!GameState.Instance.startEvent)
		{
			chosenEvent = tileEvents.FirstOrDefault(eve => eve.title == "start");
			EmitSignal(SignalName.PopulateEvent, chosenEvent);
			_eventPopup.Visible = true;
			Input.MouseMode = Input.MouseModeEnum.Confined;
			GameState.Instance.inEvent = true;
			GameState.Instance.startEvent = true;
			GD.Print($"EventManager: Start event completed: {GameState.Instance.startEvent}");
			return;
		}
		
		float _event;
		
		if(_tileType == "end")
		{
			_event = _rand.RandfRange(0f,eventWeight);
		} else {
			_event = _rand.RandfRange(0f,eventWeight * 1.5f);
		}
		 
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
		switch(option)
		{
			case EventOutcome.StartCombat:
				CombatManager.Instance.enemy = GetEnemy(false);
				GameState.Instance.inEvent = false;
				GameState.Instance.inCombat = true;
				CombatManager.Instance.StartCombat();
				break;
			case EventOutcome.StartBossCombat:
				CombatManager.Instance.enemy = GetEnemy(true);
				GD.Print($"EventResolution: Starting Boss Fight");
				GameState.Instance.inEvent = false;
				GameState.Instance.inCombat = true;
				CombatManager.Instance.StartCombat();
				break;
			case EventOutcome.AddItem:
				var item = UIManager.Instance.GetRandomItem();
				UIManager.Instance.AddItemToInventory(item);
				break;
			case EventOutcome.RemoveItem:
				UIManager.Instance.RemoveRandomItemFromInventory();
				break;
			case EventOutcome.DealDamage:
				PlayerState.Instance.currentHP -= 3;
				PlayerState.Instance.emitStatsChanged();
				break;
			case EventOutcome.HealDamage:
				PlayerState.Instance.currentHP = Mathf.Min(PlayerState.Instance.maxHP, PlayerState.Instance.currentHP + 3);
				PlayerState.Instance.emitStatsChanged(); 
				break;
		}
		GD.Print($"EventResolution: Event resolved with option: {option}.");
	}
	
	public EnemyDefinition GetEnemy(bool isBoss)
	{
		if(isBoss)
		{
			return enemies.FirstOrDefault(e => e.name == "Boss");
		} else {
			return enemies.FirstOrDefault(e => e.name == "Guard");
		}
	}
	
	public void GetSpecificEvent(string name)
	{
		EventDefinition _event = tileEvents.FirstOrDefault(e => e.title == name);
		if(_event == null)
		{
			GD.PrintErr($"EventManager: Could not find event with title {name}");
			return;
		}
		GD.Print($"EventManager: Event chosen: {_event.title}");
		EmitSignal(SignalName.PopulateEvent, _event);
		_eventPopup.Visible = true;
		GameState.Instance.inEvent = true;
	}
	
	public void Reset()
	{
		_eventPopup = null;
	}
	
	private List<EventDefinition> loadEvents(string directory)
	{
		GD.Print("EventManager: Trying to open path...");
		var dataPath = EventDataPath + "/" + directory + "/";
		using var dir = DirAccess.Open(dataPath);
		if(dir == null)
		{
			GD.PrintErr($"EventManager: Could not open event data path: {dataPath}");
			return null;
		}
		
		dir.ListDirBegin();
		List<EventDefinition> eventList = new();
		string fileName = dir.GetNext();
		while(fileName != "")
		{
			if(fileName.EndsWith(".tres") || fileName.EndsWith(".tres.remap"))
			{
				string cleanPath = dataPath + fileName.Replace(".remap", "");
				var definition = GD.Load<EventDefinition>(cleanPath);
				if(definition != null) eventList.Add(definition);
			}
			fileName = dir.GetNext();
		}
		
		dir.ListDirEnd();
		GD.Print($"EventManager: Registered {eventList.Count} events.");
		return eventList;
	}
	
	private List<EnemyDefinition> loadEnemies()
	{
		GD.Print("EventManager: Trying to open path...");
		var dataPath = EnemyDataPath;
		using var dir = DirAccess.Open(dataPath);
		if(dir == null)
		{
			GD.PrintErr($"EventManager: Could not open enemy data path: {dataPath}");
			return null;
		}
		
		dir.ListDirBegin();
		List<EnemyDefinition> enemyList = new();
		string fileName = dir.GetNext();
		while(fileName != "")
		{
			if(fileName.EndsWith(".tres") || fileName.EndsWith(".tres.remap"))
			{
				string cleanPath = dataPath + fileName.Replace(".remap", "");
				var definition = GD.Load<EnemyDefinition>(cleanPath);
				if(definition != null) enemyList.Add(definition);
			}
			fileName = dir.GetNext();
		}
		
		dir.ListDirEnd();
		GD.Print($"EventManager: Registered {enemyList.Count} enemies.");
		return enemyList;
	}
}
