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
	public delegate string LookupCallbackType(string user);
	
	public class SendReceiveMiddleLayer : PadicalObject
	{
		// services registered
		private Dictionary<string, ReceiveCallbackType> m_registerMap = new Dictionary<string, ReceiveCallbackType> ();

		// comm components
		private PerfectPointToPointSend m_perfectPointToPoint;
		
		// lookup callback
		private LookupCallbackType m_lookupCallback;
		
		public SendReceiveMiddleLayer ()
		{
		}
		
		public void SetPointToPointInterface (PerfectPointToPointSend p2p) 
		{
			if (p2p != null)
			{
				m_perfectPointToPoint = p2p;
			}
			else
			{
				DebugFatal ("FATAL: SetPointToPointInterface received null p2p pointer");
			}
		}
		
		public void SetLookupCallback (LookupCallbackType cb)
		{
			m_lookupCallback = cb;
		}
		
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
				DebugFatal ("Received a message with an unknown type");
			}
		}
		
		/**
		 * Send a message to remote clients or servers
		 */
		public void Send (Message m) 
		{
			// TODO: Later, we want to perform whole lookups
			// for a list together instead of one at a time
			// which is lolz.
			// inspect message destinations, if multiple, use group_multicast else just send with p2p
			List<string> destinations = m.GetDestinationUsers ();
			DebugInfo ("Please help {0}", destinations);
			foreach (string destination in destinations) 
			{
				DebugInfo ("Performing lookup for {0}", destination);
				// TODO: If server returns NO SUCH USER, then bail out
				string destination_uri = m_lookupCallback (destination); // for now, assume destination is uri
				DebugInfo ("Lookup returned {0}", destination_uri);
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
