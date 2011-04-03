using System;

namespace common
{
	public class PadicalObject
	{
		protected string m_debugLevel;
		string m_objectName;
		public ConfigReader m_configReader = new ConfigReader ();
		
		public PadicalObject ()
		{
			m_objectName = this.ToString ();
			
			string debuglevel = m_configReader.GetConfigurationValue (this.ToString ());
			
			if (debuglevel != null && (debuglevel.Equals ("info")
			                           || debuglevel.Equals ("all")
			                           || debuglevel.Equals ("logic")))
			{
				m_debugLevel = debuglevel;
			}
			else
			{
				m_debugLevel = ""; // No debugging for this object
			}
		}
				
		public virtual void SetDebug (string val)
		{
			m_debugLevel = val;
		}
		
		public virtual void DebugUncond (string debugstr, params object[] args)
		{
			Debug ("", debugstr, args);
		}
		
		public virtual void DebugFatal (string debugstr, params object[] args)
		{
			Debug ("fatal", debugstr, args);
			Environment.Exit (1);
		}
		
		public virtual void DebugInfo (string debugstr, params object[] args)
		{
			if (m_debugLevel.Equals ("info") || m_debugLevel.Equals ("all"))
			{
				Debug (m_debugLevel, debugstr, args);
			}
		}
		
		public virtual void DebugLogic (string debugstr, params object[] args)
		{
			if (m_debugLevel.Equals ("logic") || m_debugLevel.Equals ("all"))
			{
				Debug (m_debugLevel, debugstr, args);
			}
		}
		
		protected virtual void Debug (string type, string debugstr, params object[] args)
		{
			Console.WriteLine (type + ":" + m_objectName + ": " + debugstr, args);
		}
		
	}
}