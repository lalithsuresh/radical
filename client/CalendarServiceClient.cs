using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using comm;
using common;

namespace client
{
	public class CalendarServiceClient: PadicalObject
	{
		// As per description
		public int m_minSlot = 0;
		public int m_maxSlot = 8760;
		private Client m_client;

		/* This keeps track of active "sessions".
		 * It is basically a map between the
		 * reservation ID (provided by the central server)
		 * and the corresponding reservation object.
		 * The reservation object gives us the list of slots
		 * which are part of the reservation. Each slot
		 * give us the list of reservations that are contending
		 * for the slot.
		 */
		private Dictionary <int, Reservation> m_activeReservationSessions;
		
		/* Given a slot number, return the slot object
		 */
		private Dictionary <int, Slot> m_numberToSlotMap;
				
		public CalendarServiceClient ()
		{
			m_activeReservationSessions = new Dictionary<int, Reservation> ();
			m_numberToSlotMap = new Dictionary<int, Slot> ();
		//	m_deferredSendTimer = new Timer (DeferredSend, null, 0, 5000);
		}
		
		public void SetClient (Client client)
		{
			m_client = client;
			m_client.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("calendar",
			                                                           new ReceiveCallbackType (Receive));
		}
		
		/*
		 * This abstraction represents a slot that
		 * is one out of 8761 entries in a calendar
		 */
		public class Slot
		{
			// This list will be singular when committed
			public List<Reservation> m_reservationsForThisSlot {
				get;
				set;
			}
			
			public SortedList<int, Reservation> m_preCommitList {
				get;
				set;
			}
			
			public CalendarState m_calendarState {
				get;
				set;
			}
						
			public int m_lockedReservation {
				get;
				set;
			}
			
			public int m_slotNumber {
				get;
				set;
			}
			
			public Slot ()
			{
			}
		}
		
		/*
		 * From the specification
		 */
		public enum CalendarState
		{
			FREE,
			ACKNOWLEDGED,
			BOOKED,
			ASSIGNED,
		}
		
		/*
		 * This abstraction represents a reservation
		 * entity. Use this as a container for holding
		 * the state and properties of a reservation
		 */
		public class Reservation
		{	
			public ReservationState m_reservationState {
				get;
				set;
			}
			
			public int m_sequenceNumber {
				get;
				set;
			}
			
			public Reservation ()
			{
			}
			
			public string m_description {
				get;
				set;
			}
		
			public string m_initiator {
				get;
				set;
			}
			
			public List<int> m_slotNumberList {
				get;
				set;
			}
			
			public Dictionary<int, int> m_acksForSlot {
				get;
				set;
			}
			
			public List<string> m_userList {
				get;
				set;
			}
		}
		
