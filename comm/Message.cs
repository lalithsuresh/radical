using System;
using System.Text;
using System.Collections.Generic;

namespace comm
{
	[Serializable]
	public class Message : ICloneable
	{
		private string m_type;
		private string m_sourceUserName;
		private string m_sourceUri;
		
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
		
		public string GetSourceUserName ()
		{
			return m_sourceUserName;
		}
		
		public void SetSourceUserName (string source)
		{
			m_sourceUserName = source;
		}
		
		public string GetSourceUri ()
		{
			return m_sourceUri;
		}
		
		public void SetSourceUri (string source)
		{
			m_sourceUri = source;
		}
		
		public List<string> GetDestinationUsers () 
		{
			return m_recipientUserNames;	
		}
		
		/** 
		 * Pass a list of users that this message is intended for. 
		 * Note: duplicate users will recieve duplicate messages. 
		 */
		public void SetDestinationUsers (List<string> destinations) 
		{
			m_recipientUserNames.AddRange(destinations);	
		}
		
		/**
		 * Add a specific user as destination of this message. 
		 * Note: if already added, user will receive message twice. 
		 */
		public void SetDestinationUsers (string user) 
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
		
		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.AppendLine (string.Format("[Message] {0} ", GetMessageType ()));
			
			sb.AppendLine ("To: ");
			foreach (string s in m_recipientUserNames) 
			{
				sb.AppendLine (s);
			}
			
			sb.AppendLine ("Stack: ");
			foreach (string s in m_items) 
			{
				sb.AppendLine (s);
			}
			
			return sb.ToString ();
		}
		
		public object Clone ()
		{
			Message m = new Message ();
			m.m_items = this.m_items;
			return m;
		}

	}
}

