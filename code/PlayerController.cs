using Sandbox;
using Sandbox.Citizen;
using Sandbox.Services;
using static Sandbox.Component;

[Group( "Walker" )]
[Title( "Walker - Player Controller" )]
public sealed class PlayerController : Component, IDamageable
{
	public static PlayerController Local => Game.ActiveScene.Components.GetAll<PlayerController>( FindMode.EnabledInSelfAndDescendants ).ToList().FirstOrDefault( x => x.Network.OwnerConnection.SteamId == (ulong)Game.SteamId );

	[Property] public CharacterController CharacterController { get; set; }
	[Property] public float CrouchMoveSpeed { get; set; } = 64.0f;
	[Property] public float WalkMoveSpeed { get; set; } = 190.0f;
	public float JumpForce => 800f;
	[Property] public float RunMoveSpeed { get; set; } = 190.0f;
	[Property] public float SprintMoveSpeed { get; set; } = 320.0f;
	[Property] public GameObject DecalEffect { get; set; }

	[Property] public GameObject AimShow;

	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }
	public GameObject ViewModel => Game.ActiveScene.Camera.GameObject.Children[0].Children[0];

	[Sync] public bool Crouching { get; set; }
	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public Vector3 WishVelocity { get; set; }

	public bool WishCrouch;
	public int mode;
	[Sync, Property] public float Health { get; set; } = 100;
	public float EyeHeight = 64;
	SoundEvent shootSound = Cloud.SoundEvent( "mdlresrc.toolgunshoot" );
	TimeSince timeSinceShoot;

	private void Respawn()
	{
		List<SpawnPoint> spawnPoints = Game.ActiveScene.Components.GetAll<SpawnPoint>( FindMode.EnabledInSelfAndDescendants ).ToList();
		int index = Game.Random.Next( spawnPoints.Count );
		//Log.Info( spawnPoints );
		//Log.Info( spawnPoints.Count );
		//Log.Info( index );
		SpawnPoint spawnPoint = spawnPoints[index];
		Transform.Position = spawnPoint.Transform.Position;
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		Respawn();
	}
	[Broadcast]
	private void ShootAnim()
	{
		AnimationHelper.Target.Set( "b_attack", true );
	}

	private void Shoot()
	{
		if ( timeSinceShoot < 0.1f )
			return;
		ViewModel.Components.Get<SkinnedModelRenderer>().Set( "fire", true );
		ShootAnim();

		Sound.Play( shootSound, Transform.Position );

		timeSinceShoot = 0;

		var tr = Scene.Trace.Ray( Transform.Position + Vector3.Up * EyeHeight, Transform.Position + Vector3.Up * EyeHeight + EyeAngles.Forward * 3000.0f )
				.IgnoreGameObject(GameObject)
				.Run();
		
		if (tr.GameObject == null)
		{
			return;
		}

		if ( tr.Body is not null )
		{
			tr.Body.ApplyImpulseAt( tr.HitPosition, tr.Direction * 1000.0f * tr.Body.Mass.Clamp( 0, 1000 ) );
		}

		var damage = new DamageInfo( 10f, GameObject, GameObject, tr.Hitbox );
		if ( DecalEffect is not null )
		{
			var decal = DecalEffect.Clone( new Transform( tr.HitPosition + tr.Normal * 2.0f, Rotation.LookAt( -tr.Normal, Vector3.Random ), Game.Random.Float( 0.8f, 1.2f ) ) );
			decal.Components.Create<SelfDestroyComponent>().time = 20f;
			decal.SetParent( tr.GameObject );
			decal.NetworkSpawn();
		}
		damage.Position = tr.HitPosition;
		damage.Shape = tr.Shape;

		foreach( Component damageable in tr.GameObject.Components.GetAll<IDamageable>() )
		{
			if (damageable.Components.Get<PlayerController>() != null && !IsProxy)
			{
				Stats.Increment( "hitpl", 1 );
				if ( damageable.Components.Get<PlayerController>().Health - 10 <= 0 )
				{
					Stats.Increment( "kill", 1 );
				}
			}
			if ( (damageable as IDamageable) != null )
			{
				(damageable as IDamageable).OnDamage( damage );
			}
		}
	}

	private void RocketJump()
	{
		var tr = Scene.Trace.Ray( Transform.Position + Vector3.Up * EyeHeight, Transform.Position + Vector3.Up * EyeHeight + EyeAngles.Forward * 500.0f )
				.IgnoreGameObject( GameObject )
				.Run();
		if ( !tr.Hit ) 
			return;
		Sandbox.Services.Stats.Increment( "jumps", 1 );
		CharacterController.IsOnGround = false;
		float r = JumpForce;
		
		CharacterController.Velocity += -EyeAngles.Forward * r;
	}

	protected override void OnUpdate()
	{
		Health = (Health + 1 * Time.Delta).Clamp( 0, 100 );
		if ( !IsProxy )
		{
			MouseInput();
			Transform.Rotation = new Angles( 0, EyeAngles.yaw, 0 );
			if (Input.Pressed("attack1")) {
				Shoot();
			}
			if (Input.Pressed("attack2"))
			{
				RocketJump();
			}
			if (Input.Pressed("reload"))
			{
				Respawn();
			}
		}
		var tr = Scene.Trace.Ray( Transform.Position + Vector3.Up * EyeHeight, Transform.Position + Vector3.Up * EyeHeight + EyeAngles.Forward * 500.0f )
				.IgnoreGameObject( GameObject )
				.Run();
		if ( tr.Hit )
		{
			AimShow.Transform.Position = tr.HitPosition;
			AimShow.Transform.Rotation = Transform.Rotation;
			AimShow.Transform.Scale = new Vector3( 0.1f );
		}
		else
		{
			AimShow.Transform.Scale = new Vector3( 0 );
		}

		UpdateAnimation();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		CrouchingInput();
		MovementInput();
	}

	private void MouseInput()
	{
		var e = EyeAngles;
		e += Input.AnalogLook;
		e.pitch = e.pitch.Clamp( -90, 90 );
		e.roll = 0.0f;
		EyeAngles = e;
	}

	float CurrentMoveSpeed
	{
		get
		{
			if ( Crouching ) return CrouchMoveSpeed;
			if ( Input.Down( "run" ) ) return SprintMoveSpeed;
			if ( Input.Down( "walk" ) ) return WalkMoveSpeed;

			return RunMoveSpeed;
		}
	}

	RealTimeSince lastGrounded;
	RealTimeSince lastUngrounded;
	RealTimeSince lastJump;

	float GetFriction()
	{
		if ( CharacterController.IsOnGround ) return 6.0f;

		// air friction
		return 0.2f;
	}

	private void MovementInput()
	{
		if ( CharacterController is null )
			return;

		var cc = CharacterController;

		Vector3 halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

		WishVelocity = Input.AnalogMove;

		if ( lastGrounded < 0.2f && lastJump > 0.3f && Input.Pressed( "jump" ) )
		{
			lastJump = 0;
			cc.Punch( Vector3.Up * 300 );
		}

		if ( !WishVelocity.IsNearlyZero() )
		{
			WishVelocity = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation() * WishVelocity;
			WishVelocity = WishVelocity.WithZ( 0 );
			WishVelocity = WishVelocity.ClampLength( 1 );
			WishVelocity *= CurrentMoveSpeed;

			if ( !cc.IsOnGround )
			{
				WishVelocity = WishVelocity.ClampLength( 50 );
			}
		}


		cc.ApplyFriction( GetFriction() );

		if ( cc.IsOnGround )
		{
			cc.Accelerate( WishVelocity );
			cc.Velocity = CharacterController.Velocity.WithZ( 0 );
		}
		else
		{
			cc.Velocity += halfGravity;
			cc.Accelerate( WishVelocity );

		}

		//
		// Don't walk through other players, let them push you out of the way
		//
		var pushVelocity = PlayerPusher.GetPushVector( Transform.Position + Vector3.Up * 40.0f, Scene, GameObject );
		if ( !pushVelocity.IsNearlyZero() )
		{
			var travelDot = cc.Velocity.Dot( pushVelocity.Normal );
			if ( travelDot < 0 )
			{
				cc.Velocity -= pushVelocity.Normal * travelDot * 0.6f;
			}

			cc.Velocity += pushVelocity * 128.0f;
		}

		cc.Move();

		if ( !cc.IsOnGround )
		{
			cc.Velocity += halfGravity;
		}
		else
		{
			cc.Velocity = cc.Velocity.WithZ( 0 );
		}

		if ( cc.IsOnGround )
		{
			lastGrounded = 0;
		}
		else
		{
			lastUngrounded = 0;
		}
	}
	float DuckHeight = (64 - 36);

	bool CanUncrouch()
	{
		if ( !Crouching ) return true;
		if ( lastUngrounded < 0.2f ) return false;

		var tr = CharacterController.TraceDirection( Vector3.Up * DuckHeight );
		return !tr.Hit; // hit nothing - we can!
	}

	public void CrouchingInput()
	{
		WishCrouch = Input.Down( "duck" );

		if ( WishCrouch == Crouching )
			return;

		// crouch
		if ( WishCrouch )
		{
			CharacterController.Height = 36;
			Crouching = WishCrouch;

			// if we're not on the ground, slide up our bbox so when we crouch
			// the bottom shrinks, instead of the top, which will mean we can reach
			// places by crouch jumping that we couldn't.
			if ( !CharacterController.IsOnGround )
			{
				CharacterController.MoveTo( Transform.Position += Vector3.Up * DuckHeight, false );
				Transform.ClearLerp();
				EyeHeight -= DuckHeight;
			}

			return;
		}

		// uncrouch
		if ( !WishCrouch )
		{
			if ( !CanUncrouch() ) return;

			CharacterController.Height = 64;
			Crouching = WishCrouch;
			return;
		}


	}

	private void UpdateCamera()
	{
		var camera = Scene.GetAllComponents<CameraComponent>().Where( x => x.IsMainCamera ).FirstOrDefault();
		if ( camera is null ) return;

		var targetEyeHeight = Crouching ? 28 : 64;
		EyeHeight = EyeHeight.LerpTo( targetEyeHeight, RealTime.Delta * 10.0f );

		var targetCameraPos = Transform.Position + new Vector3( 0, 0, EyeHeight );

		// smooth view z, so when going up and down stairs or ducking, it's smooth af
		if ( lastUngrounded > 0.2f )
		{
			targetCameraPos.z = camera.Transform.Position.z.LerpTo( targetCameraPos.z, RealTime.Delta * 25.0f );
		}

		camera.Transform.Position = targetCameraPos;
		camera.Transform.Rotation = EyeAngles;
		camera.FieldOfView = Preferences.FieldOfView;
	}

	protected override void OnPreRender()
	{
		UpdateBodyVisibility();

		if ( IsProxy )
			return;

		UpdateCamera();
	}

	private void UpdateAnimation()
	{
		if ( AnimationHelper is null ) return;

		var wv = WishVelocity.Length;

		AnimationHelper.WithWishVelocity( WishVelocity );
		AnimationHelper.WithVelocity( CharacterController.Velocity );
		AnimationHelper.IsGrounded = CharacterController.IsOnGround;
		AnimationHelper.DuckLevel = Crouching ? 1.0f : 0.0f;

		AnimationHelper.MoveStyle = wv < 160f ? CitizenAnimationHelper.MoveStyles.Walk : CitizenAnimationHelper.MoveStyles.Run;

		var lookDir = EyeAngles.ToRotation().Forward * 1024;
		AnimationHelper.WithLook( lookDir, 1, 0.5f, 0.25f );
	}

	private void UpdateBodyVisibility()
	{
		if ( AnimationHelper is null )
			return;

		var renderMode = ModelRenderer.ShadowRenderType.On;
		if ( !IsProxy ) renderMode = ModelRenderer.ShadowRenderType.ShadowsOnly;

		AnimationHelper.Target.RenderType = renderMode;

		foreach ( var clothing in AnimationHelper.Target.Components.GetAll<ModelRenderer>( FindMode.InChildren ) )
		{
			if ( !clothing.Tags.Has( "clothing" ) )
				continue;

			clothing.RenderType = renderMode;
		}
	}

	[Broadcast]
	private void MinusHealth( float value )
	{
		Health -= value;
	}

	[Broadcast]
	private void ResetHealth()
	{
		Health = 100;
	}

	public void OnDamage( in DamageInfo damage )
	{
		MinusHealth( damage.Damage );
		if ( Health <= 1 )
		{
			Respawn();
			ResetHealth();
			SayAboutMyDie(damage);
		}
	}

	void SayAboutMyDie( in DamageInfo damage )
	{
		Stats.Increment( "deth", 1 );
		Hud hud = Game.ActiveScene.Components.GetInChildrenOrSelf<Hud>();
		if ( hud != null )
		{
			hud.SendText( "system", $"{damage.Attacker.Network.OwnerConnection.DisplayName} ☠ {Network.OwnerConnection.DisplayName}" );
		}
	}
}
