using System;
using common;

namespace puppet
{


	public class LookupServicePuppet : PadicalObject
	{
		PuppetMaster m_master;

		public LookupServicePuppet ()
		{
		}
		
		public void SetPuppetMaster (PuppetMaster master) 
		{
			m_master = master;
		}
		
		
		// puppet master only needs to know where the servers are
		public string Lookup (string user) 
		{
			// Lookup service knows the servers.
			// It will also check the cache at a later stage of
			// development.
			if (user.Equals ("SERVER"))
			{
				// TODO: Return server 1. Will be changed later
				return m_master.ServerList [0]; 
			} 
			
			DebugFatal ("Couldn't not find server address");
			
			return "";
		}
	}
}
