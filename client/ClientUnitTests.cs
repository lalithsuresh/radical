using System;
using System.Diagnostics;
using System.Collections.Generic;
using common;
using NUnit.Framework;

namespace client
{
	/*
	 * Use these unit tests to test the Client
	 * or server side individually.
	 * NOTE: Make sure you have a server instance
	 * running.
	 * TODO: Launch server instances from within
	 * this class to do all our validations at the
	 * click of a button!
	 * (Protocol/class validation)
	 */
	[TestFixture()]
	public class ClientUnitTests
	{
		private Client m_client1;
						
		// Constructor WIN
		public ClientUnitTests ()
		{	
			ConfigReader.ReadFile ("/home/nightstrike/programming/mono/radical/client/client.config");
			m_client1 = new Client();
			m_client1.InitClient ();
		}
		
		[Test()]
		public void ConnectDisconnectTest ()
		{
			Assert.IsTrue(m_client1.Connect());
			
			// Server shouldn't crash if we
			// send two connect requests, although
			// as per the spec, this should never
			// happen because of orderly leaves.
			//Assert.IsTrue(m_client2.Connect());
			//Assert.IsTrue(m_client2.Connect());
		}
		
		[Test()]
		public void SequenceNumberTest ()
		{
			// TODO: send a gazillion of these
			// requests simultaneously, a value shouldn't
			// be repeated.
			int i = m_client1.GetSequenceNumber ();
			Assert.AreEqual (i + 1, m_client1.GetSequenceNumber ());
			Assert.AreEqual (i + 2, m_client1.GetSequenceNumber ());
			
			// Now the next node tries
		//	Assert.AreEqual (i + 3, m_client2.GetSequenceNumber ());
		//	Assert.AreEqual (i + 4, m_client2.GetSequenceNumber ());
		}
		
		
		[Test()]
		public void ReservationTest ()
		{
			// This test should book slot 1
			{
			List<string> userlist = new List<string> ();
			List<int> slotlist = new List<int> ();
			string description = "test description";
			
			userlist.Add ("testclient2");
			userlist.Add ("testclient3");
			//userlist.Add ("user2");
			//userlist.Add ("user3");
			slotlist.Add (1);
			slotlist.Add (2);
			slotlist.Add (3);
			m_client1.Reserve (description, userlist, slotlist);
			}
			
			
			// This test should book slot 2 as opposed to slot 1
			{
			List<string> userlist = new List<string> ();
			List<int> slotlist = new List<int> ();
			string description = "test description111";
			
			userlist.Add ("testclient2");
			userlist.Add ("testclient3");
			//userlist.Add ("user5");
			//userlist.Add ("user6");
			slotlist.Add (1);
			slotlist.Add (2);
			slotlist.Add (8);
			m_client1.Reserve (description, userlist, slotlist);
			}
			
			
			// This test should book slot 8 as opposed to slot 1 or 2
			{
			List<string> userlist = new List<string> ();
			List<int> slotlist = new List<int> ();
			string description = "test description111";
			
			userlist.Add ("testclient2");
			userlist.Add ("testclient3");
			//userlist.Add ("user5");
			//userlist.Add ("user6");
			slotlist.Add (1);
			slotlist.Add (2);
			slotlist.Add (8);
			m_client1.Reserve (description, userlist, slotlist);
			}
			
			// Keep this high enough such that the coordinator is
			// still running while the reservation protocol is in
			// progress. Else, the unit tests go upto completion,
			// terminates the coordinator, and the cohorts respond
			// to a ghost.
			System.Threading.Thread.Sleep (5000);
		}
		
		
	}
}