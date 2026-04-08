using Godot;
using System;

public partial class EventPopup : CanvasLayer
{
	private VBoxContainer eventContainer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		eventContainer = GetNode<Control>("Control").GetNode<CenterContainer>("CenterContainer")
			.GetNode<VBoxContainer>("VBoxContainer");
		EventManager.Instance.PopulateEvent += PopulateEvent;
	}
	
	public void OnEventResolution(EventOutcome choice)
	{
		GD.Print($"OnEventResolution: Option chosen: {choice}");
		EventManager.Instance.EventResolution(choice);
	}
	
	public void PopulateEvent(EventDefinition eventData)
	{
		GD.Print("PopulateEvent: Trying to populate event...");
		eventContainer.GetNode<RichTextLabel>("Description").Text = eventData.description;
		var buttonContainer = eventContainer.GetNode<VBoxContainer>("Buttons");
		foreach(Node child in buttonContainer.GetChildren()) child.QueueFree();
		for(int i = 0; i<eventData.outcome.Count; i++)
		{
			var button = new Button();
			button.Text = eventData.outcomeTitle[i];
			var outcome = eventData.outcome[i];
			button.Pressed += () => OnEventResolution(outcome);
			button.Visible = true;
			buttonContainer.AddChild(button);
		}
		GD.Print($"PopulateEvent: Button amount: {buttonContainer.GetChildren().Count}");
	}
	
	public override void _ExitTree()
	{
		EventManager.Instance.PopulateEvent -= PopulateEvent;
	}
	
}
