using Godot;

public partial class WinScreen : Control
{
	public void OnWinPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/menus/main_menu.tscn");
	}
}
