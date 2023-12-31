﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGame;

public class PawnController : EntityComponent<Pawn>
{
	public int StepSize => 24;
	public int GroundAngle => 45;
	public int JumpSpeed => 300;
	public float Gravity => 800f;
	AnimatedEntity resultEntity_collect = new();
	AnimatedEntity resultEntityProj_collect = new();

	private bool isInit_collect = true;

	float rjt = 0f;
	public float JumpForce = 800f;

	HashSet<string> ControllerEvents = new( StringComparer.OrdinalIgnoreCase );

	bool Grounded => Entity.GroundEntity.IsValid();

	private void RandPos_collect()
	{
		const float mdist = 10000f;
		Vector3 HitPosition = new();
		while ( true )
		{
			Vector3 RandPos = new Vector3( new Random().Float( -mdist, mdist ), new Random().Float( -mdist, mdist ), 100000f );
			var result = Trace.Ray( RandPos, RandPos + Vector3.Down * 100000f ).StaticOnly().Run();
			HitPosition = result.HitPosition;
			if ( result.Hit && resultEntity_collect.Position.Distance( HitPosition ) > 100f )
				break;
		}
		resultEntity_collect.Position = HitPosition;
	}

	public void Simulate( IClient cl )
	{
		ControllerEvents.Clear();

		if ( (MyGame.Current as MyGame).gamemode == 1 && resultEntity_collect.Position.Distance( Entity.Position ) < resultEntity_collect.CollisionBounds.Size.Length * 3f )
		{
			Sound.FromEntity( "sounds/collect.sound", Entity );
			RandPos_collect();
			Entity.countCollected++;
		}

		if ( isInit_collect && Game.IsClient && (MyGame.Current as MyGame).gamemode == 1 )
		{
			resultEntity_collect.Model = Cloud.Model( "https://asset.party/facepunch/cardboard_box" );
			resultEntityProj_collect.Model = resultEntity_collect.Model;

			resultEntity_collect.Scale = 1.6f;
			isInit_collect = false;

			resultEntityProj_collect.Scale = 0.5f;
			RandPos_collect();
		}
		if ( (MyGame.Current as MyGame).gamemode == 1 )
		{
			Vector3 direction = resultEntity_collect.Position - Entity.Position;
			direction = direction.Normal * 40f;

			resultEntityProj_collect.Position = Entity.Position + Vector3.Up * Entity.CollisionBounds.Size.z * Entity.Scale * 0.5f + direction;
		}
		if ( (MyGame.Current as MyGame).gamemode != 1 && isInit_collect == false )
		{
			isInit_collect = true;
			resultEntityProj_collect.Delete();
			resultEntity_collect.Delete();
		}

		var movement = Entity.InputDirection.Normal;
		var angles = Entity.ViewAngles.WithPitch( 0 );
		var moveVector = Rotation.From( angles ) * movement * 320f;
		var groundEntity = CheckForGround();
		Entity.Health = Math.Min( Entity.Health + .2f * Time.Delta * ((MyGame.Current as MyGame).gamemode == 2 ? 3 : 1), 100f );

		if ( !Entity.noclip && !(Entity.GetWaterLevel() >= 0.5f) )
		{
			if ( groundEntity.IsValid() )
			{
				if ( !Grounded )
				{
					Entity.Velocity = Entity.Velocity.WithZ( 0 );
					AddEvent( "grounded" );
				}

				Entity.Velocity = Accelerate( Entity.Velocity, moveVector.Normal, moveVector.Length, 200.0f * (Input.Down( "run" ) ? 2.5f : 1f), 7.5f );
				Entity.Velocity = ApplyFriction( Entity.Velocity, 4.0f );
			}
			else
			{
				Entity.Velocity = Accelerate( Entity.Velocity, moveVector.Normal, moveVector.Length, 100, 20f );
				var gm3 = (MyGame.Current as MyGame).gamemode == 3 && (MyGame.Current as MyGame).curruntOwner_rolled != null;
				gm3 = gm3 && (MyGame.Current as MyGame).curruntOwner_rolled.IsValid();
				var g = gm3 ? ((MyGame.Current as MyGame).curruntOwner_rolled.Pawn as Pawn).EyeRotation.Forward : Vector3.Down;
				Entity.Velocity += g * Gravity * Time.Delta;
			}
		}
		else
		{
			moveVector = Entity.EyeRotation * movement * 320f;
			Entity.Velocity = Accelerate( Entity.Velocity, moveVector.Normal, moveVector.Length, 200.0f * (Input.Down( "run" ) ? 2.5f : 1f), 7.5f );
			Entity.Velocity = ApplyFriction( Entity.Velocity, 4.0f );
		}

		if ( Input.Pressed( "jump" ) && !Entity.noclip )
		{
			DoJump();
		}
		/*
		if ( Input.Down( "score" ) && Game.IsClient )
		{
			IReadOnlyCollection<IClient> clients = Game.Clients;
			for ( int i = 0; i < clients.Count; i++ )
			{
				var client = clients.ElementAt( i );
				DebugOverlay.ScreenText( $"{client.Name} - Killed: {(client.Pawn as Pawn).Killed}, Deaths: {(client.Pawn as Pawn).Deaths}, Ping: {client.Ping}", new Vector2( Screen.Width / 2 - 200, 20 ), i, Color.Green );
			}
		}
		*/

		if ( Input.Down( "flashlight" ) )
		{
			DebugOverlay.ScreenText( $"Positon: {Entity.Position}\nRotation: {Entity.Rotation.Forward.x}, {Entity.Rotation.Forward.y}\nVelocity: {Entity.Velocity}\nForce: {Entity.Velocity.Length}\nPlayer Username: {Game.UserName}\nPlayers Count: {Game.Clients.Count}\nRocket Jump Timer: {rjt}\nNoclipState: {Entity.noclip}\n1 - Rocket Jump Force: {JumpForce}\n2 - Health: {Entity.Health}\n3 - Bot Can Move: {Entity.botcanmove}" );

			if ( Input.Down( "Slot1" ) )
			{
				if ( Input.Pressed( "SlotPrev" ) )
				{
					JumpForce -= 10f;
					if ( JumpForce < 0 )
						JumpForce = 0;
				}
				if ( Input.Pressed( "SlotNext" ) )
				{

					JumpForce += 10f;
					if ( JumpForce > 1000 )
						JumpForce = 1000;
				}
			}
			if ( Input.Down( "Slot2" ) )
			{
				if ( Input.Pressed( "SlotPrev" ) )
				{
					Entity.Health -= 10f;
					if ( Entity.Health < 0 )
					{
						Entity.Health = 0;
						if ( Entity.LifeState == LifeState.Alive )
						{
							Entity.LifeState = LifeState.Dead;
						}
					}
				}
				if ( Input.Pressed( "SlotNext" ) )
				{

					Entity.Health += 10f;
					if ( Entity.Health > 100 )
						Entity.Health = 100;
				}
			}
			if ( Input.Pressed( "Slot3" ) )
				Entity.botcanmove = !Entity.botcanmove;
			if ( Input.Pressed( "drop" ) && Game.IsServer )
			{
				if ( Entity.PBot == null || !Entity.PBot.Client.IsValid )
				{
					var tr = Trace.Ray( Entity.AimRay, 1000f ).StaticOnly().Run();
					MBot.SpawnCustomBot( cl );
				}
				else
				{
					Entity.PBot.Client.Pawn.Delete();
					Entity.PBot.Client.Kick();
				}
			}
		}
		if ( Input.Pressed( "reload" ) || Entity.LifeState == LifeState.Dead )
		{

			// Get all of the spawnpoints
			var spawnpoints = Sandbox.Entity.All.OfType<SpawnPoint>();

			// chose a random one
			var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

			// if it exists, place the pawn there
			if ( randomSpawnPoint != null )
			{
				var tx = randomSpawnPoint.Transform;
				tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
				Entity.Transform = tx;
			}
			if ( Entity.LifeState == LifeState.Dead )
			{
				Entity.Health = 100f;
				Entity.LifeState = LifeState.Alive;
			}

		}

		var rt = Trace.Ray( new Ray( Entity.AimRay.Position, Entity.AimRay.Forward - Entity.Velocity.Normal / 30 ), 500f ).Ignore( Entity ).Run();

		if ( rt.Hit )
		{
			DebugOverlay.Circle( rt.HitPosition, Entity.EyeRotation, 4f, Color.Red );
			if ( Input.Pressed( "attack2" ) && rjt <= 0f && !Entity.noclip)
			{
				Sound.FromWorld( "sounds/hit_hurt2.sound", rt.HitPosition );
				float r = ((MyGame.Current as MyGame).gamemode == 2 ? 430 : JumpForce) * (1 - (rt.Distance / 500f));
				Entity.Velocity += Entity.EyeRotation.Backward * r;
				//rjt = 50f;
				if ( Game.IsClient )
					Sandbox.Services.Stats.Increment( Game.LocalClient, "jump", 1 );
				var tr = Trace.Ray( new Ray( Entity.AimRay.Position, Entity.AimRay.Forward - Entity.Velocity.Normal / 30 ), 500f ).DynamicOnly().Ignore(Entity).Run();
				if ( Game.IsServer && tr.Entity.IsValid() )
				{
					DamageInfo damginf = DamageInfo.FromExplosion(rt.HitPosition, r, 400);
					using ( Prediction.Off() )
					{
						tr.Entity.TakeDamage( damginf );
					}
				}
			}
		}

		if ( rjt > 0f )
		{
			rjt -= Time.Delta * 300f;
			if ( rjt < 0f )
				rjt = 0f;
		}
		if ( !Entity.noclip )
		{
			var mh = new MoveHelper( Entity.Position, Entity.Velocity );
			mh.Trace = mh.Trace.Size( Entity.Hull ).Ignore( Entity );

			if ( mh.TryMoveWithStep( Time.Delta, StepSize ) > 0 )
			{
				if ( Grounded )
				{
					mh.Position = StayOnGround( mh.Position );
				}
				Entity.Position = mh.Position;
				Entity.Velocity = mh.Velocity;
			}

			Entity.GroundEntity = groundEntity;
		}
		else
		{
			Entity.Position += Entity.Velocity * 0.1f;
		}
	}

