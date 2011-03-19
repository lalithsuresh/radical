using System;
using comm;

namespace server
{
	public class Server
	{
		// services
		private UserTableServiceServer m_userTableService; 
		
		// comm
		private SendReceiveMiddleLayer m_comm; 
		
		public Server ()
		{
		}
		
		public void InitServer () 
		{
			// services
			m_userTableService = new UserTableServiceServer();
			m_userTableService.SetCallback(this);
			
			// comm
			m_comm = new SendReceiveMiddleLayer();
		}
		
		public void Send (Message m) 
		{
			m_comm.Send(m);
		}
	}
}

