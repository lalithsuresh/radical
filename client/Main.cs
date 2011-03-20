using System;
using comm;
using config;

namespace client
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// some sanity checks
			if (args.Length < 1) 
			{
				Console.WriteLine ("Usage: client.exe <configfile>");
				Environment.Exit(0);
			}
						
			if (!ConfigReader.ReadFile (args[0]))
				Environment.Exit(0);
			
			Client client = new Client ();
			
			client.InitClient ();
			Console.ReadLine();
		}
	}
}

