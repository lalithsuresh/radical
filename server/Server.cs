using System;
using comm;

namespace server
{
	public class Server
	{
		// Services Layer components
		private UserTableServiceServer m_userTableService; 
		
		// Communication Layer components
		private SendReceiveMiddleLayer m_sendReceiveMiddleLayer; 
		private PerfectPointToPointSend m_perfectPointToPointSend; 
		
		public Server ()
		{
		}
		
		public void InitServer () 
		{
			// Services Layer
			m_userTableService = new UserTableServiceServer();
			m_userTableService.SetCallback(this);
			
			// Communication Layer
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer();
			m_perfectPointToPointSend = new PerfectPointToPointSend();
			
			m_perfectPointToPointSend.Start(m_sendReceiveMiddleLayer, 8080);
			m_sendReceiveMiddleLayer.SetPointToPointInterface(m_perfectPointToPointSend);
		}
		
		public void Send (Message m) 
		{
			m_sendReceiveMiddleLayer.Send(m);
		}
	}
}