		/*
		 * From the specification
		 */
		public enum ReservationState
		{
			INITIATED,
			TENTATIVELY_BOOKED,
			COMMITTED,
			ABORTED
		}
		
		
		public List<Slot> GetSlotObjectsForReservation (Reservation res)
		{
			List<Slot> slotlist = new List<Slot> ();
			
			// Iterate through the slot number-to-object map
			// TODO: Probably need sanity checks here
			foreach (int i in res.m_slotNumberList)
			{
				slotlist.Add (m_numberToSlotMap[i]);
			}
			
			return slotlist;
		}
				
		
		public void Reserve (string description, List<string> userlist, List<int> slotlist)
		{
			// 1) Create reservation object
			// TODO: Make sure m_client VALIDATES the inputs?
			// TODO: At this point, we assume all requested slots are free.
			Reservation reservation = new Reservation ();
			reservation.m_description = description;
			reservation.m_slotNumberList = slotlist;
			reservation.m_userList = userlist;
			reservation.m_reservationState = CalendarServiceClient.ReservationState.INITIATED;
			reservation.m_acksForSlot = new Dictionary<int, int> ();
			reservation.m_initiator = m_client.UserName;
			
			// 2) Obtain sequence number
			reservation.m_sequenceNumber = m_client.GetSequenceNumber ();
			
			// 2) Maintain reservation session as per sequence number
			// Add reservation object to int-to-reservation-object map.
			m_activeReservationSessions.Add (reservation.m_sequenceNumber, reservation);
			
			DebugLogic ("Created reservation object for" +
				"[Desc: {0}, Users: {1}, Slots: {2} Seq: {3}]",description, userlist, slotlist, reservation.m_sequenceNumber);
			
			// Update slot objects
			foreach (int i in slotlist)
			{
				// If slot is being encountered for the
				// first time...
				if (!m_numberToSlotMap.ContainsKey (i))
				{
					DebugLogic ("Creating new slot instance for slot-number: {0}", i);
			
					// Then create new slot.
					// TODO: Probably need internal methods for this
					Slot slot = new Slot ();
					slot.m_calendarState = CalendarState.FREE;
					slot.m_slotNumber = i;
					slot.m_reservationsForThisSlot = new List<Reservation> ();
					slot.m_preCommitList = new SortedList<int, Reservation> ();
					slot.m_lockedReservation = -1;
					
					// Add to int-to-slot-object map
					m_numberToSlotMap.Add (i, slot);
				}
				
				// Update the slot's reservation list
				m_numberToSlotMap[i].m_reservationsForThisSlot.Add (reservation);
			}			
			
			// 3) Disseminate reservation request
			DebugLogic ("Dissemination reservation request " +
				"[Desc: {0}, Users: {1}, Slots: {2}]",description, userlist, slotlist);
			
			Message reservationRequest = new Message ();
			
			
			/* Message format for reservations (stack part) is as follows: 
			 * 
			 * - SubType TODO: Later register more types up there.
			 * - ReservationSequenceNumber
			 * - User NumberOfSlots
			 * - NumberOfSlots
			 * - Slot 1
			 * - Slot 2
			 * ...
			 * - Slot NumberOfUsers
			 * - Description
			 */
			reservationRequest.SetDestinationUsers (userlist);
			reservationRequest.SetMessageType ("calendar");
			reservationRequest.SetSourceUserName (m_client.UserName);
			
			// Data part. Things will be pushed in the reverse order
			// of the format
			reservationRequest.PushString (description);
			
			// Since we're pushing on to a stack and would like
			// to retreive it in the same order at the other end.
			slotlist.Reverse ();
			foreach (int i in slotlist)
			{
				reservationRequest.PushString (i.ToString ());
			}
			
			reservationRequest.PushString (reservation.m_slotNumberList.Count.ToString ());
			reservationRequest.PushString (reservation.m_sequenceNumber.ToString ());
			reservationRequest.PushString ("reservationrequest");	
		
			SendMessage (reservationRequest);
		}
				
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			lock (this)
			{
				/* Message format for any calendar message (stack part) starts as follows:
				 * 
				 * - SubType TODO: Later register more types up there.
				 * - ReservationSequenceNumber
				 * - Message Sub Type
				 * 
				 * This remains common for all kinds of messages.
				 */
				
				// Unpack message
				Message m = eventargs.m_message;
				string src = m.GetSourceUserName (); // source user name
				List<string> userlist = m.GetDestinationUsers (); // list of users
				string messageSubType = m.PopString (); // message sub type
				int reservationSequenceNumber = Int32.Parse (m.PopString ()); // seq no
			
							
				DebugLogic ("Message of subtype {0} received", messageSubType);
				
				if (messageSubType.Equals ("reservationrequest"))
				{	
					ReceiveReservationRequest (m, src, userlist, reservationSequenceNumber, messageSubType);
				}
							
				else if (messageSubType.Equals ("reservationack") || messageSubType.Equals ("reservationnack"))
				{
					ReceiveReservationAckNack (m, src, userlist, reservationSequenceNumber, messageSubType);		
				}
			
				else if (messageSubType.Equals ("precommit"))
				{
					ReceiveReservationPreCommit (m, src, userlist, reservationSequenceNumber, messageSubType);		
				}
				else if (messageSubType.Equals ("yes"))
				{
					ReceiveReservationYes (m, src, userlist, reservationSequenceNumber, messageSubType);
				}
				else if (messageSubType.Equals ("docommit"))
				{
						ReceiveReservationDoCommit(m, src, userlist, reservationSequenceNumber, messageSubType);
				}
				else if (messageSubType.Equals ("abort"))
				{
						ReceiveReservationAbort(m, src, userlist, reservationSequenceNumber, messageSubType);
				}
				
			} // Lock end
		}
		
