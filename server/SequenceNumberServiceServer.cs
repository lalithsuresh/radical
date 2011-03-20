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
		
		public void SetServer (Server server) 
		{
			if (server != null) 
			{
				m_server = server;
			}
			
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("GetSequenceNumber", new ReceiveCallbackType (Receive));
		}
		
		
		/**
		 * Get a unique sequence number
		 */
		public long GetSequenceNumber ()
		{
			m_sequenceNumber++;
			return m_sequenceNumber;
		}
		
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			Message message = eventargs.m_message;
			string source = message.GetSource ();
			string request = message.GetMessageType ();
			if (request.Equals ("GetSequenceNumber"))
			{
				// do cool stuff 
			}
			SendReply (source, GetSequenceNumber ());
		}
		
		private void SendReply (string destination, long sequenceNumber) 
		{
			Message reply = new Message ();
			reply.SetDestination (destination);
			reply.PushString (sequenceNumber.ToString ());
			m_server.Send (reply);
		}
	}
}

