using System;
using System.Collections.Generic;
using common;
using comm;

namespace server
{
	public class PuppetServerService : PadicalObject, IServiceServer
	{
		public const string PUPPET_MASTER = "PUPPETMASTER";
		private Server m_server; 

		public PuppetServerService ()
		{
		}
		
		public void SetServer (Server server) 
		{
			if (server == null) 
			{
				DebugFatal ("Could not set server");
			}
			
			m_server = server;
			
			m_server.m_puppetSendReceiveMiddleLayer.RegisterReceiveCallback("puppetmaster", 
			                                        	                    new ReceiveCallbackType(Receive));
						
		}
		
		public void RegisterAsPuppet () 
		{
			Message m = new Message ();
			m.SetMessageType ("puppet_register");
			m.SetSourceUserName (m_server.UserName);
			m.SetDestinationUsers (PUPPET_MASTER);
			DebugInfo ("Registering server as puppet.... ");
			m_server.m_puppetSendReceiveMiddleLayer.Send (m);
		} 
		
		public void SendInfoMsgToPuppetMaster (string message, params object[] args) 
		{
			Message m = new Message ();
			m.SetMessageType ("puppet_info");
			m.SetSourceUserName (m_server.UserName);
			m.SetDestinationUsers (PUPPET_MASTER);
			m.PushString (String.Format (message, args));
			m_server.m_puppetSendReceiveMiddleLayer.Send (m);
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs) 
		{
			Message m = eventargs.m_message;
			string type = m.PopString ();				
			
			if (String.Compare (type, "connect", true) == 0)
			{
				m_server.Unpause ();
				SendInfoMsgToPuppetMaster ("Resumed main communication channel");
				DebugInfo ("Puppet Master says: Start Server");
			}
			else if (String.Compare (type, "disconnect", true) == 0)
			{
				m_server.Pause ();
				SendInfoMsgToPuppetMaster ("Paused main communication channel");
				DebugInfo ("Puppet Master says: Stop Server");
			}			
		}
		
	}
}