		private void ReceiveReservationRequest (Message m,
		                                        string src,
		                                        List<string> userlist,
		                                        int reservationSequenceNumber,
		                                        string messageSubType)
		{
			/* If I get a reservation request for a slot
			 * that is either FREE or ACKNOWLEDGED, then
			 * respond with an ACK, else if it is either
			 * BOOKED, or ASSIGNED, then respond with a NACK
			 */	
			int numSlots = Int32.Parse (m.PopString ()); // number of slots
			List<int> slotlist = new List<int> (); // unpack all the slots
			for (int i = 0; i < numSlots; i++)
			{
				slotlist.Add (Int32.Parse (m.PopString ()));
			}
			
			string description = m.PopString (); // description
			DebugLogic ("Received Message:" +
							   "[{0}, {1}, {2}, {3}, {4}, {5}, {6}]",
				               src, userlist.ToString (), messageSubType, reservationSequenceNumber, numSlots, slotlist.ToString (), description);
					
					
			/* Message unpacked, now create a reservation object
			 */
					
			if (m_activeReservationSessions.ContainsKey (reservationSequenceNumber))
			{
				DebugFatal ("Duplicate reservation request received {0}", reservationSequenceNumber);
			}
					
			/* Create reservation object
			 */
			Reservation reservation = new Reservation ();
			reservation.m_description = description;
			reservation.m_sequenceNumber = reservationSequenceNumber;
			reservation.m_userList = userlist;
			reservation.m_slotNumberList = slotlist;
			reservation.m_initiator = src;
									
			/* 
			 * check if slots are free
			 * and proceed to respond with an ACK or NACK.
			 */
			List<int> availableslots = new List<int> ();
			foreach (int i in slotlist)
			{
				// If slot is being encountered for the
				// first time...
				if (!m_numberToSlotMap.ContainsKey (i))
				{
					DebugLogic ("Creating new slot instance for slot-number: {0}", i);
					// Then create new slot.
					// TODO: Probably need internal methods for this
					Slot tempslot = new Slot ();
					tempslot.m_calendarState = CalendarState.FREE;
					tempslot.m_slotNumber = i;
					tempslot.m_reservationsForThisSlot = new List<Reservation> ();
					tempslot.m_preCommitList = new SortedList<int, Reservation> ();
					tempslot.m_lockedReservation = -1;
							
					// Add to int-to-slot-object map
					m_numberToSlotMap.Add (i, tempslot);
				}
						
				// Get the concerned slot
				Slot slot = m_numberToSlotMap[i];
				
				if (slot.m_calendarState == CalendarServiceClient.CalendarState.FREE ||
				    slot.m_calendarState == CalendarServiceClient.CalendarState.ACKNOWLEDGED)
				{
					// Update the slot's reservation list
					slot.m_reservationsForThisSlot.Add (reservation);
							
					// Append to list of slots.
					availableslots.Add (i);
							
					// Update slot state
					slot.m_calendarState = CalendarServiceClient.CalendarState.ACKNOWLEDGED;
					DebugLogic ("Slot {0} is now in ACKNOWLEDGED state", i);
				}
			}
					
			// Respond with ACK/NACK
			if (availableslots.Count > 0)
			{
				// We do have at least one free slot, so respond with an ACK
				Message ack = new Message ();
					
				ack.SetSourceUserName (m_client.UserName);
				ack.SetDestinationUsers (src);
				ack.SetMessageType ("calendar");
						
				/*
				 * ACK Message format, data part
				 * 
				 * - Number of Slots
				 * - Slot 1
				 * - Slot 2
				 * ...
				 * - Slot N
				 */
				availableslots.Reverse ();
						
				foreach (int i in availableslots)
				{
					ack.PushString (i.ToString ());
					DebugInfo ("Slot {0} is available", i);
				}
						
				ack.PushString (availableslots.Count.ToString ());
				ack.PushString (reservationSequenceNumber.ToString ());
				ack.PushString ("reservationack");
						
				DebugLogic ("Sending an ack to: {0}", src);
	
				SendMessage (ack);
				m_activeReservationSessions.Add (reservationSequenceNumber, reservation);
			}
			else
			{
				Message nack = new Message ();
				// No free slots, so respond with a NACK
				nack.SetSourceUserName (m_client.UserName);
				nack.SetDestinationUsers (src);
				nack.SetMessageType ("calendar");
					
				nack.PushString ("reservationnack");
				
				DebugLogic ("Sending a nack to: {0}", src);

				SendMessage (nack);
			}
		}
		
