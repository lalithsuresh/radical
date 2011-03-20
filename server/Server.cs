using System;
using comm;

namespace server
{
	public class Server
	{
		// Server settings
		private int m_serverId; 
		
		// Services Layer components
		private UserTableServiceServer m_userTableService; 
		private SequenceNumberServiceServer m_sequenceNumberService;
		
		// Communication Layer components
		private SendReceiveMiddleLayer m_sendReceiveMiddleLayer; 
		private PerfectPointToPointSend m_perfectPointToPointSend; 
		
		public Server ()
		{
		}
		
		public void InitServer (int port) 
		{
			// Services Layer
			m_userTableService = new UserTableServiceServer();
			m_userTableService.SetCallback(this);
			m_sequenceNumberService = new SequenceNumberServiceServer();
			m_sequenceNumberService.SetCallback(this);
			
			// Communication Layer
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer();
			m_perfectPointToPointSend = new PerfectPointToPointSend();
			
			m_perfectPointToPointSend.Start(m_sendReceiveMiddleLayer, port);
			m_sendReceiveMiddleLayer.SetPointToPointInterface(m_perfectPointToPointSend);
			
			// Server references
			m_serverId = GetServerIdFromConfig ();
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
			Console.WriteLine ("Shutdown.");
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
			 	Console.WriteLine ("Cannot determine server id, will shutdown.");
				Shutdown ();
			}
			return id;
		}
	}
}

