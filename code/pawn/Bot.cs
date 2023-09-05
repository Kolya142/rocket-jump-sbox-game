using Sandbox;

namespace MyGame;

public partial class Bot : AnimatedEntity 
{ 
	public override void Spawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		this.SetupPhysicsFromModel( PhysicsMotionType.Keyframed, false );

	}

	public void Respawn()
	{
		this.Health = 100f;
	}

	public override void Simulate( IClient cl )
	{
	}
	public override void FrameSimulate( IClient cl )
	{
	}
	public override void OnKilled()
	{
		if ( LifeState == LifeState.Alive )
		{
			LifeState = LifeState.Dead;
		}
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		Log.Info( "bot die" );
	}
}
