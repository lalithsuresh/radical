
using System;
using NUnit.Framework;

namespace puppet
{


	[TestFixture()]
	public class ConfigurationTest
	{
		PuppetConfigurationReader configreader;
		
		[SetUp()]
		public void Init ()
		{
			configreader = new PuppetConfigurationReader ();
			//configreader.LoadFile ("/home/archie/repos/radical/puppet/e");
		}

		[Test()]
		public void ParseSimpleReservationLine ()
		{
			string line = "reservation {GroupMeeting; user1; 50 }";
			PuppetInstruction instr = configreader.ParseInstruction (line);
			Assert.IsTrue (String.Equals(instr.ApplyToUser, "user1"));
			Assert.IsTrue (Int32.Parse (instr.Slots[0]) == 50);
			//Assert.IsTrue (String.Equals(instr.Users[0], "user1"));
			Assert.IsTrue (instr.Type == PuppetInstructionType.RESERVATION);
		}
		
		[Test()]
		public void ParseMediumReservationLine ()
		{
			string line = "reservation {R1; user1, user2; 50 }";
			PuppetInstruction instr = configreader.ParseInstruction (line);
			Assert.IsTrue (String.Equals(instr.ApplyToUser, "user1"));
			Assert.IsTrue (Int32.Parse (instr.Slots[0]) == 50);
			Assert.IsTrue (String.Equals(instr.Users[0], "user2"));
			Assert.IsTrue (instr.Type == PuppetInstructionType.RESERVATION);
		}

		[Test()]
		public void ParseComplexReservationLine ()
		{
			string line = "reservation {GroupMeeting; user1, user2, user3; 50, 13, 20, 5800 }";
			PuppetInstruction instr = configreader.ParseInstruction (line);
			Assert.IsTrue (String.Equals(instr.ApplyToUser, "user1"));
			Assert.IsTrue (Int32.Parse (instr.Slots[0]) == 50);
			Assert.IsTrue (Int32.Parse (instr.Slots[1]) == 13);
			Assert.IsTrue (Int32.Parse (instr.Slots[2]) == 20);
			Assert.IsTrue (Int32.Parse (instr.Slots[3]) == 5800);
			Assert.IsTrue (String.Equals(instr.Users[0], "user2"));
			Assert.IsTrue (String.Equals(instr.Users[1], "user3"));
			Assert.IsTrue (instr.Type == PuppetInstructionType.RESERVATION);
		}
		
		
		[Test()]
		public void ParseReadCalendarLine () 
		{
			string line = "readCalendar user1 127.0.0.1:23002";
			PuppetInstruction instr = configreader.ParseInstruction (line);
			Assert.IsTrue (String.Equals (instr.ApplyToUser, "user1"));
			Assert.IsTrue (instr.Type == PuppetInstructionType.READ_CALENDAR);
		}
		
		[Test()]
		public void ParseConnectLine () 
		{
			string line = "connect user2 127.0.0.1:2301";
			PuppetInstruction instr = configreader.ParseInstruction (line);
			Assert.IsTrue (String.Equals (instr.ApplyToUser, "user2"));
			Assert.IsTrue (instr.Type == PuppetInstructionType.CONNECT);
		}
	}
}
