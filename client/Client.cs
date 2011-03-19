using System;
using comm;

namespace client
{
	public class Client
	{
		PerfectPointToPointSend m_perfectPointToPointSend;
		SendReceiveMiddleLayer m_sendReceiveMiddleLayer;
		GroupMulticast m_groupMulticast;
		CalendarServiceClient m_calendarService;
		LookupServiceClient m_lookupService;
		SequenceNumberServiceClient m_sequenceNumberService;
		ConnectionServiceClient m_connectionServiceClient;
		
		public Client ()
		{
		}
		
		public void InitClient ()
		{
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer ();
			m_perfectPointToPointSend = new PerfectPointToPointSend (m_sendReceiveMiddleLayer);
			//m_groupMulticast = new GroupMulticast ();
			m_calendarService = new CalendarServiceClient ();
			m_lookupService = new LookupServiceClient ();
			m_sequenceNumberService = new SequenceNumberServiceClient (); 
			m_connectionServiceClient = new ConnectionServiceClient ();
		}
		
	}
}

