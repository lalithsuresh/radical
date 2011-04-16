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
	public delegate void RotateMasterCallbackType();
	
	public class SendReceiveMiddleLayer : PadicalObject
	{
		// services registered
		private Dictionary<string, ReceiveCallbackType> m_registerReceiveMap = new Dictionary<string, ReceiveCallbackType> ();
		private Dictionary<string, ReceiveCallbackType> m_registerFailureReceiveMap = new Dictionary<string, ReceiveCallbackType> ();
				
		// comm components
		private PerfectPointToPointSend m_perfectPointToPoint;
		private List<Message> m_deferredSendMessages = new List<Message> ();
		private List<string> m_deferredSendUris = new List<string> ();
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
				DebugInfo ("Performing lookup for {0}", destination);
				// TODO: If server returns NO SUCH USER, then bail out
				string destination_uri = m_lookupCallback (destination); // for now, assume destination is uri
				DebugInfo ("Lookup returned {0}", destination_uri);
				
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
				Message msgclone = (Message) m.Clone ();
				m_deferredSendMessages.Add (msgclone);
				m_deferredSendUris.Add (destination);
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
		
		private void DeferredSend (object state)
		{			
			// Retry every 5 seconds
			
			// TODO: Verify if the below sequence is race condition
			// free.
			
			Message [] msgarr = new Message [m_deferredSendMessages.Count];
			string [] uriarr = new string [m_deferredSendUris.Count];
			
			// sanity check
			if (m_deferredSendMessages.Count != m_deferredSendUris.Count)
			{
				DebugFatal ("#DeferredMessages != #DeferredUris");
			}
							
			msgarr = m_deferredSendMessages.ToArray ();
			uriarr = m_deferredSendUris.ToArray ();
			
			int count = m_deferredSendMessages.Count;			
			int i = 0;
			while (i < count)
			{				
				Message m = msgarr[i];
				string uri = uriarr[i];
								
				DebugInfo ("DeferredSend for message of type {0}", m.GetMessageType ());
				
				m_deferredSendMessages.Remove (m);
				m_deferredSendUris.Remove (uri);
				
				// Always do another lookup since
				// the other end might be on a 
				// different uri after reconnection
				string bleh = m_lookupCallback (uri);
				if (m_registerFailureReceiveMap.ContainsKey (m.GetMessageType ())				    )
				{						
					m_registerFailureReceiveMap [m.GetMessageType ()] (new ReceiveMessageEventArgs (m));
				}
				else
				{
					Send (m, bleh, uri);
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
