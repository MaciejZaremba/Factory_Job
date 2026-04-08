using Godot;

public partial class Settings : VBoxContainer
{
	private Vector2I _resolutionValue;
	private HBoxContainer _resolutionSettings;
	private VBoxContainer _volumeSettings;
	private bool _fullscreen;
	private HSlider _musicSlider;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_resolutionSettings = GetNode<HBoxContainer>("Resolution");
		_fullscreen = (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen);
		GetNode<CheckButton>("Fullscreen").ButtonPressed = _fullscreen;
		_resolutionSettings.GetNode<OptionButton>("Resolution").Disabled = _fullscreen;
		_resolutionValue = DisplayServer.WindowGetSize();
		_volumeSettings = GetNode<VBoxContainer>("Volume");
		_musicSlider = _volumeSettings.GetNode<HBoxContainer>("Music").GetNode<HSlider>("MusicSlide");
		int busIndex = AudioServer.GetBusIndex("Music");
		float volumeLinear = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(busIndex));
		_musicSlider.Value = volumeLinear;
	}

	public void OnFullscreenToggled(bool toggled)
	{
		if(!toggled)
		{
			GD.Print("Settings: Window Mode.");
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
			_resolutionSettings.GetNode<OptionButton>("Resolution").Disabled = false;
		} else {
			GD.Print("Settings: Fullscreen Mode.");
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			_resolutionSettings.GetNode<OptionButton>("Resolution").Disabled = true;
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
			case 3:
			{
				_resolutionValue.X = 1440;
				_resolutionValue.Y = 900;
				DisplayServer.WindowSetSize(_resolutionValue);
				GD.Print("1440x90");
				break;
			}
			case 4:
			{
				_resolutionValue.X = 1280;
				_resolutionValue.Y = 1200;
				DisplayServer.WindowSetSize(_resolutionValue);
				GD.Print("1280x1200");
				break;
			}
			case 5:
			{
				_resolutionValue.X = 1280;
				_resolutionValue.Y = 720;
				DisplayServer.WindowSetSize(_resolutionValue);
				GD.Print("1280x720");
				break;
			}
			case 6:
			{
				_resolutionValue.X = 800;
				_resolutionValue.Y = 600;
				DisplayServer.WindowSetSize(_resolutionValue);
				GD.Print("800x600");
				break;
			}
		}
	}
	
	public void OnMusicDragEnded(bool change)
	{
		if(!change) return;
		
		int busIndex = AudioServer.GetBusIndex("Music");
		
		float volumeLinear = (float)_musicSlider.Value;
		float volumeDb = Mathf.LinearToDb(volumeLinear);
		AudioServer.SetBusVolumeDb(busIndex, volumeDb);
		
		GD.Print($"Settings:Music volume set to {volumeDb} db.");
	}
	
	public void OnSFXDragEnded(bool change)
	{
		//There is no sfx right now so this isn't implemented.
	}
	
	public void OnExitPressed()
	{
		GD.Print("MainMenu: Settings Pressed.");
		GetNode<VBoxContainer>("/root/MainMenu/CanvasLayer/Menu").Visible = true;
		GetNode<VBoxContainer>("/root/MainMenu/CanvasLayer/Settings").Visible = false;
	}
}
