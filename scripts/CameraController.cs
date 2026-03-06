using Godot;

public partial class CameraController : Camera3D
{
	[Export] public float MoveSpeed = 20f;
	[Export] public float MouseSensitivity = 0.15f;
	[Export] public float FastMultiplier = 3f;

	private float _yaw;
	private float _pitch;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_yaw = RotationDegrees.Y;
		_pitch = RotationDegrees.X;
		
		Current = true;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
				Input.MouseMode = Input.MouseModeEnum.Visible;
			else
				Input.MouseMode = Input.MouseModeEnum.Captured;
		}

		if (Input.MouseMode != Input.MouseModeEnum.Captured)
			return;

		if (@event is InputEventMouseMotion mouseMotion)
		{
			_yaw -= mouseMotion.Relative.X * MouseSensitivity;
			_pitch -= mouseMotion.Relative.Y * MouseSensitivity;
			_pitch = Mathf.Clamp(_pitch, -89f, 89f);

			RotationDegrees = new Vector3(_pitch, _yaw, 0f);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{		
		float speed = MoveSpeed;
		if (Input.IsActionPressed("ui_shift"))
		{
			speed *= FastMultiplier;
		}

		Vector3 direction = Vector3.Zero;
		if (Input.IsActionPressed("ui_up")) direction -= Transform.Basis.Z;
		if (Input.IsActionPressed("ui_down")) direction += Transform.Basis.Z;
		if (Input.IsActionPressed("ui_left")) direction -= Transform.Basis.X;
		if (Input.IsActionPressed("ui_right")) direction += Transform.Basis.X;
		if (Input.IsActionPressed("ui_page_up")) direction -= Transform.Basis.Y;
		if (Input.IsActionPressed("ui_page_down")) direction += Transform.Basis.Y;

		if (direction != Vector3.Zero)
		{
			Position += direction.Normalized() * speed * (float)delta;
		}
	}
}
