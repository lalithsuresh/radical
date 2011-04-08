using System;
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
			string type = m.GetMessageType ();
			
			if (type.Equals ("connect"))
			{
				DebugInfo ("Got connect command from Puppet Master");
			}
			else if (type.Equals ("disconnect"))
			{
				DebugInfo ("Got disconnect command from Puppet Master");
			}
			else if (type.Equals ("reservation")) 
			{
				DebugInfo ("Got reservation command from Puppet Master");				
			}
			else if (type.Equals ("readCalendar")) 
			{
				DebugInfo ("Got readCalendar command from Puppet Master");
			}
		}
	}
}
