using System;
using comm;
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
		
		public void SetCallback (Server server) 
		{
			if (server != null) 
				m_server = server;
		}
		
		
		/**
		 * Get a unique sequence number
		 */
		public long GetSequenceNumber ()
		{
			m_sequenceNumber++;
			return m_sequenceNumber;
		}
		
		
		public void Receive (Message message)
		{
			string source = message.GetSource ();
			string request = message.PopString ();
			SendReply (source, GetSequenceNumber ());
		}
		
		private void SendReply (string destination, long no) 
		{
			Message reply = new Message ();
			reply.SetDestination (destination);
			reply.PushString (no.ToString ());
			m_server.Send (reply);
		}
	}
}

