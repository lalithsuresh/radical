using System;
using System.Collections.Generic;
using comm;

namespace server
{
	public class UserTableServiceServer : PadicalObject
	{
		
		private Dictionary<string, string> m_usertable;
		
		public UserTableServiceServer () 
		{
			m_usertable = new Dictionary<string, string> ();
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
		
		public void Receive (Message m) 
		{
			// parse message and call appropriate action
			return;
		}
	}
}

