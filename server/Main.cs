using System; 
using common;

namespace server
{
	class MainClass
	{
		public static void Main (string[] args)
		{			
			// some sanity checks
			if (args.Length < 1) 
			{
				Console.WriteLine ("Usage: server.exe <configfile>");
				Environment.Exit(0);
			}			
			
			// all iz well, init			
			Server server = new Server ();
			server.LoadConfig (args[0]);
			server.InitServer (); 
			
			// keep alive
			string input = Console.ReadLine ();
			while (!input.Equals ("exit"))
			{
				input = Console.ReadLine ();
			}
						
			// terminate gracefully
			server.Shutdown (); 
		}
	}
}

