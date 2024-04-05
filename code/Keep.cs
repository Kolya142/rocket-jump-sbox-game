using Sandbox;

public sealed class Keep : Component
{
	Vector3 Pos;
	Rotation Ros;
	protected override void OnAwake()
	{
		base.OnAwake();
		Ros = Transform.Rotation;
		Pos = Transform.Position;
	}
	protected override void OnUpdate()
	{
		Transform.Rotation = Ros;
		Transform.Position = Pos;
	}
	protected override void OnFixedUpdate()
	{
		Transform.Rotation = Ros;
		Transform.Position = Pos;
	}
	protected override void OnPreRender()
	{
		base.OnPreRender();
		Transform.Rotation = Ros;
		Transform.Position = Pos;
	}
}
