using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using common;

namespace puppet
{

	public class PuppetInstruction 
	{
		public PuppetInstructionType Type {
			get;
			set;
		}
		public string ApplyToUser {
			get;
			set;
		}
		public string Description {
			get;
			set;
		}
		public List<string> Users {
			get;
			set;
		}
		public List<string> Slots {
			get;
			set;
		}
	}

	public class PuppetConfigurationReader : PadicalObject 
	{
		private TextReader m_file;

		public PuppetConfigurationReader ()
		{
		}
		
		public bool LoadFile (string filename) 
		{
			if (String.IsNullOrEmpty (filename)) 
			{
				return false;
			}
			
			try 
			{
				m_file = new StreamReader (filename);
			} 
			catch (IOException) 
			{
				DebugFatal ("Could not read file {0}", filename);
				return false;
			}
			
			return true; 
		}	
		
		public Queue<PuppetInstruction> ParseAllInstructions () 
		{
			Queue<PuppetInstruction> puppetInstructions = new Queue<PuppetInstruction>();
			string line; 
			while ((line = m_file.ReadLine ()) != null) 
			{
				PuppetInstruction instruction; 
				if ((instruction = ParseInstruction (line)) != null) 
				{
					puppetInstructions.Enqueue (instruction);
					DebugLogic ("Added instruction {0} {1}", instruction.Type, instruction.ApplyToUser);
				} 
				else 
				{
					DebugInfo ("Skipping instruction: {0}", line);
				}
			}
			
			return puppetInstructions;			
		}
		
		public PuppetInstruction ParseInstruction (string line) 
		{
			PuppetInstruction instruction = new PuppetInstruction ();
			string[] type = line.Split (' ');
			
			if (type[0].Equals ("connect")) 
			{
				//if (type[1].StartsWith ("central"))
				//{
				//	return null;
				//}
				instruction.Type = PuppetInstructionType.CONNECT;
				instruction.ApplyToUser = type[1].Trim ();				
			}
			else if (type[0].Equals ("disconnect")) {
				instruction.Type = PuppetInstructionType.DISCONNECT;
				instruction.ApplyToUser = type[1].Trim ();
			}
			else if (type[0].Equals ("readCalendar")) 
			{
				instruction.Type = PuppetInstructionType.READ_CALENDAR;
				instruction.ApplyToUser = type[1].Trim ();
			}
			else if (type[0].Equals ("reservation")) 
			{
				instruction.Type = PuppetInstructionType.RESERVATION;
														
				if (PuppetConfigurationReader.AttachReservationData(instruction, line) == false)
				{
					instruction = null;
				}
			}
			else 
			{
				instruction = null;
			}
			
			return instruction;
		}
		
		private static bool AttachReservationData (PuppetInstruction instruction, string line) 
		{
			// parse: "reservation {GroupMeeting; user1, user2; 13, 25 }"
			try 
			{
				// description
				string[] r = line.Substring (10).Split (';');			
				instruction.Description = r[0].Substring(3).Trim ();				
				
				// apply to user
				string[] data = r[1].Split (',');				
				instruction.ApplyToUser = data[0].Trim ();				
				
				// users
				instruction.Users = new List<string>();	
				instruction.Users.Add (instruction.ApplyToUser);
				for (int i = 1; i < data.Length; i++) 
				{
					instruction.Users.Add (data[i].Trim ()); // all other users							
				}				
				
				// slots				
				string[] slots = r[2].Substring (0,r[2].Length-1).Split (',');
				instruction.Slots = new List<string>();
				for (int i = 0; i < data.Length; i++) 
				{
					instruction.Slots.Add (slots[i].Trim ());				
				}
			} 
			catch (Exception) 
			{
				return false;
			}
			return true; 
		}
	}
}