	void DoJump()
	{
		if ( Grounded )
		{
			Entity.Velocity = ApplyJump( Entity.Velocity, "jump" );
		}
	}

	Entity CheckForGround()
	{
		if ( Entity.Velocity.z > 100f )
			return null;
		var gm3 = (MyGame.Current as MyGame).gamemode == 3 && (MyGame.Current as MyGame).curruntOwner_rolled != null;
		var g = gm3 ? ((MyGame.Current as MyGame).curruntOwner_rolled.Pawn as Pawn).EyeRotation.Forward : Vector3.Down;
		var trace = Entity.TraceBBox( Entity.Position, Entity.Position + g, 2f );

		if ( !trace.Hit )
			return null;

		if ( trace.Normal.Angle( Vector3.Up ) > GroundAngle )
			return null;

		return trace.Entity;
	}

	Vector3 ApplyFriction( Vector3 input, float frictionAmount )
	{
		float StopSpeed = 100.0f;

		var speed = input.Length;
		if ( speed < 0.1f ) return input;

		// Bleed off some speed, but if we have less than the bleed
		// threshold, bleed the threshold amount.
		float control = (speed < StopSpeed) ? StopSpeed : speed;

		// Add the amount to the drop amount.
		var drop = control * Time.Delta * frictionAmount;

		// scale the velocity
		float newspeed = speed - drop;
		if ( newspeed < 0 ) newspeed = 0;
		if ( newspeed == speed ) return input;

		newspeed /= speed;
		input *= newspeed;

		return input;
	}

