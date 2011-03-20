using System; 

namespace server
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("I'm a server!");
			Server server = new Server (); 
			server.InitServer (); 
			Console.ReadLine ();
		}
	}
}

