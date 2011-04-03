using System;
using System.Collections.Generic;
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
			
			// 2) Obtain sequence number
			reservation.m_sequenceNumber = m_client.GetSequenceNumber ();
			
			// 2) Maintain reservation session as per sequence number
			// Add reservation object to int-to-reservation-object map.
			m_activeReservationSessions.Add (reservation.m_sequenceNumber, reservation);
			
			DebugLogic ("Created reservation object for" +
				"[Desc: {0}, Users: {1}, Slots: {2}]",description, userlist, slotlist);
			
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
			
		}
		
		public void Receive (ReceiveMessageEventArgs eventargs)
		{
			// TODO: Implement most of protocol here
			
			
			/* If I get a reservation request for a slot
			 * that is either FREE or ACKNOWLEDGED, then
			 * respond with an ACK, else if it is either
			 * BOOKED, or ASSIGNED, then respond with a NACK
			 */
			
			
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
			 * 		If I respondded with a NO, then move
			 * 		into ABORT.
			 * 
			 * 		If between any of the above, the coordinator
			 * 		sends an ABORT message, then ABORT. DUH!
			 */
			
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