		private void ReceiveReservationAckNack (Message m,
		                                        string src,
		                                        List<string> userlist,
		                                        int reservationSequenceNumber,
		                                        string messageSubType)
		{
			/* If I am the initiator of the reservation,
			 * then keep collecting ACKS/NACKS from all participants
			 * 
			 *      If ACK rcvd, then update received ACK counter.
			 * 		
			 * 			If all ACKS rcvd, move reservation state to
			 *	    	TENTATIVELY_BOOKED for those slots.
			 * 			Then inform all nodes about decision with a
			 * 			PRECOMMIT message and move to PRECOMMIT state.
			 * 
			 * 		If at least one NACK, move reservation state
			 * 		to ABORTED. TODO: Should we notify others?
			 */

			DebugLogic ("Received {0} from {1} for reservation ID: {2}",
				            messageSubType, src, reservationSequenceNumber);
				
			// obtain reservation objects
			Reservation res = m_activeReservationSessions[reservationSequenceNumber];
			int slotCount = Int32.Parse (m.PopString ());
		
			if (messageSubType.Equals ("reservationack")
			    && res.m_reservationState != CalendarServiceClient.ReservationState.ABORTED)
			{
				
				List<int> validSlots = new List<int> ();
				int ackcount = 0;
				
				while (slotCount > 0)
				{
					int s = Int32.Parse (m.PopString ());
						
					if (m_numberToSlotMap[s].m_calendarState != CalendarServiceClient.CalendarState.BOOKED
					    && m_numberToSlotMap[s].m_lockedReservation == -1)
					{
						validSlots.Add (s);
						
						DebugInfo ("Adding {0} to validslot list", s);
						if (res.m_acksForSlot.ContainsKey (s))
						{
							res.m_acksForSlot[s]++;
							ackcount = res.m_acksForSlot[s];
						}
						else
						{
							ackcount = 1;
							res.m_acksForSlot[s] = ackcount;
						}
					}
										
					slotCount--;
				}
				
				// If there are slots which were not part of
				// the ack, then we do some cleanup.				
				if (validSlots.Count != res.m_slotNumberList.Count)
				{
					foreach (int i in res.m_slotNumberList)
					{
						bool isPresent = false;
						foreach (int j in validSlots)
						{
							if (i == j)
							{
								isPresent = true;
							}
						}
						
						if (!isPresent)
						{
							m_numberToSlotMap[i].m_reservationsForThisSlot.Remove (res);
						}
					}
					
				}

				res.m_slotNumberList = validSlots;

				// If enough ACKS, then proceed with precommit for
				// each valid slot, one by one.
				
				if (res.m_userList.Count == ackcount)
				{
					res.m_reservationState = CalendarServiceClient.ReservationState.TENTATIVELY_BOOKED;
					m_numberToSlotMap[res.m_slotNumberList[0]].m_calendarState = CalendarServiceClient.CalendarState.BOOKED;
					m_numberToSlotMap[res.m_slotNumberList[0]].m_lockedReservation = reservationSequenceNumber;
					SendPreCommitMessage (res, res.m_slotNumberList[0]);
					
					res.m_acksForSlot.Clear ();
				}
			}
			else if (messageSubType.Equals ("reservationnack"))
			{
				res.m_reservationState = CalendarServiceClient.ReservationState.ABORTED;
				
				DebugLogic ("Send NACK to {0}", src);
				foreach (int i in res.m_slotNumberList)
				{
					m_numberToSlotMap[i].m_reservationsForThisSlot.Remove (res);
				}
			}
		}
		
