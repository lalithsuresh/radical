using System;
namespace server
{
	public class SequenceNumberServiceServer : IServiceServer
	{
		private Server m_server; 
		private long m_sequenceNumber; 
		
		public SequenceNumberServiceServer ()
		{
			m_sequenceNumber = 0;
		}
		
		public long GetSequenceNumber ()
		{
			m_sequenceNumber++;
			return m_sequenceNumber;
		}
		
		public void SetCallback (Server server) 
		{
			if (server != null) 
				m_server = server;
		}
		
		public void Receive (Message message)
		{
		}
		
		
	}
}

