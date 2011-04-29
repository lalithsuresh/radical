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
	public delegate string RotateMasterCallbackType();
	
	public class SendReceiveMiddleLayer : PadicalObject
	{
		// services registered
		private Dictionary<string, ReceiveCallbackType> m_registerReceiveMap = new Dictionary<string, ReceiveCallbackType> ();
		private Dictionary<string, ReceiveCallbackType> m_registerFailureReceiveMap = new Dictionary<string, ReceiveCallbackType> ();
				
		// comm components
		private PerfectPointToPointSend m_perfectPointToPoint;
		private List<Message> m_deferredSendMessages = new List<Message> ();
		private List<string> m_deferredSendDestination = new List<string> ();
		private Timer m_deferredSendTimer;
		
		// lookup callback
		private LookupCallbackType m_lookupCallback;
		
		// rotate master callback
		private RotateMasterCallbackType m_rotateMasterCallback;
		
		public SendReceiveMiddleLayer ()
		{
			m_deferredSendTimer = new Timer (DeferredSend, null, 5000, 5000);
			RegisterReceiveCallback ("resend", new ReceiveCallbackType (ResendToAnotherNode));
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
		
		public void SetRotateMasterCallback (RotateMasterCallbackType cb)
		{
			m_rotateMasterCallback = cb;
		}
		
		public void Deliver (Message m) 
		{						
			DebugInfo ("Got: {0}", m.GetType());
			
			// Extract the message type from the message
			// and look for corresponding protocol handler
			// in the registerMap
			if (m_registerReceiveMap.ContainsKey (m.GetMessageType ()))
			{
				DebugInfo ("Received {0} message", m.GetMessageType ());
				m_registerReceiveMap [m.GetMessageType ()] (new ReceiveMessageEventArgs (m));
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
			List<string> destinationUris = new List<string> ();
			List<string> destinationcopy = new List<string> ();
			foreach (string destination in destinations) 
			{
				//DebugInfo ("Performing lookup for {0}", destination);
				// TODO: If server returns NO SUCH USER, then bail out
				string destination_uri = m_lookupCallback (destination); // for now, assume destination is uri
				//DebugInfo ("Lookup returned {0}", destination_uri);
				
				destinationUris.Add (destination_uri);
				destinationcopy.Add (destination);
			}
			
			foreach (string uri in destinationUris)
			{				
				Send (m, uri, destinationcopy[0]);
				destinationcopy.RemoveAt (0);
			}
		}
		
		// Use this to send out directly
		public void Send (Message m, string uri, string destination)
		{
			try
			{
				m_perfectPointToPoint.Send(m, uri);
			}
			catch
			{				
				m_deferredSendDestination.Add (destination);
				m_deferredSendMessages.Add (m);
				DebugInfo ("Adding to deferred queue type:{0}, dst:{1}, size:{2}",m.GetMessageType (), destination, m_deferredSendMessages.Count);
			}
		}
		
		public void ResendToAnotherNode (ReceiveMessageEventArgs eventargs)
		{
			DebugLogic ("Resending to another node");
			Message m = eventargs.m_message;			
			string uri = m.PopString ();
			m_rotateMasterCallback ();
			Send (m.MessageForResending, uri, "SERVER");
		}
		
		// Use this Send mechanism to _not_ retry for failed
		// send attempts
		public void UnreliableSend (Message m, string uri)
		{
			m_perfectPointToPoint.Send(m, uri);		
		}	
		
		public void DeferredSend (object state)
		{
			Message [] msgarr = new Message[m_deferredSendMessages.Count];
			string [] dstarr = new string[m_deferredSendDestination.Count];
			
			if (m_deferredSendDestination.Count != m_deferredSendMessages.Count)
			{
				DebugFatal ("Number of deferred messages and destinations do not match");
			}
			
			msgarr = m_deferredSendMessages.ToArray ();
			dstarr = m_deferredSendDestination.ToArray ();
			
			int count = m_deferredSendMessages.Count;
			int i = 0;			
			
			m_deferredSendMessages.Clear ();
			m_deferredSendDestination.Clear ();		
			
			DebugInfo ("INITIATING DEFERREDSEND for {0} {1} msgs", count, m_deferredSendDestination.Count);
			
			while (i < count)
			{
				Message msg = msgarr[i];
				string dst = dstarr[i];
				
				DebugInfo ("DeferredSending msg type {0}, to {1}", msg.GetMessageType (), dst);
				if (dst.Equals ("SERVER"))
				{
					string newserver = m_rotateMasterCallback();
					DebugInfo ("Rotate server returned {0}", newserver);
					Send (msg, newserver, dst);
				}
				else
				{
					string uriretry = m_lookupCallback (dst);
					Send (msg, uriretry, dst);
				}
				
				i++;
			}
		}
		
		public void RegisterReceiveCallback (String service, ReceiveCallbackType cb)
		{
			DebugLogic ("Registered subscriber for {0} events", service);
			m_registerReceiveMap.Add (service, cb);
		}
		
		public void RegisterFailureCallback (String service, ReceiveCallbackType cb)
		{
			DebugLogic ("Registered failure subscriber for {0} events", service);
			m_registerFailureReceiveMap.Add (service, cb);
		}
	}
}
