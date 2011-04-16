using System;
using System.Threading;
using System.Runtime.CompilerServices;
using common;
using comm;

namespace client
{
	public class SequenceNumberServiceClient : PadicalObject
	{
		ManualResetEvent oSignalEvent = new ManualResetEvent (false);
		private Client m_client;
		private int m_sequenceNumberToReturn;
		
		public SequenceNumberServiceClient ()
		{
		}
		
		public void SetClient (Client client)
		{
			m_client = client;
			m_client.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("sequencenumber",
			                                                           new ReceiveCallbackType (Receive));
			m_client.m_sendReceiveMiddleLayer.RegisterFailureCallback ("sequencenumber",
			                                                           new ReceiveCallbackType (ReceiveFailure));		
		}
		
		public int RequestSequenceNumber ()
		{
			lock (this){
				Message m = new Message ();
				m.SetSourceUserName (m_client.UserName);
				m.SetDestinationUsers ("SERVER");
				m.SetMessageType ("sequencenumber");
				
				m_client.m_sendReceiveMiddleLayer.Send (m);
				//This thread will block here until the reset event is sent.
				oSignalEvent.WaitOne();
				oSignalEvent.Reset ();
				
				return m_sequenceNumberToReturn;
			}
		}
		
		public void ReceiveFailure (ReceiveMessageEventArgs eventargs)
		{
			Message m = eventargs.m_message;
			
			// Update response string and manually
			// reset the waiting Lookup() thread
			m_client.RotateMaster ();
			m_client.m_sendReceiveMiddleLayer.Send (m, m_client.CurrentMasterServer, "SERVER");
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			Message m = eventargs.m_message;
			
			m_sequenceNumberToReturn = Int32.Parse (m.PopString ());
			oSignalEvent.Set ();
		}
	}
}

