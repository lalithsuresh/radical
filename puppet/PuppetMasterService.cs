using System;
using System.Collections.Generic;
using common;
using comm;

namespace puppet
{

	public class PuppetMasterService : PadicalObject
	{
		private PuppetMaster m_puppetMaster; 
		
		public Dictionary<string, string> Clients {
			get;
			set;
		}

		public PuppetMasterService ()
		{
			Clients = new Dictionary<string, string> ();
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
				DebugLogic ("No such client registered at puppet master: {0}", instruction.ApplyToUser);
				return false;
			}
			
			DebugLogic ("Commanding client {0} to {1}", 
			            instruction.ApplyToUser, 
			            instruction.Type);
			
			Message m = new Message ();
			m.SetSourceUserName ("puppetmaster");
			m.SetDestinationUsers (instruction.ApplyToUser);
			m.SetMessageType ("puppetmaster");
						
			switch (instruction.Type) 
			{
			case PuppetInstructionType.RESERVATION: 
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
				DebugLogic ("Registering user {0} for remote control.", m.GetSourceUserName ());
				Clients.Add(m.GetSourceUserName (), m.GetSourceUri ());
			} 
			else if (m.GetMessageType ().Equals ("puppet_info")) 
			{
				DebugInfo ("[{0}] - {1}", m.GetSourceUserName (), m.PopString ());
			}
			else
			{
				DebugLogic ("Received unknown message."); 
			}
		}
	}
}
