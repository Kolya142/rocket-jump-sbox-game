using Sandbox;
using System;

namespace MyGame;

public class PawnAnimator : EntityComponent<Pawn>, ISingletonComponent
{
	public void Simulate()
	{
		var helper = new CitizenAnimationHelper( Entity );
		helper.WithVelocity( Entity.Velocity );
		helper.WithLookAt( Entity.EyePosition + Entity.EyeRotation.Forward * 100 );
		helper.HoldType = CitizenAnimationHelper.HoldTypes.None;
		helper.IsNoclipping = Entity.noclip;
		helper.DuckLevel = Entity.DuckLevel;
		helper.IsSwimming = Entity.GetWaterLevel() >= 0.5f;
		helper.IsGrounded = Entity.GroundEntity.IsValid();

		if ( Entity.Controller.HasEvent( "jump" ) )
		{
			helper.TriggerJump();
		}
	}
}
