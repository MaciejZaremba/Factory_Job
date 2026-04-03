using Godot;
using System;

public partial class Board : HBoxContainer
{
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		return data.As<Control>() is Card;
	}
	
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		Card card = (Card)data.As<Control>();
		CombatManager.Instance.PlayCard(card.GetData(), CombatManager.Instance.attacker, CombatManager.Instance.defender);
		card.GetParent().RemoveChild(card);
		AddChild(card);
	}
}
