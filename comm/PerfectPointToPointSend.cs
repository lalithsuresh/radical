using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.CompilerServices;
using System.Threading;
using common;

namespace comm
{
	public class PerfectPointToPointSend : PadicalObject
	{ 
		private string CHANNEL_NAME = "Radical";
		private IDictionary m_channelProperties = new Hashtable ();
        public ManualResetEvent e = new ManualResetEvent(false);

		// members
		private SendReceiveMiddleLayer m_sendReceiveMiddleLayer;
		private TcpChannel m_channel;
		private PointToPointInterface m_pointToPoint; 
		private ObjRef m_marshalRef;
		
		// delegates
	    public delegate void RemoteAsyncDelegate(Message m);

		// properties
		public bool Paused {
			get;
			set;
		}
		
		// for debugging/development purposes only
		private PointToPointInterface m_dummy = null;
		
		public PerfectPointToPointSend ()
		{
			m_channelProperties["name"] = "tcpRadical";
			CHANNEL_NAME = "Radical";	
		}
		
		public PerfectPointToPointSend (bool isPuppetChannel) 
		{
			if (isPuppetChannel) 
			{				
				m_channelProperties["name"] = "tcpPuppet";				
				CHANNEL_NAME = "Puppet";
			} 			
		}
		
		/**
		 * This constructor is experimental and for stress testing only!
		 */
		public PerfectPointToPointSend (string channelName)
		{
			CHANNEL_NAME = channelName;
			m_channelProperties["name"] = String.Format("tcp{0}",channelName);
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
			
			// Default is active
			Paused = false;
			
			m_channelProperties["port"] = port.ToString ();
			
			string useDummyRecipient = ConfigReader.GetConfigurationValue ("dummyrecipient");
			if (useDummyRecipient != null && useDummyRecipient.Equals ("true"))
			{
				m_dummy = new DummyPointToPointSend ();
				m_dummy.Init (demuxer);
			}
			// create sending interfaces
			m_pointToPoint = new PointToPointInterface ();
			m_pointToPoint.Init (demuxer);
						
			// register tcp channel and connect p2p interface				
			m_channel = new TcpChannel (m_channelProperties, null, null);
			ChannelServices.RegisterChannel (m_channel, false);
			m_marshalRef = RemotingServices.Marshal (m_pointToPoint, CHANNEL_NAME, typeof(PointToPointInterface));
			
			return true;
		}
		
		public void Stop ()
		{
			RemotingServices.Disconnect (m_pointToPoint);			
		}
		
		public bool Pause ()
		{
			if (!Paused) 
			{
				DebugInfo ("Pausing communication layer.");
				RemotingServices.Unmarshal (m_marshalRef);
				Paused = true;
				return true;
			}
			return false;
		}
		
		public bool Unpause ()
		{
			if (Paused)
			{
				DebugInfo ("Ready to rock \\o/ again.");
				m_marshalRef = RemotingServices.Marshal (m_pointToPoint, CHANNEL_NAME, typeof(PointToPointInterface));			
				Paused = false;
				return true;
			}
			return false;
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
				DebugLogic ("Sending to {0}", uri);
				p2p_send = (PointToPointInterface) Activator.GetObject (typeof (PointToPointInterface), uri);
			} 
			else
			{
				p2p_send = m_dummy;
			}
			
			// ohoy!
			if (p2p_send == null)
			{
				DebugLogic ("Crash! Cannot find recipient: {0}", uri);	
			}
			else
			{
				AsyncCallback cb = new AsyncCallback(MyCallBack);
                RemoteAsyncDelegate d = new RemoteAsyncDelegate (p2p_send.Deliver);
				d.BeginInvoke(m, cb, null);
				
				e.WaitOne ();
			}
		}
		
		public void MyCallBack(IAsyncResult ar)
		{
    		RemoteAsyncDelegate d = (RemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
    		d.EndInvoke(ar);
    		e.Set();
		}
	}
	
}

