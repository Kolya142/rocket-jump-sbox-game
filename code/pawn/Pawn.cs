using Sandbox;
using Sandbox.Component;
using System;
using System.ComponentModel;
using System.Linq;

namespace MyGame;

public partial class Pawn : AnimatedEntity
{
	[Net, Predicted]
	public Weapon ActiveWeapon { get; set; }

	[ClientInput]
	public Vector3 InputDirection { get; set; }

	[ClientInput]
	public Angles ViewAngles { get; set; }

	[Net]
	public int Killed { get; set; } = 0;
	[Net]
	public int Deaths { get; set; } = 0;

	[Net]
	public int gg { get; set; } = 0;
	public bool ggn { get; set; }

	[Net]
	public bool noclip { get; set; } = false;

	public bool scoreboard { get; set; } = false;

	[ClientInput]
	public float DuckLevel { get; set; } = 0;

	/// <summary>
	/// Position a player should be looking from in world space.
	/// </summary>
	[Browsable( false )]
	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	/// <summary>
	/// Position a player should be looking from in local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Vector3 EyeLocalPosition { get; set; }

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity.
	/// </summary>
	[Browsable( false )]
	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity. In local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Rotation EyeLocalRotation { get; set; }

	public BBox Hull = new
		(
			new Vector3( -16, -16, 0 ),
			new Vector3( 16, 16, 64 )
		);

	[BindComponent] public PawnController Controller { get; }
	[BindComponent] public PawnAnimator Animator { get; }

	public int countCollected = 0;

	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );

	[ConCmd.Server( "noclip" )]
	static void DoPlayerNoclip()
	{
		if ( ConsoleSystem.Caller.Pawn is Pawn )
		{
			Pawn pawn = (ConsoleSystem.Caller.Pawn as Pawn);
			pawn.noclip = !pawn.noclip;
		}
	}

	public MBot PBot;
	public bool botcanmove = false;

	public override void Spawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );


		this.SetupPhysicsFromModel( PhysicsMotionType.Keyframed, false );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	public void SetActiveWeapon( Weapon weapon )
	{
		ActiveWeapon?.OnHolster();
		ActiveWeapon = weapon;
		ActiveWeapon.OnEquip( this );
	}

	public void Respawn()
	{
		this.Health = 100f;
		Components.Create<PawnController>();
		Components.Create<PawnAnimator>();

		SetActiveWeapon( new Pistol() );
	}

	public void DressFromClient( IClient cl )
	{
		var c = new ClothingContainer();
		c.LoadFromClient( cl );
		c.DressEntity( this );
	}

	public override void Simulate( IClient cl )
	{
		SimulateRotation();
		if ( Game.IsClient )
		{
			if ( LifeState == LifeState.Dead )
				Deaths += 1;
		}
		Controller?.Simulate( cl );
		Animator?.Simulate();
		ActiveWeapon?.Simulate( cl );
		EyeLocalPosition = Vector3.Up * (64f * Scale);
	}

	public override void BuildInput()
	{
		InputDirection = Input.AnalogMove;

		if ( Input.StopProcessing )
			return;

		var look = Input.AnalogLook;

		if ( ViewAngles.pitch > 90f || ViewAngles.pitch < -90f )
		{
			look = look.WithYaw( look.yaw * -1f );
		}

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89f, 89f );
		viewAngles.roll = 0f;
		ViewAngles = viewAngles.Normal;
	}

	bool IsThirdPerson { get; set; } = false;



	public override void FrameSimulate( IClient cl )
	{
		SimulateRotation();

		Camera.Rotation = ViewAngles.ToRotation();
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		if ( Input.Pressed( "view" ) )
		{
			IsThirdPerson = !IsThirdPerson;
		}
		scoreboard = Input.Down( "score" );
		if ( Game.IsClient && gg == 1 )
		{
			Sandbox.Services.Stats.Increment( "kils", 1 );
			gg = 0;
		}

		Hull.Maxs.z = 64 - DuckLevel * 10;

		if ( Input.Down( "duck" ) )
			DuckLevel = Math.Min(4, DuckLevel + Time.Delta * 5);
		else
			DuckLevel = 0;
		// DebugOverlay.Box( Hull.Mins + Position, Hull.Maxs + Position );
		var p = EyePosition;
		p.z = Position.z + Hull.Maxs.z;
		EyePosition = p;
		EyeLocalPosition = p - Position;

		if (noclip != ggn && Game.IsClient)
			Sandbox.Services.Stats.Increment( "spj2401", 1 );
		String k = "";

		if ( (MyGame.Current as MyGame).gamemode == 1 )
			k = $"Collected: {countCollected}";

		// DebugOverlay.ScreenText( "Health: " + Health + $"\ncurent mode: {(MyGame.Current as MyGame).gamemode}" + "\n" + k, new Vector2( 20f, Screen.Height - 60f ) );


		if ( IsThirdPerson || (MyGame.Current as MyGame).gamemode == 1 )
		{
			Vector3 targetPos;
			var pos = EyePosition + Vector3.Up * 16;
			var rot = Camera.Rotation * Rotation.FromAxis( Vector3.Up, -16 );

			float distance = 80.0f * Scale;
			targetPos = pos + rot.Right * ((CollisionBounds.Mins.x + 50) * Scale);
			targetPos += rot.Forward * -distance;

			var tr = Trace.Ray( pos, targetPos )
				.WithAnyTags( "solid" )
				.Ignore( this )
				.Radius( 8 )
				.Run();

			Camera.FirstPersonViewer = null;
			Camera.Position = tr.EndPosition;
		}
		else
		{
			Camera.FirstPersonViewer = this;
			Camera.Position = EyePosition;
		}
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f )
	{
		return TraceBBox( start, end, Hull.Mins, Hull.Maxs, liftFeet );
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f )
	{
		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		var tr = Trace.Ray( start, end )
					.Size( mins, maxs )
					.WithAnyTags( "solid", "playerclip", "passbullets" )
					.Ignore( this )
					.Run();

		return tr;
	}

	protected void SimulateRotation()
	{
		EyeRotation = ViewAngles.ToRotation();
		Rotation = ViewAngles.WithPitch( 0f ).ToRotation();
	}
	public override void OnKilled()
	{
		if ( LifeState == LifeState.Alive )
		{
			LifeState = LifeState.Dead;
			Deaths += 1;
			gg = 1;
			//Game.LocalClient.SetInt( "Deaths", Game.LocalClient.GetInt( "Deaths" ) + 1 );
		}
	}
}
