using Sandbox;

namespace MyGame;

public class MBot : Bot
{

	IClient Owner { get; set; }

	float angle = 0;

	[ConCmd.Admin( "bot_custom", Help = "Spawn my custom bot." )]
	internal static void SpawnCustomBot( IClient cl )
	{
		Game.AssertServer();

		// Create an instance of your custom bot.
		MBot bot = new()
		{
			Owner = cl
		};
		(cl.Pawn as Pawn).PBot = bot;
	}

	public static float getRand()
	{
		float rand = 0;
		for ( int i = 50; i >= 0; i-- )
		{
			rand += (float)Game.Random.NextDouble();
		}
		rand /= 50;
		return rand;
	}

	public override void BuildInput()
	{
		if ( (Owner.Pawn as Pawn).botcanmove ) 
		{ 
			Input.AnalogMove = new Vector3( getRand(), getRand(), 0 ) * .015f + Input.AnalogMove * .995f;
			Input.AnalogMove = Input.AnalogMove.Normal;
			Input.AnalogLook = new Angles( 0, getRand() * 20 * .01f + angle * .6f, 0 ); 
		}
		Input.SetAction( "attack1", getRand() > .6f );
		( Client.Pawn as Entity).BuildInput();
	}

	public override void Tick()
	{
		// Here we can do something with the bot each tick.
		// Here we'll print our bot's name every tick.
		Log.Info( Client.Name );
	}
}
