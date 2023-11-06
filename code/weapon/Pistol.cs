using Sandbox;

namespace MyGame;

public partial class Pistol : Weapon
{
	public override string ModelPath => "weapons/rust_pistol/rust_pistol.vmdl";
	public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

	private float reload_timer = 0;

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
		if ( reload_timer > 0 || ammo <= 0 ) return;
		ShootEffects();
		Pawn.PlaySound( "rust_pistol.shoot" );
		ShootBullet( 0.04f, 100, ((MyGame.Current as MyGame).gamemode == 2 ? 52 : 25), 5 );
		Game.SetRandomSeed( Time.Tick + 25 - (MyGame.Current as MyGame).gamemode );
		ammo--;
	}

	protected override void Animate()
	{
		Pawn.SetAnimParameter( "holdtype", (int)CitizenAnimationHelper.HoldTypes.Pistol );
	}

	public override void ReloadSimul()
	{
		if ( (Input.Released( "use" ) || (ammo == 0 && Input.Down( "attack1" ))) && reload_timer <= 0 )
		{
			ammo = 25;
			ViewModelEntity?.SetAnimParameter( "reload", true );
			reload_timer = 3;
		}
		reload_timer -= Time.Delta;
		if ( reload_timer < 0 ) {
			reload_timer = 0;
			ViewModelEntity?.SetAnimParameter( "reload", false );
		}
	}
}