	Vector3 Accelerate( Vector3 input, Vector3 wishdir, float wishspeed, float speedLimit, float acceleration )
	{
		if ( speedLimit > 0 && wishspeed > speedLimit )
			wishspeed = speedLimit;

		var currentspeed = input.Dot( wishdir );
		var addspeed = wishspeed - currentspeed;

		if ( addspeed <= 0 )
			return input;
		
		var accelspeed = acceleration * Time.Delta * wishspeed * 1.2f;

		if ( accelspeed > addspeed )
			accelspeed = addspeed;

		input += wishdir * accelspeed;

		return input;
	}

	Vector3 ApplyJump( Vector3 input, string jumpType )
	{
		AddEvent( jumpType );

		return input + Vector3.Up * JumpSpeed;
	}

	Vector3 StayOnGround( Vector3 position )
	{
		var start = position + Vector3.Up * 2;
		var end = position + Vector3.Down * StepSize;
		// See how far up we can go without getting stuck
		var trace = Entity.TraceBBox( position, start );
		start = trace.EndPosition;

		// Now trace down from a known safe position
		trace = Entity.TraceBBox( start, end );

		if ( trace.Fraction <= 0 ) return position;
		if ( trace.Fraction >= 1 ) return position;
		if ( trace.StartedSolid ) return position;
		if ( Vector3.GetAngle( Vector3.Up, trace.Normal ) > GroundAngle ) return position;

		return trace.EndPosition;
	}

	public bool HasEvent( string eventName )
	{
		return ControllerEvents.Contains( eventName );
	}

	void AddEvent( string eventName )
	{
		if ( HasEvent( eventName ) )
			return;

		ControllerEvents.Add( eventName );
	}
}
