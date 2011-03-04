using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace radical
{
	public class ClientSample
	{
		public ClientSample ()
		{
			// empty
		}
		
		public static void Main () 
		{
			TcpChannel channel = new TcpChannel();
			ChannelServices.RegisterChannel(channel, false);
			
			// get a reference to the remote object
			ServerRemotingServicesSample sample = 
				(ServerRemotingServicesSample) Activator.GetObject(typeof(ServerRemotingServicesSample),
				                                                   "tcp://localhost:8080/Send");
			
			// now we can start using it hopefully
			if (sample == null) 
			{
				Console.WriteLine("Error occurred!");
			}
			else
			{
				sample.Send(); // only increments counter
			}
		}
	}
}

