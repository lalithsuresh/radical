using System;
using System.Threading;
using System.Runtime.CompilerServices;
using comm;

namespace client
{
	public class LookupServiceClient : comm.PadicalObject
	{
		ManualResetEvent m_oSignalEvent = new ManualResetEvent (false);
		Client m_client;
		
		public LookupServiceClient ()
		{
		}
		
		public void SetClient (Client client)
		{
			m_client = client;
			m_client.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("lookup",
			                                                           new ReceiveCallbackType (Receive));
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Lookup ()
		{
			Message m = new Message ();
			
			m.SetSource ("testuser1");
			m.SetDestination ("server1");
			m.SetMessageType ("lookup");
			m.PushString ("testuser2");
			
			m_client.m_sendReceiveMiddleLayer.Send (m);
				
			//This thread will block here until the reset event is sent.
			Console.WriteLine ("Sent Lookup request");
			m_oSignalEvent.WaitOne();
			Console.WriteLine ("Releasing block");
			m_oSignalEvent.Reset ();
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			m_oSignalEvent.Set ();
		}
	}
}