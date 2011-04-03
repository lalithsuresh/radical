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
			
			
			//FIXME: Remove later
			client.Connect ();
			Console.ReadLine();			
		}
	}
}

