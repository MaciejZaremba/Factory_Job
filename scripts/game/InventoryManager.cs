using Godot;
using System;

public partial class InventoryManager : Node
{
	private GridContainer _equipment;
	private GridContainer _storage;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_equipment = GetNode<GridContainer>("/root/TestRoom/Inventory/Control/HBoxContainer/Equipment");
		_storage = GetNode<GridContainer>("/root/TestRoom/Inventory/Control/HBoxContainer/Storage");
	}
	
	public void RefreshUI()
	{
		foreach(var slot in _equipment.GetChildren())
		{
			//slot.
		}
	}
}
