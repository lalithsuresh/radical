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
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public int RequestSequenceNumber ()
		{
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
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			Message m = eventargs.m_message;
			
			m_sequenceNumberToReturn = Int32.Parse (m.PopString ());
			oSignalEvent.Set ();
		}
	}
}

