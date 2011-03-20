using System;

namespace comm
{
	public class PointToPointInterface : MarshalByRefObject
	{
		private SendReceiveMiddleLayer m_demuxer; 
			
		public PointToPointInterface () 
		{
			// empty
		}
		
		public void Init (SendReceiveMiddleLayer demuxer) 
		{
			if (demuxer != null) 
				m_demuxer = demuxer;
		}
		
		public void Deliver (Message m) {
			// deliver to incoming queue in SendRecvMiddleLayer
			m_demuxer.Deliver(m);
		}
	}
}

