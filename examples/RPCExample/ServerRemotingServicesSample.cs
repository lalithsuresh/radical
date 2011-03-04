using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace radical
{
	public class ServerRemotingServicesSample : MarshalByRefObject
	{
		private int m_remoteCallsCounter = 0;
		
		public ServerRemotingServicesSample ()
		{
			// empty
		}
		
		public void Send() 
		{
			m_remoteCallsCounter++;
			Console.WriteLine("Counter is {0}", m_remoteCallsCounter);
		}
		
	}
}

