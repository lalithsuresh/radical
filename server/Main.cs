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
			if (args.Length < 2) 
			{
				Console.WriteLine ("Usage: server.exe <port> <configfile>");
				Environment.Exit(0);
			}
			
			try 
			{
				port = Int32.Parse(args[0]);
			} 
			catch (Exception)
			{
				Console.WriteLine ("Invalid port number.");
				Environment.Exit(0);
			}
			
			if (!ConfigReader.ReadFile (args[1]))
				Environment.Exit(0);
			
			
			// all iz well, init
			Console.WriteLine ("Hi, I'm a server on port {0}!", port);
			Server server = new Server (); 
			server.InitServer (port); 
			
			// terminate gracefully
			Console.ReadLine ();
			server.Shutdown (); 
		}
	}
}

