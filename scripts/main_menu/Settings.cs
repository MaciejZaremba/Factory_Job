using Godot;
using System;

public partial class Settings : VBoxContainer
{
	private Vector2I _resolutionValue;
	private HBoxContainer _resolutionSettings;
	private bool _fullscreen;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_resolutionSettings = GetNode<HBoxContainer>("Resolution");
		_fullscreen = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen;
		GetNode<CheckButton>("Fullscreen").ButtonPressed = _fullscreen;
		//_resolutionSettings.GetNode<OptionButton>("Resolution").Selected = 
		_resolutionValue = DisplayServer.WindowGetSize();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void OnFullscreenToggled(bool toggled)
	{
		if(!toggled)
		{
			GD.Print("Settings: Window Mode.");
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		} else {
			GD.Print("Settings: Fullscreen Mode.");
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}
	}
	
	public void OnResolutionSelected(int resolution)
	{
		GD.Print("Settings: Resolution Selected: ");
		switch(resolution)
		{
			case 0:
			{
				_resolutionValue.X = 3840;
				_resolutionValue.Y = 2160;
				DisplayServer.WindowSetSize(_resolutionValue);
				GD.Print("3840x2160");
				break;
			}
			case 1:
			{
				_resolutionValue.X = 2560;
				_resolutionValue.Y = 1440;
				DisplayServer.WindowSetSize(_resolutionValue);
				GD.Print("2560x1440");
				break;
			}
			case 2:
			{
				_resolutionValue.X = 1920;
				_resolutionValue.Y = 1080;
				DisplayServer.WindowSetSize(_resolutionValue);
				GD.Print("1920x1080");
				break;
			}
		}
	}
}
