using System;
using System.Threading;
using System.Runtime.CompilerServices;
using comm;
using common;

namespace client
{
	public class ConnectionServiceClient : PadicalObject
	{
		ManualResetEvent m_oSignalEvent = new ManualResetEvent (false);
		Client m_client;
		
		public ConnectionServiceClient ()
		{
		}
		
		public void SetClient (Client client)
		{
			m_client = client;
			m_client.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("connect",
			                                                           new ReceiveCallbackType (Receive));
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Connect ()
		{
			//This thread will block here until the reset event is sent.
			m_oSignalEvent.WaitOne();
			m_oSignalEvent.Reset ();
		}
		
		public void Disconnect ()
		{
			// Don't forget to notify all peers about leave at this point
			
			//This thread will block here until the reset event is sent.
			m_oSignalEvent.WaitOne();
			m_oSignalEvent.Reset ();
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			m_oSignalEvent.Set ();
		}
	}
}

