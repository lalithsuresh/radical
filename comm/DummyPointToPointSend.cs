using System;
namespace comm
{
	public class DummyPointToPointSend : PointToPointInterface
	{	
		public DummyPointToPointSend ()
		{
		}
		
		override public void Deliver (Message m)
		{
			m_demuxer.Deliver(m);
		}
	}
}

