using System;
using System.Collections.Generic;

namespace comm
{
	[Serializable]
	public class Message
	{
		private string m_type;
		private string m_sourceUserName;
		private List<string> m_recipientUserNames;
		private Stack<object> m_items;
		
		public Message ()
		{
			m_items = new Stack<object>();
			m_recipientUserNames = new List<string> ();
		}
		
		/**
		 * Returns the type of the message for demuxing
		 */
		public string GetMessageType () 
		{
			return m_type;
		}
		
		public void SetMessageType (string type)
		{
			m_type = type;
		}		
		
		public string GetSource ()
		{
			return m_sourceUserName;
		}
		
		public void SetSource (string source)
		{
			m_sourceUserName = source;
		}
				
		public List<string> GetDestinations () 
		{
			return m_recipientUserNames;	
		}
		
		/** 
		 * Pass a list of users that this message is intended for. 
		 * Note: duplicate users will recieve duplicate messages. 
		 */
		public void SetDestination (List<string> destinations) 
		{
			m_recipientUserNames.AddRange(destinations);	
		}
		
		/**
		 * Add a specific user as destination of this message. 
		 * Note: if already added, user will receive message twice. 
		 */
		public void SetDestination (string user) 
		{
			if (user != null) 
			{
				m_recipientUserNames.Add(user);
			}
		}
			
		public void PushString (string s) 
		{
			if (s != null)
			{
				m_items.Push(s);
			}
		}
		
		public string PopString () 
		{
			if (m_items.Peek() is string)
			{
				return (string)m_items.Pop(); 
			}
			else
			{
				return null;
			}
		}
	}
}

