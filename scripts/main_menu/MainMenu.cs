using Godot;
using System;

public partial class MainMenu : Node3D
{
	private CanvasLayer _canvas;
	private VBoxContainer _menu;
	private VBoxContainer _settings;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_canvas = GetNode<CanvasLayer>("CanvasLayer");
		_menu = _canvas.GetNode<VBoxContainer>("Menu");
		_settings = _canvas.GetNode<VBoxContainer>("Settings");
		
		_menu.Visible = true;
		_settings.Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void OnStartPressed()
	{
		GD.Print("MainMenu: Start Pressed.");
		GetTree().ChangeSceneToFile("res://scenes/test/test_room.tscn");
	}
	
	public void OnSettingsPressed()
	{
		GD.Print("MainMenu: Settings Pressed.");
		_menu.Visible = false;
		_settings.Visible = true;
	}
	
	public void OnExitPressed()
	{
		GD.Print("MainMenu: Exit Pressed.");
		GetTree().Quit();
	}
	
}
