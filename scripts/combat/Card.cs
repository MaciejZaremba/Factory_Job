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
		_img = GetNode<ColorRect>("TextureRect");
		_name = GetNode<VBoxContainer>("VBoxContainer").GetNode<Label>("Name");
		_description = GetNode<VBoxContainer>("VBoxContainer").GetNode<Label>("Description");
		_img.Color = Colors.Black;
	}
	
	public void Setup(CardDefinition data)
	{
		_data = data;
		_name.Text = data.name;
		SetDesctiption();
		_description.Text = data.description;
	}
	
	public void SetDesctiption()
	{
		_data.description = "Unmodified Effects:\n";
		foreach(var (effect, amount) in _data.effects)
		{
			switch(effect)
			{
				case CardEffect.Damage:
					_data.description += $"Attack: {amount}\n";
					break;
				case CardEffect.Parry:
					_data.description += $"Parry: {amount}\n";
					break;
				case CardEffect.Heal:
					_data.description += $"Heal: {amount}\n";
					break;
				case CardEffect.DrawEnemy:
					_data.description += $"Enemy Draw: {amount}\n";
					break;
			}
		}
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
