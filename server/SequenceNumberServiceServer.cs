using System;
using comm;
using common;
using System.Threading;
using System.Runtime.CompilerServices;

namespace server
{
	public class SequenceNumberServiceServer : PadicalObject, IServiceServer
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
		
		public bool SetSequenceNumber (long number) 
		{
			//if (!m_server.m_replicationService.IsMaster && m_sequenceNumber < number) 
			if (m_sequenceNumber < number)
			{
				DebugInfo ("Updating sequence number on replication request. Old: {0} New: {1}",
				           m_sequenceNumber, number);
				m_sequenceNumber = number;
				return true;
			}
			else 
			{
				DebugLogic ("Sequence number already up to date / use mine");
				return false;
			}
		}
		
		/**
		 * Get a unique sequence number
		 * TODO: Talk with all servers before doing
		 * this.
		 */
		public long GetSequenceNumber ()
		{
			// TODO: check if I am master and should reply to this, or forward
			lock (this)
			{
				m_sequenceNumber++;
				return m_sequenceNumber;
			}
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			Message message = eventargs.m_message;									
			
			// All we need from the request is the source
			// uri.
			DebugInfo ("Server {0} got request for sequence number from {1}", m_server.UserName, message.GetSourceUserName ());
			
			// Create response to the requester
			Message response = new Message ();
			response.SetSourceUri (m_server.UserName);
			response.SetDestinationUsers (message.GetSourceUserName ());
			response.SetMessageType ("sequencenumber");
						
			long nextSequenceNumber = GetSequenceNumber ();
			
			
			// this is implictly a blocking call
			long sequenceNumberResponse = m_server.m_replicationService.ReplicateSequenceNumber (nextSequenceNumber);
			
			if (sequenceNumberResponse > nextSequenceNumber)
			{				
				nextSequenceNumber = sequenceNumberResponse;
			}
				
			response.PushString (nextSequenceNumber.ToString ());
			
			m_server.m_sendReceiveMiddleLayer.Send (response);
		}		
		
		public void PrintSequenceNumber () 
		{
			Console.WriteLine ("Sequence number: ");
			Console.WriteLine ("> {0}", m_sequenceNumber);
		}
				
	}
}