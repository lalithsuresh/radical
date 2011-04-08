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
			
			public CalendarState m_calendarState {
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
			PRECOMMIT,
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
		
			m_client.m_sendReceiveMiddleLayer.Send (reservationRequest);
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			lock (this){
			/* Message format for any calendar message (stack part) starts as follows:
			 * 
			 * - SubType TODO: Later register more types up there.
			 * - ReservationSequenceNumber
			 * - Message Sub Type
			 * 
			 * This remains common for all kinds of messages.
			 */
			
			// Unpack message
			// FIXME: Put me in a method
			Message m = eventargs.m_message;
			
			string src = m.GetSourceUserName (); // source user name
			List<string> userlist = m.GetDestinationUsers (); // list of users
			
			string messageSubType = m.PopString (); // message sub type
			int reservationSequenceNumber = Int32.Parse (m.PopString ()); // seq no
		
			// TODO: Implement most of protocol here
			/* If I get a reservation request for a slot
			 * that is either FREE or ACKNOWLEDGED, then
			 * respond with an ACK, else if it is either
			 * BOOKED, or ASSIGNED, then respond with a NACK
			 */
			DebugLogic ("Message of subtype {0} received", messageSubType);
			
			if (messageSubType.Equals ("reservationrequest"))
			{
				int numSlots = Int32.Parse (m.PopString ()); // number of slots
				List<int> slotlist = new List<int> (); // unpack all the slots
				for (int i = 0; i < numSlots; i++)
				{
					slotlist.Add (Int32.Parse (m.PopString ()));
				}
			
				string description = m.PopString (); // description
				Console.WriteLine ("Received Message:" +
						   "[{0}, {1}, {2}, {3}, {4}, {5}, {6}]",
			               src, userlist, messageSubType, reservationSequenceNumber, numSlots, slotlist, description);
				
				
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
					}
					
					ack.PushString (availableslots.Count.ToString ());
					ack.PushString (reservationSequenceNumber.ToString ());
					ack.PushString ("reservationack");
					
					DebugLogic ("Sending an ack to: {0}", src);

					m_client.m_sendReceiveMiddleLayer.Send (ack);
				}
				else
				{
					Message nack = new Message ();
					// No free slots, so respond with a NACK
					nack.SetSourceUserName (m_client.UserName);
					nack.SetDestinationUsers (src);
					nack.SetMessageType ("calendar");
					
					nack.PushString ("reservationnack");
					
					//m_client.m_sendReceiveMiddleLayer.Send (nack);
				}
			}
			
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
	
				if (messageSubType.Equals ("reservationack") || messageSubType.Equals ("reservationnack"))
				{
					DebugLogic ("Received {0} from {1} for reservation ID: {2}",
				            messageSubType, src, reservationSequenceNumber);
				
					// obtain reservation objects
					Reservation res = m_activeReservationSessions[reservationSequenceNumber];
				
					if (messageSubType.Equals ("reservationack"))
					{
						int slotCount = Int32.Parse (m.PopString ());
						while (slotCount > 0)
						{
							int s = Int32.Parse (m.PopString ());
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
							
							// If we get ACKS from all clients for slot 's',
							// and the reservation isn't already tentatively booked for any slot,
							// and the slot in concern isn't already booked for some other reservation...
							if (ackcount == res.m_userList.Count
							    && res.m_reservationState != CalendarServiceClient.ReservationState.TENTATIVELY_BOOKED
							    && m_numberToSlotMap[s].m_calendarState != CalendarServiceClient.CalendarState.BOOKED)
							{
								DebugLogic ("Woopee! I can haz slot! {0}", s);
								
								// Update reservation state.
								res.m_reservationState = CalendarServiceClient.ReservationState.TENTATIVELY_BOOKED;
								res.m_slotNumberList.Clear ();
								
								// m_slotNumberList now holds the only
								// slot under consideration.
								res.m_slotNumberList.Add (s);
								
								// Update slot state.
								m_numberToSlotMap [s].m_calendarState = CalendarServiceClient.CalendarState.BOOKED;
								
								
								// Now send a precommit message to everyone involved.
								// Party time!
								
								Message precommitMsg = new Message ();
								
								precommitMsg.SetSourceUserName (m_client.UserName);
								precommitMsg.SetDestinationUsers (res.m_userList);
								DebugUncond ("SENDING PRECOMMIT TO {0} many ppl", res.m_userList.Count);
								precommitMsg.SetMessageType ("calendar");
								precommitMsg.PushString (s.ToString ());
								precommitMsg.PushString (reservationSequenceNumber.ToString ());
								precommitMsg.PushString ("precommit");
								m_client.m_sendReceiveMiddleLayer.Send (precommitMsg);
									
								break; // safe to not go through the other slot.
							}
							
							slotCount--;
						}
					}
				else if (messageSubType.Equals ("reservationnack"))
					{
						res.m_reservationState = CalendarServiceClient.ReservationState.ABORTED;
					}
				}
				
			
				/* If I get a PRECOMMIT update for a slot,
				 * and it is still in ACKNOWLEDGED state, then
				 * respond with YES!. Else, respond with a NO!
				 * 
				 * 		If I responded with a YES, then#include.

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
					
				if (messageSubType.Equals ("precommit"))
				{
					int slotCount = Int32.Parse (m.PopString ());
					DebugLogic ("Received precommit for slot {0}",slotCount);
				}
				
					
				/* If I am the initiator/coordinator, and I get a YES!
				 * message, then keep collecting them.
				 * 
				 * 		If some cohort times out, then ABORT.
				 * 
				 * If I get a NO message, then ABORT everyone.
				 * 
				 * If I get all YES!'s, then run commit and send DOCOMMIT
				 * messages to all cohorts.
				 */
				
				/* If I am a cohort and get a DOCOMMIT message, then commit.
				 */
			}
		}
	}
}