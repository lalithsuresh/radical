using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace radical
{
	public class ServerSample
	{
		public ServerSample ()
		{
		}
		
		public static void Main() 
		{
			// Set up remoting service
			TcpChannel channel = new TcpChannel(8080);
			ChannelServices.RegisterChannel(channel, false);
			
			// register public services
			RemotingConfiguration.RegisterWellKnownServiceType(typeof(ServerRemotingServicesSample),
			                                                   "Send",
			                                                   WellKnownObjectMode.Singleton);
			
			Console.WriteLine("Press enter to exit...");
			Console.ReadLine();
		}
	}
}

