using System;
using comm;

namespace server
{
	public class Server
	{
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
			m_userTableService.RegisterUser("server1", ConfigReader.GetConfigurationValue("server1"));
			m_userTableService.RegisterUser("server2", ConfigReader.GetConfigurationValue("server2"));
			m_userTableService.RegisterUser("server3", ConfigReader.GetConfigurationValue("server3"));
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
			m_perfectPointToPointSend.Stop ();	
		}
	}
}

