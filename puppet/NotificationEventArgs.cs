using System;

namespace puppet
{
	public class NotificationEventArgs : EventArgs
	{
		public string Notification {
			get;
			set;
		}

		public NotificationEventArgs (string text)
		{
			Notification = text;
		}
	}
}
