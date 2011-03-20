using System;
using comm;
using common;

namespace server
{
	public class Server : PadicalObject
	{
		// Server settings
		private int m_serverId; 
		
		// Services Layer components
		private UserTableServiceServer m_userTableService; 
		private SequenceNumberServiceServer m_sequenceNumberService;
		
		// Communication Layer components
		public SendReceiveMiddleLayer m_sendReceiveMiddleLayer; 
		private PerfectPointToPointSend m_perfectPointToPointSend; 
		
		public Server ()
		{
		}
		
		public void InitServer (int port) 
		{
			// Communication Layer
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer ();
			m_perfectPointToPointSend = new PerfectPointToPointSend ();
			
			m_perfectPointToPointSend.Start (m_sendReceiveMiddleLayer, 8080);
			m_sendReceiveMiddleLayer.SetPointToPointInterface (m_perfectPointToPointSend);
			
			// Services Layer
			m_userTableService = new UserTableServiceServer ();
			m_userTableService.SetServer (this);
			
			m_sequenceNumberService = new SequenceNumberServiceServer ();
			m_sequenceNumberService.SetServer (this);
			
			// Server references
			m_serverId = GetServerIdFromConfig ();
			
			// Determine current master
			// (now it's only me) 
			m_userTableService.RegisterUser ("server1", ConfigReader.GetConfigurationValue ("server1"));
		}
		
		/**
		 * Relays messages from services layer to communication layer
		 */ 
		public void Send (Message m) 
		{
			m_sendReceiveMiddleLayer.Send (m);
		}
		
		public void Shutdown () 
		{
			DebugLogic ("Shutdown.");
			m_perfectPointToPointSend.Stop ();	
			Environment.Exit(1);
		}
		
		private int GetServerIdFromConfig () 
		{
			int id = -1; 
			string serverInstance = ConfigReader.GetConfigurationValue ("id");
			string serverId = serverInstance.Substring (6);
			try 
			{
				id = Int32.Parse (serverId);
			} 
			catch (Exception) 
			{
			 	DebugInfo ("Cannot determine server id, will shutdown.");
				Shutdown ();
			}
			return id;
		}
	}
}

