using System;
using comm;
using common;

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
						
			Client client = new Client ();
			client.LoadConfig (args[0]);
			client.InitClient ();
			
			Client client1 = new Client ();
			client1.LoadConfig ("/home/nightstrike/programming/mono/radical/client/client1.config");
			client1.InitClient ();
			
			Console.ReadLine();			
		}
	}
}

