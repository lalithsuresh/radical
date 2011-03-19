using System;
namespace comm
{
	public class PadicalObject
	{
		bool m_debugFlag = false;
		
		public PadicalObject ()
		{
		}
		
		public virtual void Debug (bool value)
		{
			m_debugFlag = value;
			Console.WriteLine ("Debug method set");
		}
		
	}
}