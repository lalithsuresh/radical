using System;
namespace comm
{
	public class ReceiveMessageEventArgs : EventArgs
	{
		public Message m_message;
		public ReceiveMessageEventArgs (Message m)
		{
			m_message = m;
		}		
	}
}

