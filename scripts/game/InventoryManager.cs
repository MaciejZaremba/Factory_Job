using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class InventoryManager : Node
{
	public static InventoryManager Instance {get;private set;}
	private GridContainer _equipment;
	private GridContainer _storage;
	[Signal] public delegate void InventoryAlteredEventHandler();
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}
	
	public void RefreshInventoryFromCategory(ItemCategory category)
	{
		if(_equipment == null) _equipment = GetNode<GridContainer>("/root/TestRoom/Inventory/Control/HBoxContainer/Equipment");
		if(_storage == null) _storage = GetNode<GridContainer>("/root/TestRoom/Inventory/Control/HBoxContainer/Storage");
		var storageSlots = _storage.GetChildren().OfType<InventorySlot>().ToList();
		Inventory.Instance.storage.Clear();
		foreach(var slot in storageSlots)
		{
			Inventory.Instance.storage.Add(slot.stored);
		}
		var equipSlots = new List<InventorySlot>();
		foreach(var slot in _equipment.GetChildren().OfType<InventorySlot>().ToList())
		{
			if(slot.category == category)
			{
				equipSlots.Add(slot);
			}
		}
		Inventory.Instance.equipment[category] = new Array<ItemDefinition>();
		foreach(var slot in equipSlots)
		{
			if(slot.stored != null)
			{
				Inventory.Instance.equipment[category].Add(slot.stored);
				GD.Print($"InventoryManager: equipped item to slot {slot.stored.category}");
			} 
		}
		emitInventoryAltered();
	}
	
	public void emitInventoryAltered()
	{
		EmitSignal(SignalName.InventoryAltered);
	}
}
