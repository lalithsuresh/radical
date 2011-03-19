using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace comm
{
	public class PerfectPointToPointSend : MarshalByRefObject, PointToPointInterface
	{
		private SendReceiveMiddleLayer m_demuxer; 
		
		public PerfectPointToPointSend (SendReceiveMiddleLayer demuxer)
		{
			m_demuxer = demuxer;
		}
		
		public void Deliver (Message m)
		{
			// deliver to incoming queue in SendRecvMiddleLayer
			m_demuxer.Deliver(m);
		}
		
		public void Send (Message m, string uri) 
		{
			// get reference to remote object 
			PointToPointInterface p2p_send = (PointToPointInterface) 
				Activator.GetObject(typeof(PointToPointInterface), uri);
			
			// ohoy!
			p2p_send.Deliver(m);	
		}
	}
	
}

