using Godot;
using Godot.Collections;

public partial class InventorySlot : HBoxContainer
{
	[Export] public ItemCategory category {get;set;}
	[Export] public ItemDefinition stored {get;set;}
	private Label _label;
	
	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		UpdateSlotIcon();
	}
	
	public override Variant _GetDragData(Vector2 atPosition)
	{
		if(stored == null) return default;
		var dragData = new Dictionary<InventorySlot, ItemDefinition>();
		dragData.Add(this, stored);
		return dragData;
	}
	
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		var item = data.As<ItemDefinition>();
		if(item != null)
		{
			return (category == ItemCategory.Any || item.category == category);
		}
		return false;
	}
	
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		var incomingData = data.As<Godot.Collections.Dictionary<InventorySlot, ItemDefinition>>();
		foreach(var (oldSlot, item) in incomingData)
		{
			oldSlot.stored = stored;
			stored = item;
			this.UpdateSlotIcon();
			oldSlot.UpdateSlotIcon();
			break;
		}
		
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
			_label.Text = "{category.ToString()}\nstored.name";
		}
	}
}
