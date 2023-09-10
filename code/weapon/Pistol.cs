using Sandbox;

namespace MyGame;

public partial class Pistol : Weapon
{
	public override string ModelPath => "weapons/rust_pistol/rust_pistol.vmdl";
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	[ClientRpc]
	protected virtual void ShootEffects()
	{
		Game.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );

		Pawn.SetAnimParameter( "b_attack", true );
		ViewModelEntity?.SetAnimParameter( "fire", true );
	}

	public override void PrimaryAttack()
	{
		ShootEffects();
		Pawn.PlaySound( "rust_pistol.shoot" );
		ShootBullet( 0.04f, 100, ((MyGame.Current as MyGame).gamemode == 2 ? 52 : 25), 5 );
		Game.SetRandomSeed( Time.Tick + 25 - (MyGame.Current as MyGame).gamemode );
	}

	protected override void Animate()
	{
		Pawn.SetAnimParameter( "holdtype", (int)CitizenAnimationHelper.HoldTypes.Pistol );
	}
}
