using System;

namespace puppet
{
	public class UserStatus
	{
		public string UserName;
		public string Status;
		
		public UserStatus (string user, string status)
		{
			UserName = user;
			Status = status;
		}
	}
}
