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
		
		public void Send (Message m)
		{
			// deliver to incoming queue in SendRecvMiddleLayer
			m_demuxer.Deliver(m);
		}
	}
	
	/*public class PerfectPointToPointSend : PadicalObject
	{
		
		public PerfectPointToPointSend ()
		{
			// empty
		}
		
		
		public void Send (string uri, Message m)
		{
			Console.WriteLine("Sending message to remote instance");
		}	
		
	}*/
	
	
}

