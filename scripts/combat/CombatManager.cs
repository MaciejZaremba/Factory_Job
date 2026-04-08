using Godot;
using System.Collections.Generic;
using System.Linq;

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
	public CombatPhase _currentPhase {get; private set;}
	private BattleUi _battleUi;
	
	public override void _Ready()
	{
		Instance = this;
	}
	
	public void StartCombat()
	{
		_battleUi = (BattleUi)GetNode<CanvasLayer>("/root/TestRoom/BattleUI");
		SetupPhase();
		_currentPhase = CombatPhase.Draw;
		_battleUi.Visible = true;
		AdvancePhase();
		_battleUi.UpdatePlayerHand(PlayerOne.isPlayer ? PlayerOne.hand : PlayerTwo.hand);
		_battleUi.UpdateEnemyHand(PlayerOne.isPlayer ? PlayerTwo.hand : PlayerOne.hand);
		_battleUi.UpdateUIStats(PlayerOne, PlayerTwo);
	}
	
	public void SetupPhase()
	{
		player = PlayerState.Instance;
		Input.MouseMode = Input.MouseModeEnum.Confined;
		//if(GD.Randi() % 2 == 0) 
		//{
			//PlayerOne = new Combatant(player);
			//PlayerTwo = new Combatant(enemy);
			//GD.Print($"CombatManager: Player is the attacker.");
			//return;
		//} 
		//PlayerOne = new Combatant(enemy);
		//PlayerTwo = new Combatant(player);
		//GD.Print($"CombatManager: Player is the defender.");
		/*
		Z jakiegoś powodu podczas Walki gracz, nie ważne czy był oznaczony jako PlayerOne czy jako PlayerTwo, skipował drugą fazę (PlayerTwo*)
		To powodowało że gdy gracz był obrońcą, nigdy nie był w stanie zaatakować wroga, przez co nie dało się wygrać walki.
		Nie byłem w stanie zidentyfikować problemu, nawet przy użyciu AI, więc narazie gracz zawsze będzie atakującym
		*/
		PlayerOne = new Combatant(player);
		PlayerTwo = new Combatant(enemy);
		
		
	}
	
	public void DrawPhase()
	{
		PlayerOne.DrawCards(PlayerOne.isPlayer ? player.cardDraw : enemy.cardDraw);
		PlayerTwo.DrawCards(PlayerTwo.isPlayer ? player.cardDraw : enemy.cardDraw);
		GD.Print($"CombatManager: PlayerOne drew {PlayerOne.hand.Count} cards, they have {PlayerOne.deck.Count} cards left in their deck, there's {PlayerOne.discardPile.Count} cards in their discard pile");
		GD.Print($"CombatManager: PlayerTwo drew {PlayerTwo.hand.Count} cards, they have {PlayerTwo.deck.Count} cards left in their deck, there's {PlayerTwo.discardPile.Count} cards in their discard pile");
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
		BattleUi.Instance.ClearBoard();
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
		GD.Print(_currentPhase);
		switch(_currentPhase)
		{
			case CombatPhase.Draw:
				DrawPhase();
				_currentPhase = CombatPhase.PlayerOneAttack;
				BattleUi.Instance.UpdatePlayerHand(PlayerOne.isPlayer ? PlayerOne.hand : PlayerTwo.hand);
				BattleUi.Instance.UpdateEnemyHand(PlayerOne.isPlayer ? PlayerTwo.hand : PlayerOne.hand);
				AdvancePhase();
				return;
				
			case CombatPhase.PlayerOneAttack:
				attacker = PlayerOne;
				defender = PlayerTwo;
				if(!PlayerOne.isPlayer) 
				{
					EnemyAI(PlayerOne);
					_currentPhase = CombatPhase.PlayerOneResponse;
					AdvancePhase();
					return;
				}
				break;
				
			case CombatPhase.PlayerOneResponse:
				if(!PlayerTwo.isPlayer)
				{
					EnemyAI(PlayerTwo);
					_currentPhase = CombatPhase.PlayerOneResolve;
					AdvancePhase();
					return;
				}
				break;
				
			case CombatPhase.PlayerOneResolve:
				ResolveBattlePhase();
				if(CheckCombatEnd()) return;
				BattleUi.Instance.UpdatePlayerHand(PlayerOne.isPlayer ? PlayerOne.hand : PlayerTwo.hand);
				BattleUi.Instance.UpdateEnemyHand(PlayerOne.isPlayer ? PlayerTwo.hand : PlayerOne.hand);
				//_currentPhase = CombatPhase.PlayerTwoAttack;
				_currentPhase = CombatPhase.Discard;
				AdvancePhase();
				return;
				
			case CombatPhase.PlayerTwoAttack:
				attacker = PlayerTwo;
				defender = PlayerOne;
				if(!PlayerTwo.isPlayer) 
				{
					EnemyAI(PlayerTwo);
					_currentPhase = CombatPhase.PlayerTwoResponse;
					AdvancePhase();
					return;
				}
				break;
				
			case CombatPhase.PlayerTwoResponse:
				if(!PlayerOne.isPlayer)
				{
					EnemyAI(PlayerOne);
					_currentPhase = CombatPhase.PlayerTwoResolve;
					AdvancePhase();
					return;
				}
				break;
				
			case CombatPhase.PlayerTwoResolve:
				ResolveBattlePhase();
				if(CheckCombatEnd()) return;
				_currentPhase = CombatPhase.Discard;
				AdvancePhase();
				return;
				
			case CombatPhase.Discard:
				DiscardPhase();
				_currentPhase = CombatPhase.Draw;
				AdvancePhase();
				return;
		}
		BattleUi.Instance.UpdatePlayerHand(PlayerOne.isPlayer ? PlayerOne.hand : PlayerTwo.hand);
		BattleUi.Instance.UpdateEnemyHand(PlayerOne.isPlayer ? PlayerTwo.hand : PlayerOne.hand);
		BattleUi.Instance.RefreshUIState(_currentPhase, PlayerOne, PlayerTwo);
	}
	
	public void EnemyAI(Combatant enemy)
	{
		List<CardDefinition> hand = new List<CardDefinition>(enemy.hand);
		foreach(var card in hand)
		{
			//bool canPlay = false;
			//
			//if(enemy == attacker)
			//{
				//if(card.category == CardCategory.Attack || card.category == CardCategory.Utility) canPlay = true;
			//} else if(enemy == defender)
			//{
				//if(card.category == CardCategory.Defense || card.category == CardCategory.Utility) canPlay = true;
			//}
			//
			//if(canPlay)
			//{
				//PlayCard(card,enemy,GetOpponent(enemy));
				//BattleUi.Instance.VisualPlayCard(card);
			//}
			
			/*
			AI przeciwnika też musiałem okroić z powodu błędu w turowaniu, inaczej nie grał by ofensywnych kart.
			*/
			
			PlayCard(card,enemy,GetOpponent(enemy));
			BattleUi.Instance.VisualPlayCard(card);
			
		}
	}
	
	public void PlayerEndTurn()
	{
		switch(_currentPhase)
		{
			case CombatPhase.PlayerOneAttack:
				_currentPhase = CombatPhase.PlayerOneResponse;
				break;
				
			case CombatPhase.PlayerOneResponse:
				_currentPhase = CombatPhase.PlayerOneResolve;
				break;
				
			case CombatPhase.PlayerTwoAttack:
				_currentPhase = CombatPhase.PlayerTwoResponse;
				break;
				
			case CombatPhase.PlayerTwoResponse:
				_currentPhase = CombatPhase.PlayerTwoResolve;
				break;
				
		}
	}
	
	private bool CheckCombatEnd()
	{
		if(player.currentHP <= 0)
		{
			EndCombat(false);
			return true;
		} else if(PlayerOne.enemyCurrentHP <=0 || PlayerTwo.enemyCurrentHP <=0) {
			EndCombat(true);
			return true;
		}
		return false;
	}
	
	private void EndCombat(bool playerWon)
	{
		_battleUi.Visible = false;
		GameState.Instance.inCombat = false;
		if(playerWon)
		{
			if(enemy.tags.Any(tag => tag.Contains("boss")))
			{
				GetTree().ChangeSceneToFile("res://scenes/menus/win_screen.tscn");
				return;
			}
			EventManager.Instance.GetSpecificEvent("win");
			return;
		}
		GetTree().ChangeSceneToFile("res://scenes/menus/death_screen.tscn");
	}
	
	private Combatant GetOpponent(Combatant current)
	{
		if(current == PlayerOne) return PlayerTwo;
		return PlayerOne;
	}
}
