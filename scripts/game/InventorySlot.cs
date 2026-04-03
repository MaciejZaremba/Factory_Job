using Godot;
using Godot.Collections;
using System;

public partial class InventorySlot : HBoxContainer
{
	[Export] public ItemCategory category {get;set;}
	private ItemDefinition _stored;
	private Label _label;
	
	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		UpdateSlotIcon();
	}
	
	public ItemDefinition stored
	{
		get => _stored;
		set
		{
			_stored = value;
			UpdateSlotIcon();
			UpdateTooltip();
			if(category == ItemCategory.Any)
			{
				if(stored != null) Inventory.Instance.storage.Add(stored);
			} else {
				if(!Inventory.Instance.equipment.ContainsKey(category)) Inventory.Instance.equipment[category] = new Array<ItemDefinition>();
				if(stored != null) Inventory.Instance.equipment[category].Add(stored);
			}
			
		}
	}
	
	public override Variant _GetDragData(Vector2 atPosition)
	{
		if(stored == null) return default;
		var dragData = new Dictionary();
		dragData["item"] = stored;
		dragData["sourceSlot"] = this;
		return dragData;
	}
	
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		var dragData = data.As<Godot.Collections.Dictionary>();
		if(dragData != null && dragData.Count != 0)
		{
			if(category == ItemCategory.Any) return true;
			if(dragData["item"].As<ItemDefinition>() != null)
			{
				return dragData["item"].As<ItemDefinition>().category == category;
			}
		}
		return false;
	}
	
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var incomingData = data.As<Godot.Collections.Dictionary>();
		var item = incomingData["item"].As<ItemDefinition>();
		var oldSlot = incomingData["sourceSlot"].As<InventorySlot>();
		
		if(item == null || oldSlot == null) return;
		
		var temp = _stored;
		_stored = item;
		oldSlot._stored = temp;
		
		InventoryManager.Instance.RefreshInventoryFromCategory(category);
		InventoryManager.Instance.RefreshInventoryFromCategory(oldSlot.category);
		
		this.UpdateTooltip();
		oldSlot.UpdateTooltip();
		
		this.UpdateSlotIcon();
		oldSlot.UpdateSlotIcon();
	}
	
	public void UpdateSlotIcon()
	{
		var icon = GetNode<PanelContainer>("PanelContainer").GetNode<TextureRect>("TextureRect");
		if(_label == null) return;
		if(category == ItemCategory.Any) _label.Visible = false;
		if(stored == null)
		{
			icon.Texture = null;
			_label.Text = $"{category.ToString()}\nEmpty";
		} else {
			icon.Texture = stored.icon;
			_label.Text = $"{category.ToString()}\n{stored.name}";
		}
	}
	
	public void UpdateTooltip()
	{
		if(stored == null)
		{
			this.TooltipText = null;
			return;
		}
		var tooltip = new System.Text.StringBuilder();
		tooltip.AppendLine(stored.name);
		tooltip.AppendLine(stored.description);
		
		if(stored.buffs != null && stored.buffs.Count > 0)
		{
			tooltip.AppendLine("Buffs:");
			foreach(var buff in stored.buffs)
			{
				tooltip.AppendLine($"{buff.name} ({buff.type.ToString()}: {buff.strength})");
			}
		}
		
		if(stored.cards != null && stored.cards.Count > 0)
		{
			tooltip.AppendLine("Cards:");
			foreach(var (card, amount) in stored.cards)
			{
				tooltip.AppendLine($"{amount}x {card.name} ({card.category})");
			}
		}
		this.TooltipText = tooltip.ToString();
	}
}
