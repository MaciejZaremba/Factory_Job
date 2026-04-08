using Godot;
using System.Collections.Generic;

public class Combatant
{
	public bool isPlayer;
	public PlayerState playerCombatant;
	public EnemyDefinition enemyCombatant;
	
	public List<CardDefinition> hand = new();
	public List<CardDefinition> deck = new();
	public List<CardDefinition> discardPile = new();
	public List<CardDefinition> destroyedPile = new();
	public int currentDefense = 0;
	public int enemyCurrentHP = 999;
	public RandomNumberGenerator _rand = new();
	
	public Combatant(PlayerState player)
	{
		playerCombatant = player;
		isPlayer = true;
		_rand.Randomize();
		InitializeDeck(playerCombatant.deck);
	}
	public Combatant(EnemyDefinition enemy)
	{
		enemyCombatant = enemy;
		isPlayer = false;
		_rand.Randomize();
		enemyCurrentHP = enemy.maxHP;
		InitializeDeck(enemyCombatant.cards);
	}
	
	public void InitializeDeck(Godot.Collections.Array<CardDefinition> startingDeck)
	{
		deck = new List<CardDefinition>(startingDeck);
		ShuffleDeck();
	}
	
	public void TakeDamage(int amount)
	{
		int realDamage = amount - currentDefense;
		GD.Print($"TakeDamage: Damage amount: {amount}, defense amount: {currentDefense}, damage through defense: {realDamage}.");
		currentDefense = Mathf.Max(0, currentDefense - amount);
		if(realDamage <= 0) return;
		if(isPlayer)
		{
			playerCombatant.currentHP -= realDamage;
			playerCombatant.emitStatsChanged();
		} else {
			enemyCurrentHP -= realDamage;
		}
	}
	
	public void AddDefense(int amount)
	{
		currentDefense += amount;
	}
	
	public void Heal(int amount)
	{
		if(isPlayer)
		{
			playerCombatant.currentHP = Mathf.Min(playerCombatant.maxHP, playerCombatant.currentHP + amount);
			return;
		}
		enemyCurrentHP = Mathf.Min(enemyCombatant.maxHP, enemyCurrentHP + amount);
	}
	
	public void DiscardCards(int amount)
	{
		for(int i=0; i<amount; i++)
		{
			if(hand.Count == 0) return;
			int discard = _rand.RandiRange(0,hand.Count-1);
			discardPile.Add(hand[discard]);
			hand.RemoveAt(discard);
		}
	}
	
	public void DrawCards(int amount)
	{
		for(int i = 0; i<amount; i++)
		{
			if(deck.Count == 0)
			{
				ReshuffleDeck();
			}
			hand.Add(deck[0]);
			deck.RemoveAt(0);
			
		}
	}
	
	public void ReshuffleDeck()
	{
		deck.AddRange(discardPile);
		discardPile.Clear();
		ShuffleDeck();
	}
	
	public void ShuffleDeck()
	{
		int n = deck.Count;
		while(n>1)
		{
			n--;
			int m = _rand.RandiRange(0, n);
			CardDefinition card = deck[m];
			deck[m] = deck[n];
			deck[n] = card;
		}
	}
	
	public int GetCardStrength(CardCategory category)
	{
		if(isPlayer)
		{
			switch(category)
			{
				case CardCategory.Attack:
					return playerCombatant.cardAttackStrength;
				case CardCategory.Defense:
					return playerCombatant.cardDefenseStrength;
				case CardCategory.Utility:
					return playerCombatant.cardUtilityStrength;
				default:
					return 0;
			}
		}
		switch(category)
			{
				case CardCategory.Attack:
					return enemyCombatant.cardAttackStrength;
				case CardCategory.Defense:
					return enemyCombatant.cardDefenseStrength;
				case CardCategory.Utility:
					return enemyCombatant.cardUtilityStrength;
				default:
					return 0;
			}
	}
}
