using System;
using System.Collections.Generic;

namespace comm
{
	[Serializable]
	public class Message
	{
		private string m_type; 
		private List<string> m_destinations; 
		private Stack<object> m_items;
		
		/** 
		 * Use this function to create a new message. 
		 * Ensures that all messages are created in the same way.
		 */
		public static Message Create (string type) 
		{
			Message msg = new Message();
			msg.m_type = type;
			return msg;
		}
		
		/**
		 * Returns the type of the message for demuxing
		 */
		public static string GetType (Message m) 
		{
			return m.m_type;
		}
		
		public static List<string> GetDestinations (Message m) 
		{
			return m.m_destinations;	
		}
		
		private Message ()
		{
			m_items = new Stack<object>();
		}
		
		/** 
		 * Pass a list of users that this message is intended for. 
		 * Note: duplicate users will recieve duplicate messages. 
		 */
		public void SetDestination (List<string> destinations) 
		{
			m_destinations.AddRange(destinations);	
		}
		
		/**
		 * Add a specific user as destination of this message. 
		 * Note: if already added, user will receive message twice. 
		 */
		public void SetDestination (string user) 
		{
			if (user != null) 
				m_destinations.Add(user); 
		}
			
		public void PushString (string s) 
		{
			if (s != null) 
				m_items.Push(s);
		}
		
		public string PopString () 
		{
			if (m_items.Peek() is string)
				return (string)m_items.Pop(); 
			else
				return null;
		}
	}
}

