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
			
			m_client.m_sendReceiveMiddleLayer.RegisterReceiveCallback("puppetmaster", 
			                                                          new ReceiveCallbackType(Receive));
						
		}
		
		public void RegisterAsPuppet () 
		{
			Message m = new Message ();
			m.SetMessageType ("puppet_register");
			m.SetSourceUserName (m_client.UserName);
			m.SetDestinationUsers (PUPPET_MASTER);
			
			m_client.m_sendReceiveMiddleLayer.Send (m);
		} 
		
		public void SendInfoMsgToPuppetMaster (string message) 
		{
			Message m = new Message ();
			m.SetMessageType ("puppet_info");
			m.SetSourceUserName (m_client.UserName);
			m.SetDestinationUsers (PUPPET_MASTER);
			m.PushString (message);
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs) 
		{
			Message m = eventargs.m_message;
			string type = m.PopString ();				
			
			if (String.Compare (type, "connect", true) == 0)
			{
				DebugInfo ("Puppet Master says: Connect");
				m_client.Connect ();
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
				DebugInfo ("Puppet Master says: Reserve ({0} {1} {2})", 
				           description, userstringlist, slotstringlist);		
				// m_client.Reserve (description, userlist, slotlist);
			}
			else if (String.Compare (type, "readcalendar", true) == 0) 
			{
				DebugInfo ("Puppet Master says: ReadCalendar");
				// TODO implement read calendar interface
			}
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
