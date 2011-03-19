using System;
namespace comm
{
	public class GroupMulticast : PadicalObject
	{
		private PointToPointInterface m_transmitter; 
		
		public GroupMulticast (PointToPointInterface transmitter)
		{
			m_transmitter = transmitter;
		}
	}
}

