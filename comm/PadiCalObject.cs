using System;
namespace comm
{
	public class PadicalObject
	{
		string m_debugLevel;
		string m_objectName;
		
		public PadicalObject ()
		{
			m_objectName = this.ToString ();
			m_debugLevel = "";
		}
				
		public virtual void SetDebug (string val)
		{
			m_debugLevel = val;
		}
		
		public virtual void DebugUncond (string debugstr, params object[] args)
		{
			Debug (debugstr, args);
		}
		
		public virtual void DebugInfo (string debugstr, params object[] args)
		{
			if (m_debugLevel.Equals ("info") || m_debugLevel.Equals ("all"))
			{
				Debug (debugstr, args);
			}
		}
		
		public virtual void DebugLogic (string debugstr, params object[] args)
		{
			if (m_debugLevel.Equals ("logic") || m_debugLevel.Equals ("all"))
			{
				Debug (debugstr, args);
			}
		}
		
		protected virtual void Debug (string debugstr, params object[] args)
		{
			Console.WriteLine (m_objectName + ": " + debugstr, args);
		}
		
	}
}