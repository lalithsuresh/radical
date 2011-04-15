using System;
using System.Collections.Generic;
using common;
using comm;

namespace client
{
	public class PuppetClientService : PadicalObject
	{
		private const string PUPPET_MASTER = "PUPPETMASTER";
		private Client m_client; 

		public PuppetClientService ()
		{
		}
		
		public void SetClient (Client client) 
		{
			if (client == null) 
			{
				DebugFatal ("Could not set client");
			}
			
			m_client = client;
			
			m_client.m_puppetSendReceiveMiddleLayer.RegisterReceiveCallback("puppetmaster", 
			                                        	                    new ReceiveCallbackType(Receive));
						
		}
		
		public void RegisterAsPuppet () 
		{
			Message m = new Message ();
			m.SetMessageType ("puppet_register");
			m.SetSourceUserName (m_client.UserName);
			m.SetDestinationUsers (PUPPET_MASTER);
			
			m_client.m_puppetSendReceiveMiddleLayer.Send (m);
		} 
		
		public void SendInfoMsgToPuppetMaster (string message, params object[] args) 
		{
			Message m = new Message ();
			m.SetMessageType ("puppet_info");
			m.SetSourceUserName (m_client.UserName);
			m.SetDestinationUsers (PUPPET_MASTER);
			m.PushString (String.Format (message, args));
			m_client.m_puppetSendReceiveMiddleLayer.Send (m);
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs) 
		{
			Message m = eventargs.m_message;
			string type = m.PopString ();				
			
			if (String.Compare (type, "connect", true) == 0)
			{
				DebugInfo ("Puppet Master says: Connect");
				if (m_client.Connect ()) 
				{					
					SendInfoMsgToPuppetMaster ("{0} connected", m_client.UserName);
				}
			}
			else if (String.Compare (type, "disconnect", true) == 0)
			{
				DebugInfo ("Puppet Master says: Disconnect");
				m_client.Disconnect ();
			}
			else if (String.Compare (type, "reservation", true) == 0) 
			{				
				string description = m.PopString ();
				string userstringlist = m.PopString ();
				string slotstringlist = m.PopString ();
				
				List<string> userlist = BuildList (userstringlist);
				List<string> slotlist = BuildList (slotstringlist);
				
				List<int> slots = ConvertToInts(slotlist);
				
				DebugInfo ("Puppet Master says: Reserve (Description: {0} Users: {1} Slots: {2})", 
				           description, userstringlist, slotstringlist);	
				
				m_client.Reserve (description, userlist, slots);
			}
			else if (String.Compare (type, "readcalendar", true) == 0) 
			{
				DebugInfo ("Puppet Master says: ReadCalendar");
				SendInfoMsgToPuppetMaster (m_client.ReadCalendar ());				
			}
		}
		
		private List<int> ConvertToInts (List<string> slotlist) 
		{
			List<int> ints = new List<int> ();
			foreach (string s in slotlist)
			{
				try 
				{
					ints.Add (Int32.Parse (s));
				} 
				catch (FormatException)
				{
					DebugFatal ("Could not convert slot (string) from message to slot (int)");
				}
			}
			return ints;
		}
		
		private List<string> BuildList (string stringlist) 
		{
			List<string> list = new List<string> ();
			string[] items = stringlist.Split (',');
			for (int i = 0; i < items.Length; i++) 
			{
				list.Add (items[i].Trim ());
			}
			return list;
		}
				
	}
}
