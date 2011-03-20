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
			// Communication Layer
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer();
			m_perfectPointToPointSend = new PerfectPointToPointSend();
			
			m_perfectPointToPointSend.Start(m_sendReceiveMiddleLayer, 9090);
			m_sendReceiveMiddleLayer.SetPointToPointInterface(m_perfectPointToPointSend);
			
			Message m = new Message ();
			m.SetMessageType ("haxxor");
			m.SetDestination ("monkey");
			
			m_sendReceiveMiddleLayer.Send (m);
			
			/*
			//m_groupMulticast = new GroupMulticast ();
			m_calendarService = new CalendarServiceClient ();
			m_lookupService = new LookupServiceClient ();
			m_sequenceNumberService = new SequenceNumberServiceClient (); 
			m_connectionServiceClient = new ConnectionServiceClient ();
			*/
		}
		
	}
}

