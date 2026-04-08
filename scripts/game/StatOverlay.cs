using Godot;

public partial class StatOverlay : CanvasLayer
{
	private HBoxContainer _hpContainer;
	private VBoxContainer _statsContainer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_hpContainer = GetNode<Control>("HP").GetNode<HBoxContainer>("HBoxContainer");
		_statsContainer = GetNode<Control>("Stats").GetNode<VBoxContainer>("VBoxContainer");
		
		PlayerState.Instance.StatsChanged += OnPlayerStatsChanged;
		UpdateLabels();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	private void OnPlayerStatsChanged()
	{
		UpdateLabels(); //Probably shouldn't be here but for now it'll do
	}
	
	public void UpdateLabels()
	{
		_hpContainer.GetNode<Label>("Label").Text = $"HP: {PlayerState.Instance.currentHP}/{PlayerState.Instance.maxHP}";
		_statsContainer.GetNode<Label>("cardDraw").Text = $"Card Draw: {PlayerState.Instance.cardDraw}";
		_statsContainer.GetNode<Label>("cardRetention").Text = $"Card Retention: {PlayerState.Instance.cardRetention}";
		_statsContainer.GetNode<Label>("cardAttackStrength").Text = $"Attack: {PlayerState.Instance.cardAttackStrength}";
		_statsContainer.GetNode<Label>("cardDefenseStrength").Text = $"Defense: {PlayerState.Instance.cardDefenseStrength}";
		_statsContainer.GetNode<Label>("cardUtilityStrength").Text = $"Utility: {PlayerState.Instance.cardUtilityStrength}";
		_statsContainer.GetNode<Label>("consumableRetention").Text = $"Consume chance: {(1 - PlayerState.Instance.consumableRetention):P0}";
		_statsContainer.GetNode<Label>("movementSpeed").Text = $"Speed: {PlayerState.Instance.movementSpeed:0.0}";
		
	}
	
	public override void _ExitTree()
	{
		PlayerState.Instance.StatsChanged -= OnPlayerStatsChanged;
	}
}
