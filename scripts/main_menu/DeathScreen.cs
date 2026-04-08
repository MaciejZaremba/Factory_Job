using Godot;

public partial class DeathScreen : Control
{
	public void OnDeathPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/menus/main_menu.tscn");
	}
}
