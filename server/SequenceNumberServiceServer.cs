using System;
using comm;
using System.Threading;
using System.Runtime.CompilerServices;

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
			
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("sequencenumber", new ReceiveCallbackType (Receive));
		}
		
		
		/**
		 * Get a unique sequence number
		 * TODO: Talk with all servers before doing
		 * this.
		 */
		[MethodImpl(MethodImplOptions.Synchronized)]
		public long GetSequenceNumber ()
		{
			m_sequenceNumber++;
			return m_sequenceNumber;
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			// All we need from the request is the source
			// uri.
			Message message = eventargs.m_message;
			
			// Create response to the requester
			Message response = new Message ();
			response.SetSourceUri (m_server.UserName);
			response.SetDestinationUsers (message.GetSourceUserName ());
			response.SetMessageType ("sequencenumber");
			
			// In future, this will probably be a blocking call
			response.PushString (GetSequenceNumber ().ToString ());
			
			m_server.m_sendReceiveMiddleLayer.Send (response);
		}
	}
}