		private void SendPreCommitMessage (Reservation res, int s)
		{
			
			
			// Now send a precommit message to everyone involved.
			// Party time!
			
			Message precommitMsg = new Message ();
			
			precommitMsg.SetSourceUserName (m_client.UserName);
			precommitMsg.SetDestinationUsers (res.m_userList);
			DebugLogic ("SENDING PRECOMMIT TO {0} many ppl for slot {1}", res.m_userList.Count, s);
			precommitMsg.SetMessageType ("calendar");
			precommitMsg.PushString (s.ToString ());
			precommitMsg.PushString (res.m_sequenceNumber.ToString ());
			precommitMsg.PushString ("precommit");
			SendMessage (precommitMsg);
			
			res.m_acksForSlot.Clear ();
		}
				
		private void ReceiveReservationPreCommit (Message m,
		                                          string src,
		                                          List<string> userlist,
		                                          int reservationSequenceNumber,
		                                          string messageSubType)
		{
						
			/* If I get a PRECOMMIT update for a slot,
			 * and it is still in ACKNOWLEDGED state, then
			 * respond with YES!. Else, respond with a NO!
			 * 
			 * 		If I responded with a YES, then
	
			 * 		move into PRECOMMIT state and begin
			 * 		verification and commit timers.
			 * 
			 * 			When verification timer fires, verify
			 * 			your neighbours. TODO: Might need ping
			 * 			utility
			 * 
			 * 				If verification fails, ABORT, else do nothing.
			 * 
			 * 			When commit timer fires, commit.
			 * 
			 * 		If I responded with a NO, then move
			 * 		into ABORT.
			 * 
			 * 		If between any of the above, the coordinator
			 * 		sends an ABORT message, then ABORT. DUH!
			 */
				
			int s = Int32.Parse (m.PopString ());
			DebugLogic ("Received precommit for slot {0}, reservation number {1}",s,reservationSequenceNumber);
			
			Slot slot = m_numberToSlotMap[s];
			Reservation res = m_activeReservationSessions[reservationSequenceNumber];
			
			if (slot.m_calendarState == CalendarServiceClient.CalendarState.ACKNOWLEDGED
			    || slot.m_calendarState == CalendarServiceClient.CalendarState.BOOKED)
			{				

				if (slot.m_preCommitList.Count == 0 && slot.m_lockedReservation == -1)
				{
					res.m_reservationState = CalendarServiceClient.ReservationState.TENTATIVELY_BOOKED;
					slot.m_calendarState = CalendarServiceClient.CalendarState.BOOKED;
					// No one has a lock, so this reservation gets it.
					Message yes = new Message ();
					
					yes.SetDestinationUsers (src);
					yes.SetMessageType ("calendar");
					yes.SetSourceUserName (m_client.UserName);
					yes.PushString (s.ToString ());
					yes.PushString (reservationSequenceNumber.ToString ());
					yes.PushString ("yes");
					
					slot.m_lockedReservation = reservationSequenceNumber;
					
					SendMessage (yes);
				}
				else if (slot.m_lockedReservation != -1 && slot.m_lockedReservation < s)
				{
					// Some reservation older is already locked, so wait.
					slot.m_preCommitList.Add (reservationSequenceNumber, res);
				}
				else
				{
					// A newer reservation has been locked. For now,
					// we try and ABORT the newer one in favour of the older one.
					// But the optimisation here would be for coordinators to
					// initiate an is-there-a-deadlock check, which the cohort
					// would ask the coordinator to initiate.
					Message abortmsg = new Message ();
				
					abortmsg.SetDestinationUsers (m_activeReservationSessions[reservationSequenceNumber].m_initiator);
					abortmsg.SetMessageType ("calendar");
					abortmsg.SetSourceUserName (m_client.UserName);
					abortmsg.PushString (s.ToString ()); // we need to mention the slot that is being aborted
					abortmsg.PushString (slot.m_lockedReservation.ToString ());
					abortmsg.PushString ("abort");
				
					SendMessage (abortmsg);
					ReservationAbortCohort (slot.m_lockedReservation, s);
				}
			}
		}
		
		
		private void ReceiveReservationYes	 (Message m,
		                                      string src,
		                                      List<string> userlist,
		                                      int reservationSequenceNumber,
		                                      string messageSubType)
		{
			int s = Int32.Parse (m.PopString ());
			DebugLogic ("Received YES for slot {0}, reservation number {1}",s,reservationSequenceNumber);
			
			Slot slot = m_numberToSlotMap[s];
			Reservation res = m_activeReservationSessions[reservationSequenceNumber];
			
			int ackcount;
			if (res.m_acksForSlot.ContainsKey (s))
			{
				res.m_acksForSlot[s]++;
				ackcount = res.m_acksForSlot[s];
			}
			else
			{
				ackcount = 1;
				res.m_acksForSlot[s] = ackcount;
			}
			
			if (ackcount == res.m_userList.Count
			    // the below two conditions are sanity checks.
			    && res.m_reservationState != CalendarServiceClient.ReservationState.COMMITTED
			    && m_numberToSlotMap[s].m_calendarState != CalendarServiceClient.CalendarState.ASSIGNED)
			{
				DebugLogic ("Woopee! I can haz slot! {0}", s);
				
				//send DoCommit				
				Message docommitMsg = new Message ();
				docommitMsg.SetSourceUserName (m_client.UserName);
				docommitMsg.SetDestinationUsers (res.m_userList);
				docommitMsg.SetMessageType ("calendar");
				docommitMsg.PushString (s.ToString ());
				docommitMsg.PushString (reservationSequenceNumber.ToString ());
				docommitMsg.PushString ("docommit");
				SendMessage (docommitMsg);
				
				// If you have to send this, then you can commit.
				// TODO: Handle 2/1, 1/2 deadlock.
				slot.m_calendarState = CalendarServiceClient.CalendarState.ASSIGNED;
				res.m_reservationState = CalendarServiceClient.ReservationState.COMMITTED;
			}
			
		}
		
