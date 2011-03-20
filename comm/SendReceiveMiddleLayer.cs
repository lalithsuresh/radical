using System;
using common;
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
		}
		
		public void SetPointToPointInterface (PerfectPointToPointSend p2p) 
		{
			if (p2p != null) 
				m_perfectPointToPoint = p2p;
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Deliver (Message m) 
		{
			DebugInfo ("Got: {0}", m.GetType());
			
			// Extract the message type from the message
			// and look for corresponding protocol handler
			// in the registerMap
			if (m_registerMap.ContainsKey (m.GetMessageType ()))
			{
				DebugInfo ("Received {0} message", m.GetMessageType ());
				m_registerMap [m.GetMessageType ()] (new ReceiveMessageEventArgs (m));
			}
			else
			{
				DebugInfo ("Fatal error: Received a message with an unknown type");
				Environment.Exit (0);
			}
		}
		
		/**
		 * Send a message to remote clients or servers
		 */
		public void Send (Message m) 
		{
			// inspect message destinations, if multiple, use group_multicast else just send with p2p
			List<string> destinations = m.GetDestinations ();
			foreach (string destination in destinations) 
			{
				string destination_uri = destination; // for now, assume destination is uri
				m_perfectPointToPoint.Send(m, destination_uri);
			}
		}
		
		public void RegisterReceiveCallback (String service, ReceiveCallbackType cb)
		{
			DebugLogic ("Registered subscriber for {0} events", service);
			m_registerMap.Add (service, cb);
		}
	}
}
