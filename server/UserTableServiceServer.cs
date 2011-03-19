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
		
		public void SetCallback (Server server) 
		{
			if (server != null) 
				m_server = server;
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
		
		public void Receive (Message m) 
		{
			// parse message and call appropriate action
			string source = m.GetSource();
			string request = m.PopString();
			
			if (request.Equals("GetUriForUser")) 
			{
				string username = m.PopString();
				string reply = GetUriForUser(username);
				SendReply(source, reply);
			}
			else if (request.Equals("Register")) 
			{
				string username = m.PopString();
				string uri = m.PopString();
				RegisterUser(username, uri);
			}
			else if (request.Equals("Remove")) 
			{
				string username = m.PopString();
				RemoveUser(username);
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

