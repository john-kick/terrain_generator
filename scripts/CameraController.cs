using Godot;

public partial class CameraController : Camera3D
{
	[Export] public float MoveSpeed = 20f;
	[Export] public float MouseSensitivity = 0.15f;
	[Export] public float FastMultiplier = 3f;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		Current = true;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
		}

		if (Input.MouseMode != Input.MouseModeEnum.Captured)
			return;

		if (@event is InputEventMouseMotion mouseMotion)
		{
			RotationDegrees -= new Vector3(mouseMotion.Relative.Y, mouseMotion.Relative.X, 0) * MouseSensitivity;
		}
	}

	public override void _Process(double delta)
	{
		float speed = MoveSpeed;

		if (Input.IsActionPressed("ui_shift"))
			speed *= FastMultiplier;

		Vector3 direction = Vector3.Zero;

		if (Input.IsActionPressed("ui_up")) direction -= Transform.Basis.Z;
		if (Input.IsActionPressed("ui_down")) direction += Transform.Basis.Z;
		if (Input.IsActionPressed("ui_left")) direction -= Transform.Basis.X;
		if (Input.IsActionPressed("ui_right")) direction += Transform.Basis.X;
		if (Input.IsActionPressed("ui_page_up")) direction += Transform.Basis.Y;
		if (Input.IsActionPressed("ui_page_down")) direction -= Transform.Basis.Y;

		if (direction != Vector3.Zero)
		{
			direction = direction.Normalized();
			Position += direction * speed * (float)delta;
		}
	}
}
