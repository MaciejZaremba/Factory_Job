using Godot;
using Godot.Collections;

public partial class Card : Control
{
	private CardDefinition _data;
	private Label _name;
	private ColorRect _img;
	private Label _description;
	
	public override void _Ready()
	{
		_name = GetNode<VBoxContainer>("VBoxContainer").GetNode<Label>("Name");
		_description = GetNode<VBoxContainer>("VBoxContainer").GetNode<Label>("Description");
		_img = GetNode<VBoxContainer>("VBoxContainer").GetNode<ColorRect>("TextureRect");
		_img.Color = Colors.Black;
	}
	
	public void Setup(CardDefinition data)
	{
		_data = data;
		_name.Text = data.name;
		_description.Text = data.description;
		TooltipText = data.description;
		
	}
	
	public override Variant _GetDragData(Vector2 atPosition)
	{
		var preview = new ColorRect { CustomMinimumSize = new Vector2(100,150), Color = Colors.Black};
		SetDragPreview(preview);
		return this;
	}
	
	public void SetFaceDown(bool isFaceDown)
	{
		_name.Visible = !isFaceDown;
		_description.Visible = !isFaceDown;
	}
	
	public CardDefinition GetData()
	{
		return _data;
	}
}
