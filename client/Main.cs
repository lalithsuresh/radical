using System;
using comm;
using common;
using System.Collections.Generic;

namespace client
{
	class MainClass
	{
		/*
		 * There are two ways to start a client:
		 * 1) with a full config file
		 * 2) with a Username, Port and Default config file
		 */
		public static void Main (string[] args)
		{
			// some sanity checks
			if (args.Length < 1) 
			{
				Usage ();
				Environment.Exit(0);
			}
			
			Client client = new Client ();
			
			if (args.Length == 1) {
				// only for stress testing				
				if (args[0].Equals ("--stress")) 
				{	
					StressTestClient stc = new StressTestClient ();
					stc.Run ();
					Console.ReadLine ();
					Console.WriteLine ("Exiting stress test");
					Environment.Exit (0);
				}				
				
				// normal case
				if (!ConfigReader.ReadFile (args[0]))
					Environment.Exit (0);
				
				client.LoadConfig ();			
			} 
			else if (args.Length == 3) 
			{
				// start from puppet master
				if (!ConfigReader.ReadFile (args[2]))
					Environment.Exit (0);
								
				try 
				{
					client.LoadConfig (args[0], Int32.Parse (args[1]));					
				} 
				catch (FormatException fe) 
				{
					Console.WriteLine ("Invalid port.");
					Environment.Exit (0);
				}
			}
			else 
			{
				Usage ();
				Environment.Exit (0);
			}
						
			client.InitClient ();
			
			
			Console.ReadLine();			
			
		}
		
		private static void Usage () 
		{
			Console.WriteLine ("Usage: client.exe <configfile>");
			Console.WriteLine ("Usage: client.exe <UserName> <Port> <generic.config>");
		}
	}
}

   