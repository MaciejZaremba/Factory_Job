using Godot;
using System.Collections.Generic;

public partial class BattleUi : CanvasLayer
{
	public static BattleUi Instance {get;private set;}
	[Export] private HBoxContainer _playerHand;
	[Export] private HBoxContainer _enemyHand;
	[Export] private ProgressBar _playerHP;
	[Export] private ProgressBar _enemyHP;
	[Export] private Label _enemyName;
	[Export] private Button _endTurn;
	[Export] private HBoxContainer _board;
	
	public override void _Ready()
	{
		Instance = this;
		_endTurn.Pressed += OnEndTurnPressed;
	}
	
	public void UpdateUIStats(Combatant p1, Combatant p2)
	{
		if(p1.isPlayer)
		{
			_playerHP.Value = p1.playerCombatant.currentHP;
			_playerHP.MaxValue = p1.playerCombatant.maxHP;
			_enemyHP.Value = p2.enemyCurrentHP;
			_enemyHP.MaxValue = p2.enemyCombatant.maxHP;
			_enemyName.Text = p2.enemyCombatant.name;
		} else {
			_playerHP.Value = p2.playerCombatant.currentHP;
			_playerHP.MaxValue = p2.playerCombatant.maxHP;
			_enemyHP.Value = p1.enemyCurrentHP;
			_enemyHP.MaxValue = p1.enemyCombatant.maxHP;
			_enemyName.Text = p1.enemyCombatant.name;
		}
	}
	
	public void RefreshUIState(CombatPhase currentPhase, Combatant p1, Combatant p2)
	{
		_playerHP.Value = p1.isPlayer ? p1.playerCombatant.currentHP : p2.playerCombatant.currentHP;
		_enemyHP.Value = p1.isPlayer ? p2.enemyCurrentHP : p1.enemyCurrentHP;
		
		Combatant attacker = CombatManager.Instance.attacker;
		bool isPlayerTurn = attacker.isPlayer;
		_endTurn.Disabled = !isPlayerTurn;
		_endTurn.Text = isPlayerTurn ? "End Phase" : "Enemy Turn...";
	}
	
	public void UpdatePlayerHand(List<CardDefinition> hand)
	{
		foreach (Node child in _playerHand.GetChildren())
		{
			child.QueueFree();
		}
		foreach(var card in hand)
		{
			var cardScene = GD.Load<PackedScene>("res://scenes/game/card.tscn").Instantiate<Card>();
			_playerHand.AddChild(cardScene);
			cardScene.Setup(card);
		}
	}
	
	public void UpdateEnemyHand(List<CardDefinition> hand)
	{
		foreach (Node child in _enemyHand.GetChildren())
		{
			child.QueueFree();
		}
		foreach(var card in hand)
		{
			var cardScene = GD.Load<PackedScene>("res://scenes/game/card.tscn").Instantiate<Card>();
			_enemyHand.AddChild(cardScene);
			cardScene.Setup(card);
			cardScene.SetFaceDown(true);
		}
	}
	
	public void VisualPlayCard(CardDefinition card)
	{
		var cardScene = GD.Load<PackedScene>("res://scenes/game/card.tscn").Instantiate<Card>();
		_board.AddChild(cardScene);
		cardScene.Setup(card);
	}
	
	public void ClearBoard()
	{
		foreach (Node child in _board.GetChildren())
		{
			child.QueueFree();
		}
	}
	
	public void OnEndTurnPressed()
	{
		CombatManager.Instance.AdvancePhase();
	}
	
	public void EnableEndTurnButton(bool enabled)
	{
		_endTurn.Disabled = !enabled;
		_endTurn.Text = enabled ? "End Turn" : "Enemy Turn...";
	}
}
