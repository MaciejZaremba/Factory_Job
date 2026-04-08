using Godot;
using System;

public partial class Board : HBoxContainer
{
	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		//if(!(data.As<Control>() is Card card)) return false;
		//
		//var cardData = card.GetData();
		//var phase = CombatManager.Instance._currentPhase;
		//
		//bool isAttackPhase = (phase == CombatPhase.PlayerOneAttack || phase == CombatPhase.PlayerTwoAttack);
		//bool isResponsePhase = (phase == CombatPhase.PlayerOneResponse || phase == CombatPhase.PlayerTwoResponse);
		//
		//if(isAttackPhase && CombatManager.Instance.attacker.isPlayer)
			//return cardData.category == CardCategory.Attack || cardData.category == CardCategory.Utility;
			   //
		//if(isResponsePhase && CombatManager.Instance.defender.isPlayer)
			//return cardData.category == CardCategory.Defense || cardData.category == CardCategory.Utility;
		//
		//return false;
		
		/*
		Dopóki turowanie nie zostanie naprawione, ta metoda musi być uproszczona
		*/
		return (data.As<Control>() is Card);
	}
	
	public override void _DropData(Vector2 atPosition, Variant data)
	{
		Card card = (Card)data.As<Control>();
		
		if(CombatManager.Instance.PlayerOne.isPlayer)
		{
			CombatManager.Instance.PlayCard(card.GetData(), CombatManager.Instance.PlayerOne, CombatManager.Instance.PlayerTwo);
		} else {
			CombatManager.Instance.PlayCard(card.GetData(), CombatManager.Instance.PlayerTwo, CombatManager.Instance.PlayerOne);
		}
		card.GetParent().RemoveChild(card);
		AddChild(card);
	}
}
