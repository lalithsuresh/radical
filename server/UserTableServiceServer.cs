using System;
using System.Collections.Generic;
using comm;

namespace server
{
	public class UserTableServiceServer : PadicalObject, IServiceServer
	{
		
		private Server m_server;
		private Dictionary<string, string> m_usertable;
		
		
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
		
		public void RegisterUser (string username, string uri) 
		{
			if (username == null || uri == null) 
				return;
			
			m_usertable.Add(username, uri);
		}
		
		public void RemoveUser (string username) 
		{
			if (username == null)
				return;
			
			if (!m_usertable.ContainsKey(username))
				return;
			
			m_usertable.Remove(username);
		}
		
		public string GetUriForUser (string username) 
		{
			if (username == null)
				return "Bad Request";
			
			if (!m_usertable.ContainsKey(username))
				return "No such user";
			
			return m_usertable[username];
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs) 
		{
			// parse message and call appropriate action
			Message m = eventargs.m_message;
			string source = m.GetSource ();
			string request = m.GetMessageType ();
			
			if (request.Equals ("lookup")) 
			{
				//string username = m.PopString();
				//string reply = GetUriForUser(username);
				//SendReply(source, reply);
			}
			else if (request.Equals ("connect")) 
			{
				//string username = m.PopString();
				//string uri = m.PopString();
				//RegisterUser(username, uri);
			}
			else if (request.Equals ("disconnect")) 
			{
				//string username = m.PopString();
				//RemoveUser(username);
			}
			
			return;
		}
		
		private void SendReply (string destination, string reply) 
		{
			Message message = new Message();
			message.SetDestination(destination);
			message.PushString(reply);
			message.PushString("UriForUser");
			m_server.Send(message);
		}
	}
}

