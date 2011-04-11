using System;
using System.Collections.Generic;
using Gtk;
using common;

namespace puppet
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// some sanity checks
			if (args.Length < 2) 
			{
				Console.WriteLine ("Usage: puppet.exe <configfile> <instructionfile>");
				Environment.Exit(0);
			}
			
			if (!ConfigReader.ReadFile (args[0]))
				Environment.Exit (0);
			
			PuppetMaster puppetMaster = new PuppetMaster();
			puppetMaster.LoadConfig (args[0]);
			
			
			PuppetConfigurationReader configreader = new PuppetConfigurationReader ();
			configreader.LoadFile (args[1]);
			puppetMaster.InstructionSet = configreader.ParseAllInstructions ();	
						
			puppetMaster.InitPuppetMaster ();
			//puppetMaster.Play ();
			
			
			// initiate gui
			Application.Init ();		
			MainWindow win = new MainWindow ();
			win.SetPuppetMaster(puppetMaster);
			
			win.Show ();
			Application.Run ();
			
			
			// terminate gracefully
			Console.ReadLine ();
			puppetMaster.Shutdown ();
			Environment.Exit (0);
		}
	}
}


