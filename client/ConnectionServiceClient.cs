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
			m_client.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("disconnect",
			                                                           new ReceiveCallbackType (Receive));
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Connect ()
		{
			Message m = new Message ();
			m.SetMessageType ("connect");
			m.SetSourceUserName (m_client.UserName);
			m.SetDestinationUsers ("SERVER");
			
			DebugLogic ("Sending connection request");
			m_client.m_sendReceiveMiddleLayer.Send (m);
			
			//This thread will block here until the reset event is sent.
			m_oSignalEvent.WaitOne();
			m_oSignalEvent.Reset ();
		}
		
		public void Disconnect ()
		{
			// TODO: Don't forget to notify all peers about leave at this point
			Message m = new Message ();
			m.SetMessageType ("disconnect");
			m.SetSourceUserName (m_client.UserName);
			m.SetDestinationUsers ("SERVER");
			
			DebugLogic ("Sending disconnection request");
			m_client.m_sendReceiveMiddleLayer.Send (m);
			
			// TODO: For now, no need to block on disconnect
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			// TODO: Change to connect ACK type or something
			
			DebugLogic ("Got {0} ACK", eventargs.m_message.GetMessageType ());
			m_oSignalEvent.Set ();
		}
	}
}

