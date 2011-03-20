using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections.Generic;

namespace comm
{
	public delegate void ReceiveCallbackType(ReceiveMessageEventArgs e);
	
	public class SendReceiveMiddleLayer : PadicalObject
	{
		// services registered
		private Dictionary<string, ReceiveCallbackType> m_registerMap = new Dictionary<string, ReceiveCallbackType> ();

		// comm components
		private PerfectPointToPointSend m_perfectPointToPoint; 
		
		public SendReceiveMiddleLayer ()
		{
			// empty
		}
		
		public void SetPointToPointInterface (PerfectPointToPointSend p2p) 
		{
			if (p2p != null) 
				m_perfectPointToPoint = p2p;
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Deliver (Message m) 
		{
			Console.WriteLine("Got: {0}", m.GetType());
			
			// Extract the message type from the message
			// and look for corresponding protocol handler
			// in the registerMap
			if (m_registerMap.ContainsKey (m.GetMessageType ()))
			{
				Console.WriteLine ("Received {0} message", m.GetMessageType ());
				m_registerMap [m.GetMessageType ()] (new ReceiveMessageEventArgs (m));
			}
			else
			{
				Console.WriteLine ("Fatal error: Received a message with an unknown type");
				Environment.Exit (0);
			}
		}
		
		/**
		 * Send a message to remote clients or servers
		 */
		public void Send (Message m) 
		{
			// inspect message destinations, if multiple, use group_multicast else just send with p2p
			m_perfectPointToPoint.Send(m, "tcp://localhost:8080/Radical");
		}
		
		public void RegisterReceiveCallback (String service, ReceiveCallbackType cb)
		{
			Console.WriteLine ("Registered subscriber for {0} events", service);
			m_registerMap.Add (service, cb);
		}
	}
}
