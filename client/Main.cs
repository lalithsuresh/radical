using System;
using comm;
using common;
using System.Collections.Generic;

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
				Environment.Exit (0);
			
			Client client = new Client ();
			//client.LoadConfig (args[0]);
			client.InitClient ();
			
			
			//FIXME: Remove later
			client.Connect ();
			
			//System.Threading.Thread.Sleep (100);
			if (client.UserName.Equals ("testclient1"))
			{
				
					// This test should book slot 1
				{
				List<string> userlist = new List<string> ();
				List<int> slotlist = new List<int> ();
				string description = "test description";
				
				userlist.Add ("testclient2");
				userlist.Add ("testclient3");
				//userlist.Add ("user2");
				//userlist.Add ("user3");
				slotlist.Add (1);
				slotlist.Add (2);
				slotlist.Add (3);
				client.Reserve (description, userlist, slotlist);
				}
				
				/*
				// This test should book slot 2 as opposed to slot 1
				{
				List<string> userlist = new List<string> ();
				List<int> slotlist = new List<int> ();
				string description = "test description111";
				
				userlist.Add ("testclient2");
				userlist.Add ("testclient3");
				//userlist.Add ("user5");
				//userlist.Add ("user6");
				slotlist.Add (1);
				slotlist.Add (2);
				slotlist.Add (8);
				client.Reserve (description, userlist, slotlist);
				}
				
				
				// This test should book slot 8 as opposed to slot 1 or 2
				{
				List<string> userlist = new List<string> ();
				List<int> slotlist = new List<int> ();
				string description = "test description111";
				
				userlist.Add ("testclient2");
				userlist.Add ("testclient3");
				//userlist.Add ("user5");
				//userlist.Add ("user6");
				slotlist.Add (1);
				slotlist.Add (2);
				slotlist.Add (8);
				client.Reserve (description, userlist, slotlist);
				}
				*/
			}
			
			Console.ReadLine();			
			
		}
	}
}

