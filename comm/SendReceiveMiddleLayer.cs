using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections.Generic;

namespace comm
{
	public delegate void ReceiveCallbackType(object sender, EventArgs e);
	
	public class SendReceiveMiddleLayer : PadicalObject
	{
		private const string CHANNEL_NAME = "Radical";
		private TcpChannel m_channel;
		private ObjRef m_remoteReference;
		private PerfectPointToPointSend m_transmitter; 
		private Dictionary<string, ReceiveCallbackType> m_registerMap = new Dictionary<string, ReceiveCallbackType> ();

		
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
			
			// register tcp channel and connect p2p interface
			m_channel = new TcpChannel(port);
			ChannelServices.RegisterChannel(m_channel, false);
			m_remoteReference = RemotingServices.Marshal(m_transmitter, CHANNEL_NAME, typeof(PointToPointInterface));
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
				ChannelDataStore data = (ChannelDataStore) m_channel.ChannelData;
				uri = new System.Uri(data.ChannelUris[0]).AbsoluteUri;
			}
			
			return uri + CHANNEL_NAME;
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
		
		public void RegisterReceiveCallback (String service, ReceiveCallbackType cb)
		{
			m_registerMap.Add (service, cb);
		}
	}
}
