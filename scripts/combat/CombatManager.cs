using Godot;
using System.Collections.Generic;

public partial class CombatManager : Node
{
	public static CombatManager Instance {get; private set;}
	public Combatant PlayerOne;
	public Combatant PlayerTwo;
	public EnemyDefinition enemy;
	private PlayerState player;
	private List<(CardDefinition card, Combatant source, Combatant target)> _attackList = new();
	private List<(CardDefinition card, Combatant source, Combatant target)> _defenseList = new();
	public Combatant attacker;
	public Combatant defender;
	private CombatPhase _currentPhase;
	private BattleUi _battleUi;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}
	
	public void StartCombat()
	{
		_battleUi = (BattleUi)GetNode<CanvasLayer>("/root/TestRoom/BattleUI");
		SetupPhase();
		_currentPhase = CombatPhase.Draw;
		_battleUi.Show();
		AdvancePhase();
		_battleUi.UpdateUIStats(PlayerOne,PlayerTwo);
		_battleUi.UpdatePlayerHand(PlayerOne.isPlayer ? PlayerOne.hand : PlayerTwo.hand);
		_battleUi.UpdateEnemyHand(PlayerOne.isPlayer ? PlayerTwo.hand : PlayerOne.hand);
	}
	
	public void SetupPhase()
	{
		player = PlayerState.Instance;
		if(GD.Randi() % 2 == 0) 
		{
			PlayerOne = new Combatant(player);
			PlayerTwo = new Combatant(enemy);
			return;
		} 
		PlayerOne = new Combatant(enemy);
		PlayerTwo = new Combatant(player);
	}
	
	public void DrawPhase()
	{
		PlayerOne.DrawCards(PlayerOne.isPlayer ? player.cardDraw : enemy.cardDraw);
		PlayerTwo.DrawCards(!PlayerOne.isPlayer ? player.cardDraw : enemy.cardDraw);
	}
	
	public void PlayCard(CardDefinition card, Combatant source, Combatant target)
	{
		if(card.category == CardCategory.Utility) ResolveCard(card,source,target);
		else if (card.category == CardCategory.Defense) _defenseList.Add((card,source,target));
		else if (card.category == CardCategory.Attack) _attackList.Add((card,source,target));
		source.hand.Remove(card);
		source.discardPile.Add(card);
	}
	
	public void ResolveBattlePhase()
	{
		foreach(var defense in _defenseList)
		{
			ResolveCard(defense.card, defense.source, defense.target);
		}
		_defenseList.Clear();
		
		foreach(var attack in _attackList)
		{
			ResolveCard(attack.card, attack.source, attack.target);
		}
		_attackList.Clear();
		
		PlayerOne.currentDefense = 0;
		PlayerTwo.currentDefense = 0;
	}
	
	public void DiscardPhase()
	{
		PlayerOne.DiscardCards(PlayerOne.hand.Count);
		PlayerTwo.DiscardCards(PlayerTwo.hand.Count);
	}
	
	public void ResolveCard(CardDefinition card, Combatant source, Combatant target)
	{
		int strengthBonus = source.GetCardStrength(card.category);
		
		foreach(var (effect, baseValue) in card.effects)
		{
			int trueValue = baseValue + strengthBonus;
			switch(effect)
			{
				case CardEffect.Damage:
					target.TakeDamage(trueValue);
					break;
				case CardEffect.Parry:
					source.AddDefense(trueValue);
					break;
				case CardEffect.Draw:
					source.DrawCards(trueValue);
					break;
				case CardEffect.DrawEnemy:
					target.DrawCards(trueValue);
					break;
				case CardEffect.Discard:
					source.DiscardCards(trueValue);
					break;
				case CardEffect.DiscardEnemy:
					target.DiscardCards(trueValue);
					break;
				case CardEffect.Heal:
					source.Heal(trueValue);
					break;
			}
		}
	}
	
	public void AdvancePhase()
	{
		BattleUi.Instance.ClearBoard();
		GD.Print(_currentPhase);
		switch(_currentPhase)
		{
			case CombatPhase.Draw:
				DrawPhase();
				attacker = PlayerOne;
				defender = PlayerTwo;
				_currentPhase = CombatPhase.PlayerOneAttack;
				BattleUi.Instance.UpdatePlayerHand(PlayerOne.isPlayer ? PlayerOne.hand : PlayerTwo.hand);
				BattleUi.Instance.UpdatePlayerHand(!PlayerOne.isPlayer ? PlayerOne.hand : PlayerTwo.hand);
				if(!attacker.isPlayer) AdvancePhase();
				break;
			case CombatPhase.PlayerOneAttack:
				if(!PlayerTwo.isPlayer) EnemyAI(PlayerTwo);
				ResolveBattlePhase();
				if(CheckCombatEnd()) return;
				attacker = PlayerTwo;
				defender = PlayerOne;
				_currentPhase = CombatPhase.PlayerTwoAttack;
				if(!attacker.isPlayer) AdvancePhase();
				break;
			case CombatPhase.PlayerTwoAttack:
				if(!PlayerOne.isPlayer) EnemyAI(PlayerOne);
				ResolveBattlePhase();
				if(CheckCombatEnd()) return;
				_currentPhase = CombatPhase.Discard;
				AdvancePhase();
				break;
			case CombatPhase.Discard:
				DiscardPhase();
				_currentPhase = CombatPhase.Draw;
				AdvancePhase();
				break;
		}
		BattleUi.Instance.RefreshUIState(_currentPhase, PlayerOne, PlayerTwo);
	}
	
	public void EnemyAI(Combatant enemy)
	{
		List<CardDefinition> hand = new List<CardDefinition>(enemy.hand);
		foreach(var card in hand)
		{
			bool canPlay = false;
			
			if(enemy == attacker)
			{
				if(card.category == CardCategory.Attack || card.category == CardCategory.Utility) canPlay = true;
			} else if(enemy == defender)
			{
				if(card.category == CardCategory.Defense || card.category == CardCategory.Utility) canPlay = true;
			}
			
			if(canPlay)
			{
				PlayCard(card,enemy,GetOpponent(enemy));
				BattleUi.Instance.VisualPlayCard(card);
			}
		}
	}
	
	private bool CheckCombatEnd()
	{
		bool playerOneDead = PlayerOne.isPlayer ? player.currentHP <= 0 : PlayerOne.enemyCurrentHP <=0;
		bool playerTwoDead = PlayerTwo.isPlayer ? player.currentHP <= 0 : PlayerTwo.enemyCurrentHP <=0;
		if(playerOneDead || playerTwoDead)
		{
			return true;
		}
		return false;
	}
	
	private Combatant GetOpponent(Combatant current)
	{
		if(current == PlayerOne) return PlayerTwo;
		return PlayerOne;
	}
}