		private void ReceiveReservationDoCommit	 (Message m,
		                                          string src,
		                                          List<string> userlist,
		                                          int reservationSequenceNumber,
		                                          string messageSubType)
		{
			int s = Int32.Parse (m.PopString ());
			DebugLogic ("Received docommit for slot {0}, reservation number {1}",s,reservationSequenceNumber);
			
			Slot slot = m_numberToSlotMap[s];
			Reservation res = m_activeReservationSessions[reservationSequenceNumber];
			
			slot.m_calendarState = CalendarServiceClient.CalendarState.ASSIGNED;			
			res.m_reservationState = CalendarServiceClient.ReservationState.COMMITTED;
			
			DebugLogic ("Reservation ID: {0}, Slot: {1} has COMMITTED", reservationSequenceNumber, s);
			
			if (m_client.m_isPuppetControlled)
			{
				m_client.m_puppetService.SendInfoMsgToPuppetMaster ("Reservation ID: {0}, Slot: {1} has COMMITTED", 
				                                                    reservationSequenceNumber, s);
			}
			
			// Perform cleanup of all reservation objects
			foreach (int i in res.m_slotNumberList)
			{
				if (i != s)
				{
					m_numberToSlotMap[i].m_reservationsForThisSlot.Remove (res);
				}
			}
			
			foreach (int i in slot.m_preCommitList.Keys)
			{
					
				Message abortmsg = new Message ();
				
				abortmsg.SetDestinationUsers (m_activeReservationSessions[reservationSequenceNumber].m_initiator);
				abortmsg.SetMessageType ("calendar");
				abortmsg.SetSourceUserName (m_client.UserName);
				abortmsg.PushString (s.ToString ()); // we need to mention the slot that is being aborted
				abortmsg.PushString (reservationSequenceNumber.ToString ());
				abortmsg.PushString ("abort");
				
				SendMessage (abortmsg);
				ReservationAbortCohort (i, s);
			}
			
		}
		
		
		private void ReservationAbortCohort (int reservationSequenceNumber, int s)
		{
			Slot slot = m_numberToSlotMap[s];
						
			Reservation oldReservation = m_activeReservationSessions[reservationSequenceNumber];
			oldReservation.m_slotNumberList.Remove (s);
						
			// The earlier reservation is now removed from this slot
			slot.m_reservationsForThisSlot.Remove (oldReservation);
			
			// If this was the last slot in contention for
			// the reservation...
			if (oldReservation.m_slotNumberList.Count == 0)
			{
				// ... then the reservation should be in ABORTED state. Sigh.				
				oldReservation.m_reservationState = CalendarServiceClient.ReservationState.ABORTED;
			}
			
			// This slot is currently locked, only an abort can kill it.
			if (slot.m_lockedReservation == reservationSequenceNumber)
			{				
				// Promote next reservation in precommit queue
				if (slot.m_preCommitList.Count > 0)
				{
					slot.m_lockedReservation = slot.m_preCommitList.Keys[0]; // Lock top queue elements
					slot.m_preCommitList.RemoveAt (0); // remove top element from queue.
				
					Reservation newres = m_activeReservationSessions[slot.m_lockedReservation];
					
					// Send a yes message for the succeeding reservation
					Message yes = new Message ();
					yes.SetDestinationUsers (m_activeReservationSessions[slot.m_lockedReservation].m_initiator);
					yes.SetMessageType ("calendar");
					yes.SetSourceUserName (m_client.UserName);
					yes.PushString (slot.m_slotNumber.ToString ());
					yes.PushString (newres.m_sequenceNumber.ToString ());
					yes.PushString ("yes");
							
					SendMessage (yes);
				}
				else
				{
					// We don't have any more reservations for this slot, so
					// let's keep it in the ACKNOWLEDGED state.
					slot.m_calendarState = CalendarServiceClient.CalendarState.ACKNOWLEDGED;
					
					// Release locks
					slot.m_lockedReservation = -1;
				}
			}
			else
			{
				// This slot is in the precommit list				
				slot.m_preCommitList.Remove (s);
			}
		}
		
