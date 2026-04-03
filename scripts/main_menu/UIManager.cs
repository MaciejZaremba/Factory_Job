using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class UIManager : Node
{
	private CanvasLayer _inventory;
	private List<InventorySlot> _equipment;
	private List<InventorySlot> _storage;
	public static UIManager Instance {get; private set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public override void _Input(InputEvent @event)
	{
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
}
