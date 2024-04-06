using Sandbox;

public sealed class SelfDestroyComponent : Component
{
	[Property] public float time = 10;
	public TimeUntil Time;
	protected override void OnEnabled()
	{
		Time = time;
	}
	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;
		if (Time <= 0)
		{
			GameObject.Destroy();
		}
	}
}
