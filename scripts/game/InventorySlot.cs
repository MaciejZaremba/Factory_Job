using Godot;

public partial class InventorySlot : PanelContainer
{
	[Export] public ItemCategory category {get;set;}
	[Export] public string stored {get;set;}
	
	public override Variant _GetDragData(Vector2 atPosition)
	{
		return stored;
	}
	
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		return data.VariantType == Variant.Type.String && stored.Equals(data.AsString());
	}
}
