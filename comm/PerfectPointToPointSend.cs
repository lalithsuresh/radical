using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace comm
{
	public class PerfectPointToPointSend
	{ 
		private const string CHANNEL_NAME = "Radical";
		private TcpChannel m_channel;
		private PointToPointInterface m_pointToPoint; 
		
		public PerfectPointToPointSend ()
		{
			// empty
		}
		
		/**
		 * Start a remote listening interface on a specific port
		 * Servers: use unique ports
		 * Clients: may use same port (unless run on same machine)
		 */
		public bool Start (SendReceiveMiddleLayer demuxer, int port) 
		{	
			if (demuxer == null) 
				return false; 
			
			// create sending interfaces
			m_pointToPoint = new PointToPointInterface ();
			m_pointToPoint.Init (demuxer);
			
			// register tcp channel and connect p2p interface
			m_channel = new TcpChannel (port);
			ChannelServices.RegisterChannel (m_channel, false);
			RemotingServices.Marshal (m_pointToPoint, CHANNEL_NAME, typeof(PointToPointInterface));
			
			return true;
		}
		
		public void Stop ()
		{
			RemotingServices.Disconnect (m_pointToPoint);
		}
		
		public string GetURI () 
		{
			string uri = "";
			
			if (m_channel != null) 
			{
				ChannelDataStore data = (ChannelDataStore) m_channel.ChannelData;
				uri = new System.Uri (data.ChannelUris[0]).AbsoluteUri;
			}
			
			return uri + CHANNEL_NAME;
		}
		
		public void Send (Message m, string uri) 
		{
			// get reference to remote object 
			PointToPointInterface p2p_send = (PointToPointInterface) 
				Activator.GetObject (typeof (PointToPointInterface), uri);
			
			// ohoy!
			p2p_send.Deliver (m);	
		}
	}
	
}

