using Sandbox;
using System;
using System.Linq;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace MyGame;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class MyGame : Sandbox.GameManager
{
	[Net]
	public int gamemode { get; set; }
	public IClient curruntOwner_rolled { get; set; }
	public float timer_rolled { get; set; }
	public bool is_init_rolled { get; set; }
	/// <summary>
	/// Called when the game is created (on both the server and client)
	/// </summary>
	public MyGame()
	{
		if ( Game.IsClient )
		{
			_ = new Hud();
		}
	}

	[ConCmd.Admin( "mode" )]
	public static void TestServerCmd( string parameter )
	{
		Log.Info( $"Caller is {ConsoleSystem.Caller} - {parameter}" );
		(MyGame.Current as MyGame).gamemode = parameter.ToInt();
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new Pawn();
		client.Pawn = pawn;
		pawn.Respawn();
		pawn.DressFromClient( client );

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}
	}
	
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
		if ( gamemode == 3 )
		{
			if ( Time.Now - timer_rolled > 10 || is_init_rolled )
			{
				timer_rolled = Time.Now;
				curruntOwner_rolled = Game.Clients.ElementAt( (int)Game.Random.NextInt64( Game.Clients.Count ) );
			}
			if ( is_init_rolled )
				is_init_rolled = false;
		}
	}
}
