using System;

namespace puppet
{
	public class NotificationEventArgs : EventArgs
	{
		public string UserName {
			get;
			set;
		}
		
		public string Notification {
			get;
			set;
		}

		public NotificationEventArgs (string text)
		{
			Notification = text;
		}
		
		public NotificationEventArgs (string text, string user) 
		{
			Notification = text;
			UserName = user;
		}
	}
}
