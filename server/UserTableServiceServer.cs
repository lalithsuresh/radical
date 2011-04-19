using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using comm;
using common;

namespace server
{
	public class UserTableServiceServer : PadicalObject, IServiceServer
	{
		
		private Server m_server;
		private Dictionary<string, string> m_usertable;
		
		public Dictionary<string,string> UserTable {
			get 
			{
				return m_usertable;
			}			
		}
		
		public UserTableServiceServer () 
		{
			m_usertable = new Dictionary<string, string> ();
		}
		
		public void SetServer (Server server) 
		{
			m_server = server;
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("lookup", new ReceiveCallbackType (Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("connect", new ReceiveCallbackType (Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("disconnect", new ReceiveCallbackType (Receive));
			
		}
		
		public void UserConnect (string username, string uri) 
		{
			if (username == null || uri == null)
			{
				DebugFatal ("FATAL: Received null username or uri");
			}
			
			// Because it's OK if we get multiple Connect
			// requests from the same user, just don't
			// re-enter.
			if (!m_usertable.ContainsKey (username))
			{
				DebugLogic ("Adds [{0},{1}] to database", username, uri);
				m_usertable.Add(username, uri);
			}
			
			//PrintUserTable ();
		}
		
		public void UserDisconnect (string username) 
		{
			if (username == null)
			{
				DebugFatal ("null username received in UserDisconnect");
			}
			
			if (!m_usertable.ContainsKey(username))
			{
				DebugFatal ("Disconnect message received for non-registered user");
			}
			
			// Remove user from DB
			DebugLogic ("Removes [{0}] from database", username);
				
			m_usertable.Remove(username);
			
			//PrintUserTable ();
		}
		
		public string GetUriForUser (string username) 
		{
			if (username == null)
			{
				// crash
				return "Bad Request";
			}
			
			if (!m_usertable.ContainsKey(username))
			{
				// crash
				return "No such user";
			}
			
			return m_usertable[username];
		}
		
		public string Lookup (string user)
		{
			return GetUriForUser (user);
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Receive (ReceiveMessageEventArgs eventargs) 
		{
			// parse message and call appropriate action
			Message m = eventargs.m_message;
			string message_source = m.GetSourceUserName ();
			string request_type = m.GetMessageType ();
			string message_source_uri = m.GetSourceUri ();
			
			if (request_type.Equals ("lookup")) 
			{
				// Client has requested a username lookup,
				// respond with the URI string for the
				// user
				string user_request = m.PopString ();
				string uri = GetUriForUser (user_request);
				
				DebugInfo ("Answering lookup for {0} with {1}", user_request, uri);
				
				Message response = new Message ();
				response.SetMessageType ("lookup");
				response.SetDestinationUsers (message_source);
				response.SetSourceUserName (m_server.UserName);
				response.PushString (uri);
				SendReply(response);
			}
			else if (request_type.Equals ("connect")) 
			{				
				// add user to DB				
				UserConnect (message_source, message_source_uri);
				
				// Send an ACK or the client starves to death
				Message ack_message = new Message ();
				ack_message.SetMessageType ("connect");
				ack_message.SetDestinationUsers (message_source);
				ack_message.SetSourceUserName (m_server.UserName);
				
				// no need to check if user has same name as that is not in spec
				// replicate message (implicit block)
				m_server.m_replicationService.ReplicateUserConnect (message_source, message_source_uri);
								
				SendReply (ack_message);
			}
			else if (request_type.Equals ("disconnect")) 
			{					
				// remove from replicas (implicit block until 1 ack is received)
				m_server.m_replicationService.ReplicateUserDisconnect (message_source);
				
				UserDisconnect (message_source);
				
				// TODO: Find a way to get an ACK across
				// for a disconnect message
			}
			
			return;
		}
		
		private void SendReply (Message m) 
		{
			m_server.m_sendReceiveMiddleLayer.Send (m);
		}
		
		public void PrintUserTable ()
		{
			Console.WriteLine ("UserTable:");
			foreach (string user in m_usertable.Keys) 
			{
				Console.WriteLine ("> {0}\t {1}", user, m_usertable[user]);
			}
		}
	}
}

