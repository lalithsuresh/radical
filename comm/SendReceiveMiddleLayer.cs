using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace comm
{
	public class SendReceiveMiddleLayer : PadicalObject
	{
		private TcpChannel m_channel;
		private ObjRef m_remoteReference;
		private PerfectPointToPointSend m_transmitter; 
		private GroupMulticast m_groupMulticast; 
		
		public SendReceiveMiddleLayer ()
		{
			// empty
		}
	
		
		/**
		 * Start a remote listening interface on a specific port
		 * Servers: use unique ports
		 * Clients: may use same port (unless run on same machine)
		 */
		public void Start (int port) 
		{			
			// create sending interfaces
			m_transmitter = new PerfectPointToPointSend(this);
			m_groupMulticast = new GroupMulticast(m_transmitter);
			
			// register tcp channel and connect p2p interface
			m_channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(m_channel, false);
			m_remoteReference = RemotingServices.Marshal(m_transmitter, "Radical", typeof(PointToPointInterface));
		}
		
		public void Stop ()
		{
			RemotingServices.Disconnect(m_transmitter);
		}
		
		public string GetURI () 
		{
			string uri = "";
			
			if (m_channel != null) 
			{
				uri = "somecooluri";
			}
			
			return uri;
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Deliver (Message m) 
		{
			Console.WriteLine("SendRecv got: {0}", Message.GetType(m));
		}
		
		/**
		 * Send a message to remote clients or servers
		 */
		public void Send (Message m) 
		{
			// inspect message destinations, if multiple, use group_multicast else just send with p2p
			m_transmitter.Send(m, "tcp://localhost:8081/Radical");
		}
	}
}

