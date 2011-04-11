using System;
using System.Collections.Generic;
using System.Text;
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
				Clients.Add(m.GetSourceUserName (), m.GetSourceUri ());
			} 
			else if (m.GetMessageType ().Equals ("puppet_info")) 
			{
				string msg = String.Format ("PuppetInfo [{0}] {1}", m.GetSourceUserName (), m.PopString ());
				m_puppetMaster.NotifySubscribers (msg);
			}
			else
			{
				DebugLogic ("Received unknown message."); 
			}
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
	}
}
