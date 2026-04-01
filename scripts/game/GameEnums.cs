public enum ItemCategory
{
	Consumable = 0,
	Helmet = 1,
	Armor = 2,
	Glove = 3,
	Boots = 4,
	Weapon = 5,
	Accessory = 6,
	Treasure = 7,
	Any = 12
}

public enum EventOutcome
{
	None = 0,
	AddItem = 1,
	RemoveItem = 2,
	StartCombat = 3,
	DealDamage = 4,
	HealDamage = 5,
}

public enum BuffCategory
{
	None = 0,
	MaxHP = 1,
	CardDrawFight = 2,
	CardDrawTurn = 3,
	CardRetention = 4,
	MovementSpeed = 5,
	ConsumableRetention = 6,
	CardAttackFight = 7,
	CardDefenseFight = 8,
	CardAttackTurn = 9,
	CardDefenseTurn = 10,
	CardUtilityTurn = 11,
}
