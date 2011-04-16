using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using common;
using comm;

namespace puppet
{

	public class PuppetMasterService : PadicalObject
	{
		private PuppetMaster m_puppetMaster; 
		private const int START_PORT = 12000;
		private int m_currentPortOffset;
		
		public Dictionary<string, Process> SpawnedClients {
			get;
			set;
		}
		
		public Dictionary<string, string> Clients {
			get;
			set;
		}

		public PuppetMasterService ()
		{
			Clients = new Dictionary<string, string> ();
			SpawnedClients = new Dictionary<string, Process> ();
			m_currentPortOffset = 0;
		}
		
		public void SetPuppetMaster (PuppetMaster master)
		{
			if (master == null) 
			{
				DebugFatal ("PuppetMaster not set");
			}
			
			m_puppetMaster = master;
			
			m_puppetMaster.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("puppet_register", 
			                                                                 new ReceiveCallbackType (Receive));
			m_puppetMaster.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("puppet_info", 
			                                                                 new ReceiveCallbackType (Receive));
			
		}
		
		public bool CommandClient (PuppetInstruction instruction)  
		{
			if (!Clients.ContainsKey(instruction.ApplyToUser)) 
			{
				if (instruction.Type == PuppetInstructionType.CONNECT)
				{
					DebugLogic ("Starting a new client: {0}", instruction.ApplyToUser);
					SpawnClient (instruction.ApplyToUser);
					// client will register with puppet master when initing, safe to return
					return true;
				}
				
				DebugLogic ("No such user is connected: {0}", instruction.ApplyToUser);
				return false;
			}		
			
			Message m = new Message ();
			m.SetSourceUserName ("puppetmaster");
			m.SetDestinationUsers (instruction.ApplyToUser);
			m.SetMessageType ("puppetmaster");
						
			switch (instruction.Type) 
			{
			case PuppetInstructionType.RESERVATION: 
				m.PushString (BuildStringList (instruction.Slots));
				m.PushString (BuildStringList (instruction.Users));
				m.PushString (instruction.Description);
				m.PushString (instruction.Type.ToString ());
				break;				
			default:
				m.PushString (instruction.Type.ToString ());
				break;
			}
			
			m_puppetMaster.m_sendReceiveMiddleLayer.Send (m);
			
			return true;
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs) 
		{
			Message m = eventargs.m_message;								
			
			if (m.GetMessageType ().Equals ("puppet_register")) 
			{				
				string msg = String.Format ("REGISTERED {0} for remote control.", m.GetSourceUserName ());				
				m_puppetMaster.NotifySubscribers (msg, m.GetSourceUserName ());
				HandleClientRegistration (m.GetSourceUserName (), m.GetSourceUri ());				
			} 
			else if (m.GetMessageType ().Equals ("puppet_info")) 
			{
				string msg = String.Format ("PuppetInfo [{0}] {1}", m.GetSourceUserName (), m.PopString ());
				m_puppetMaster.NotifySubscribers (msg, m.GetSourceUserName ());
			}
			else
			{
				DebugLogic ("Received unknown message."); 
			}
		}
		
		private void HandleClientRegistration (string username, string uri)
		{
			Clients.Add(username, uri);
		}
		
		private void SpawnClient (string username) 
		{
			Process client = new Process ();
			client.StartInfo.Verb = "client.exe";
			client.StartInfo.FileName = m_puppetMaster.ClientExecutable; 
			client.StartInfo.Arguments = String.Format ("{0} {1} {2}", username, START_PORT+m_currentPortOffset, 
			                                m_puppetMaster.GenericConfig);
			client.StartInfo.UseShellExecute = false;
			
			try 
			{
				client.Start ();
			} 
			catch (InvalidOperationException ioe) 
			{				
				DebugLogic ("Could not start client: {0}\n{1}", username, ioe.Message);				
			}
			
			SpawnedClients.Add (username, client);
			m_currentPortOffset++;
		}
		
		private string BuildStringList (List<string> list)  
		{
			StringBuilder builder = new StringBuilder ();
			
			for (int i = 0; i < list.Count; i++) 
			{
				builder.Append (list[i]);		
				
				if (i < list.Count-1) 
				{
					builder.Append (",");
				}
			}					
			
			return builder.ToString ();
		}
		
		public void Cleanup () 
		{
			foreach (Process c in SpawnedClients.Values) 
			{
				c.Kill ();
			}
		}
	}
}
