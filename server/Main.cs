using System; 
using common;

namespace server
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			int port = 0;
			
			// some sanity checks
			if (args.Length < 1) 
			{
				Console.WriteLine ("Usage: server.exe <configfile>");
				Environment.Exit(0);
			}			
			
			// all iz well, init
			Console.WriteLine ("Hi, I'm a server!");
			Server server = new Server ();
			server.LoadConfig (args[0]);
			server.InitServer (); 
			
			// terminate gracefully
			Console.ReadLine ();
			server.Shutdown (); 
		}
	}
}