		private void ReceiveReservationAbort (Message m,
		                                      string src,
		                                      List<string> userlist,
		                                      int reservationSequenceNumber,
		                                      string messageSubType)
		{
			int s = Int32.Parse (m.PopString ());
			DebugLogic ("Received ABORT for slot {0}, reservation number {1}",s,reservationSequenceNumber);
			
			Slot slot = m_numberToSlotMap[s];
			Reservation res = m_activeReservationSessions[reservationSequenceNumber];
			
			// if i am the initiator, and the reservation has not already committed,
			// or has not already been aborted by someone else,
			// then send every1 aborts.
			if (res.m_initiator.Equals (m_client.UserName)
			    && res.m_reservationState != CalendarServiceClient.ReservationState.COMMITTED
			    && res.m_reservationState != CalendarServiceClient.ReservationState.ABORTED)
			{
				//Perform my own cleanup,
				res.m_slotNumberList.Remove (s);
						
				// The earlier reservation is now removed from this slot
				slot.m_reservationsForThisSlot.Remove (res);				
			
				// If this was the last slot in contention for
				// the reservation...
				if (res.m_slotNumberList.Count == 0)
				{
					// ... then the reservation should be in ABORTED state. Sigh.				
					res.m_reservationState = CalendarServiceClient.ReservationState.ABORTED;
				}
				else
				{
					// Pick next tentatively booked slot and send out precommit messages
					SendPreCommitMessage (res, res.m_slotNumberList[0]);
				}
				
				// TODO:send everyone abort messages.
			}
			else
			{
				// Abort the reservation entry for this particular slot.
				ReservationAbortCohort (reservationSequenceNumber, s);	
			}
		}
		
		private void SendMessage (Message m)
		{
			m_client.m_sendReceiveMiddleLayer.Send (m);
		}
		
		public string ReadCalendar ()
		{
			string ret = "";
			foreach (int i in m_numberToSlotMap.Keys)
			{
				ret = ret + i.ToString () + ":" + m_numberToSlotMap[i].m_calendarState.ToString () + ", ";
			}
			
			return ret;
		}
	}
}