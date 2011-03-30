using System;
using common;
using NUnit.Framework;

namespace client
{
	
	[TestFixture()]
	public class ConnectionManagerTest
	{
		private Client m_client;
		
		public ConnectionManagerTest ()
		{
			ConfigReader.ReadFile ("/home/nightstrike/programming/mono/radical/client/client.config");
			m_client = new Client();
			m_client.InitClient ();			
		}
		
		[Test()]
		public void ConnectDisconnectTest ()
		{
			Assert.IsTrue(m_client.Connect());
		}
		
		[Test()]
		public void SequenceNumberTest ()
		{
			Assert.AreEqual (1, m_client.GetSequenceNumber ());
			Assert.AreEqual (2, m_client.GetSequenceNumber ());
			Assert.AreEqual (3, m_client.GetSequenceNumber ());
		}
		
	}
}

