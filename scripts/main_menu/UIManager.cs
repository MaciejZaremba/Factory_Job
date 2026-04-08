using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class UIManager : Node
{
	private CanvasLayer _inventory;
	private List<InventorySlot> _equipment;
	private List<InventorySlot> _storage;
	private List<ItemDefinition> _itemList;
	private static string ItemDataPath = "res://data/items/";
	private RandomNumberGenerator _rand;
	public static UIManager Instance {get; private set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_itemList = loadItems();
		Instance = this;
	}
	
	public void FloorGenerated(RandomNumberGenerator seed)
	{
		_rand = seed;
		GD.Print("EventManager: RNG seeded.");
	}
	
	public override void _Input(InputEvent @event)
	{
		if(GameState.Instance.inEvent || GameState.Instance.inInventory || GameState.Instance.inCombat) return;
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && GameState.Instance.startEvent == true)
		{
			if (keyEvent.Keycode == Key.Escape)
			{
				if(_inventory == null) _inventory = GetNode<CanvasLayer>("/root/TestRoom/Inventory");
				_inventory.Visible = !_inventory.Visible;
				Input.MouseMode = _inventory.Visible ? Input.MouseModeEnum.Confined : Input.MouseModeEnum.Captured;
			}
		}
	}
	
	public void AddItemToEquipped(ItemDefinition item)
	{
		if(_inventory == null) _inventory = GetNode<CanvasLayer>("/root/TestRoom/Inventory");
		if(_equipment == null) _equipment = _inventory.GetNode<GridContainer>("Control/HBoxContainer/Equipment").GetChildren().OfType<InventorySlot>().ToList();
		
		foreach(var slot in _equipment) 
		{
			if(item.category != slot.category) continue;
			if(slot.stored != null) continue;
			slot.stored = item;
			slot.UpdateSlotIcon();
			return;
		}
		AddItemToInventory(item);
	}
	
	public void AddItemToInventory(ItemDefinition item)
	{
		if(_storage == null) _storage = _inventory.GetNode<GridContainer>("Control/HBoxContainer/Storage").GetChildren().OfType<InventorySlot>().ToList();
		foreach(var slot in _storage)
		{
			if(slot.stored != null) continue;
			slot.stored = item;
			slot.UpdateSlotIcon();
			return;
		}
		GD.Print("UIManager: Out of inventory space!");
	}
	
	public void RemoveRandomItemFromInventory()
	{
		if(_storage == null) _storage = _inventory.GetNode<GridContainer>("Control/HBoxContainer/Storage").GetChildren().OfType<InventorySlot>().ToList();
		GD.Print("UIManager: looking for items to remove.");
		List<InventorySlot> occupiedSlots = new();
		foreach(var slot in _storage)
		{
			if(slot.stored == null) continue;
			occupiedSlots.Add(slot);
		}
		GD.Print($"UIManager: Found {occupiedSlots.Count} items.");
		if(occupiedSlots == null || occupiedSlots.Count == 0)
		{
			return;
		}
		int roll = _rand.RandiRange(0, occupiedSlots.Count - 1);
		GD.Print($"UIManager: Random item removed: {occupiedSlots[roll].stored}");
		occupiedSlots[roll].stored = null;
		occupiedSlots[roll].UpdateSlotIcon();
	}
	
	public ItemDefinition GetRandomItem()
	{
		int itemCount = _itemList.Count;
		int roll = _rand.RandiRange(0, itemCount - 1);
		GD.Print($"UIManager: Randomly generated item: {_itemList[roll].name}");
		return _itemList[roll];
	}
	
	private List<ItemDefinition> loadItems()
	{
		GD.Print("UIManager: Trying to open path...");
		using var dir = DirAccess.Open(ItemDataPath);
		if(dir == null)
		{
			GD.PrintErr($"UIManager: Could not open item data path: {ItemDataPath}");
			return null;
		}
		
		dir.ListDirBegin();
		List<ItemDefinition> itemList = new();
		string fileName = dir.GetNext();
		while(fileName != "")
		{
			if(fileName.EndsWith(".tres") || fileName.EndsWith(".tres.remap"))
			{
				string cleanPath = ItemDataPath + fileName.Replace(".remap", "");
				var definition = GD.Load<ItemDefinition>(cleanPath);
				if(definition != null) itemList.Add(definition);
			}
			fileName = dir.GetNext();
		}
		
		dir.ListDirEnd();
		GD.Print($"UIManager: Registered {itemList.Count} items.");
		return itemList;
	}
	
	public void Reset()
	{
		_inventory = null;
		_equipment = null;
		_storage = null;
	}
}
