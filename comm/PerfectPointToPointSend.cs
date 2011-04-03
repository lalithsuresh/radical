using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.CompilerServices;
using System.Threading;
using common;

namespace comm
{
	public class PerfectPointToPointSend : PadicalObject
	{ 
		private const string CHANNEL_NAME = "Radical";
		private SendReceiveMiddleLayer m_sendReceiveMiddleLayer;
		private TcpChannel m_channel;
		private PointToPointInterface m_pointToPoint; 
		
		// for debugging/development purposes only
		private PointToPointInterface m_dummy = null;
		
		public PerfectPointToPointSend ()
		{
		}
		
		/**
		 * Start a remote listening interface on a specific port
		 * Servers: use unique ports
		 * Clients: may use same port (unless run on same machine)
		 */
		public bool Start (SendReceiveMiddleLayer demuxer, int port) 
		{	
			if (demuxer == null) 
			{
				DebugFatal ("Received null demuxer");
			}
			
			string useDummyRecipient = m_configReader.GetConfigurationValue ("dummyrecipient");
			if (useDummyRecipient != null && useDummyRecipient.Equals ("true"))
			{
				m_dummy = new DummyPointToPointSend ();
				m_dummy.Init (demuxer);
			}
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
			m.SetSourceUri (GetURI ());
			
			// get reference to remote object 
			PointToPointInterface p2p_send;
			if (m_dummy == null) 
			{
				p2p_send = (PointToPointInterface) Activator.GetObject (typeof (PointToPointInterface), uri);
			} 
			else
			{
				p2p_send = m_dummy;
			}
			
			// ohoy!
			p2p_send.Deliver (m);
		}
	}
	
}

