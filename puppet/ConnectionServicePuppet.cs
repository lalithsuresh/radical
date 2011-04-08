using System;
using System.Threading;
using System.Runtime.CompilerServices;
using comm;
using common;

namespace puppet
{
	public class ConnectionServicePuppet : PadicalObject
	{

		ManualResetEvent m_oSignalEvent = new ManualResetEvent (false);
		PuppetMaster m_master;
		
		public ConnectionServicePuppet ()
		{
		}
		
		public void SetPuppetMaster (PuppetMaster master)
		{
			m_master = master;
			m_master.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("connect",
			                                                           new ReceiveCallbackType (Receive));
			m_master.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("disconnect",
			                                                           new ReceiveCallbackType (Receive));
		}
		
		public bool Connect ()
		{
			Message m = new Message ();
			m.SetMessageType ("connect");
			m.SetSourceUserName (m_master.UserName);
			m.SetDestinationUsers ("SERVER");
			
			DebugLogic ("Sending connection request");			
			m_master.m_sendReceiveMiddleLayer.Send (m);
			
			//This thread will block here until the reset event is sent.
			m_oSignalEvent.WaitOne();
			m_oSignalEvent.Reset ();
			
			return true;
		}
		
		public bool Disconnect ()
		{
			// TODO: Don't forget to notify all peers about leave at this point
			Message m = new Message ();
			m.SetMessageType ("disconnect");
			m.SetSourceUserName (m_master.UserName);
			m.SetDestinationUsers ("SERVER");
			
			DebugLogic ("Sending disconnection request");
			m_master.m_sendReceiveMiddleLayer.Send (m);
			
			return true;
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
