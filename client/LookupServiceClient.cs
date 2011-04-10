using System;
using System.Threading;
using System.Runtime.CompilerServices;
using comm;
using common;

namespace client
{
	public class LookupServiceClient : PadicalObject
	{
		ManualResetEvent m_oSignalEvent = new ManualResetEvent (false);
		Client m_client;
		string m_lookupResponse;
		
		public LookupServiceClient ()
		{
		}
		
		public void SetClient (Client client)
		{
			m_client = client;
			m_client.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("lookup",
			                                                           new ReceiveCallbackType (Receive));
		}
		
		public string Lookup (string user)
		{
			lock (this){
				// TODO: This should later move into a lookup for
				// a list of users.
				// TODO: It should also implement a cache
				// to avoid remote invocations all the time.
				// TODO: Avoid restricted "SERVER" username
				// registrations at server end.
				
				// Lookup service knows the servers.
				// It will also check the cache at a later stage of
				// development.
				if (user.Equals ("SERVER"))
				{
					// Return server 1. Will be changed later.
					return m_client.ServerList [0]; 
				}
				
				// If not SERVER or in cache, create a new
				// lookup message destined to SERVER. Eventually,
				// lower layer will query for SERVER, and this
				// method will answer it.
				Message m = new Message ();
				m.SetSourceUserName (m_client.UserName);
				m.SetDestinationUsers ("SERVER");
				m.SetMessageType ("lookup");
				m.PushString (user); // This should change to a list later
				
				m_client.m_sendReceiveMiddleLayer.Send (m);
				// This thread will block here until the reset event is sent.
				// In this case, it happens when the Receive() method is invocated
				// upon getting a response.
				DebugInfo ("Sent Lookup request");
				m_oSignalEvent.WaitOne();
				DebugInfo ("Releasing block");
				m_oSignalEvent.Reset ();
				
				return m_lookupResponse;
			}
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			Message m = eventargs.m_message;
			
			// Update response string and manually
			// reset the waiting Lookup() thread
			m_lookupResponse = m.PopString ();
			m_oSignalEvent.Set ();
		}
	}
}