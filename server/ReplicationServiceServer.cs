using System;
using System.Collections.Generic;
using System.Threading;
using comm;
using common; 

namespace server
{
	public class ReplicationServiceServer : PadicalObject, IServiceServer
	{
		ManualResetEvent m_oSignalEvent = new ManualResetEvent (false);
		
		// members
		private Server m_server;
		private List<string> m_replicationList;
		
		// buffers
		private List<string> m_pendingReplicationAcks;
		
		// properties
		public bool IsMaster {
			get 
			{
				return (CurrentMaster.Equals (m_server.UserName));
			}
		}
		
		public string CurrentMaster {
			get;
			set;
		}

		public ReplicationServiceServer ()
		{			
			m_pendingReplicationAcks = new List<string> ();
		}
		
		public void SetServer (Server server) 
		{
			if (server != null) 
			{
				m_server = server;
			}
			
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback("replicate",
			                                                          new ReceiveCallbackType(Receive));			
		}
		
		public void Start () 
		{
			m_replicationList = new List<string> ();
			m_replicationList.Add ("server2");
			m_replicationList.Add ("server3");
			
			// determine who is master
			CurrentMaster = "server1";
			
			if (!IsMaster) 
			{
				// TODO: make sure to test that master is reachable, otherwise rotate?
				DebugInfo ("I am not master");
			}
			
			DebugInfo ("[{0}] Replication Service activated. Master is: {1}", m_server.UserName, CurrentMaster);
		}
		
		public void ReplicateUserConnect (string username, string uri) 
		{
			DebugInfo ("Sending UserConnect replication request");
			Message m = PrepareReplicationMessage ();
			m.PushString (uri);
			m.PushString (username);
			m.PushString ("user_connect");
			DistributeReplicationMessage (m);
		}
				
		public void ReplicateUserDisconnect (string username) 
		{
			DebugInfo ("Sending UserDisconnect replication request");
			Message m = PrepareReplicationMessage ();
			m.PushString (username);
			m.PushString ("user_disconnect");
			DistributeReplicationMessage (m);
		}
		
		public void ReplicateSequenceNumber (long number) 
		{															
			DebugInfo ("Sending sequence number ({0}) replication request", number);
			
			Message m = PrepareReplicationMessage ();
			m.PushString (number.ToString ());
			m.PushString ("sequencenumber"); // subtyped message				
			
			DistributeReplicationMessage (m);
		}
		

		public void Receive (ReceiveMessageEventArgs e) 
		{							
			Message m = e.m_message;
			
			if (m.GetMessageType ().Equals ("replicate")) 
			{
				string subtype = m.PopString ();				
				
#region sequencenumber
				if (subtype.Equals ("sequencenumber")) 
				{
					DebugLogic ("Got sequence number replication request from {0}", m.GetSourceUserName ());
					HandleSequenceNumberReplication (m.PopString (), m.GetSourceUserName ());					
				}
				else if (subtype.Equals ("sequencenumber_ack")) 
				{							
					DebugLogic ("ACK replication request: SequenceNumber");
					m_pendingReplicationAcks.Remove (m.GetSourceUserName ());					
					m_oSignalEvent.Set ();					
				}
#endregion
#region user_connect
				else if (subtype.Equals ("user_connect"))
				{
					DebugLogic ("Got user_connect replication request from {0}", m.GetSourceUserName ());
					string username = m.PopString ();
					string uri = m.PopString ();
					HandleUserConnectReplication (username, uri, m.GetSourceUserName ());
				}
				else if (subtype.Equals ("user_connect_ack"))
				{
					DebugLogic ("ACK replication request: UserConnect");
					m_pendingReplicationAcks.Remove (m.GetSourceUserName ());
					m_oSignalEvent.Set ();
				}
#endregion
#region user_disconnect
				else if (subtype.Equals ("user_disconnect"))
				{
					DebugLogic ("Got user_disconnect replication request from {0}", m.GetSourceUserName ());
					string username = m.PopString ();
					HandleUserDisconnectReplication (username, m.GetSourceUserName ());
				}
				else if (subtype.Equals ("user_connect_ack"))
				{
					DebugLogic ("ACK replication request: UserDisconnect");
					m_pendingReplicationAcks.Remove (m.GetSourceUserName ());
					m_oSignalEvent.Set ();
				}
#endregion
			}
		
		}
		
		private Message PrepareReplicationMessage () 
		{
			Message m = new Message ();
			m.SetMessageType ("replicate");			
			m.SetSourceUserName (m_server.UserName);
			return m;
		}
		
		private void DistributeReplicationMessage (Message m) 
		{
			foreach (string replicationServer in m_replicationList)
			{
				m.SetDestinationUsers (replicationServer);
				
				m_pendingReplicationAcks.Add (replicationServer); // possible deadlock here
																  // need unique tuple
				m_server.m_sendReceiveMiddleLayer.Send (m);
			}
			Block ();
		}

		private void HandleUserConnectReplication (string username, string uri, string replyTo)
		{
			m_server.m_userTableService.UserConnect (username, uri);
			Message ack = new Message ();
			ack.SetMessageType ("replicate");
			ack.SetDestinationUsers (replyTo);
			ack.SetSourceUserName (m_server.UserName);
			ack.PushString (username);
			ack.PushString ("user_connect_ack");	
			m_server.m_sendReceiveMiddleLayer.Send (ack);
		}
		
		private void HandleUserDisconnectReplication (string username, string replyTo)
		{
			m_server.m_userTableService.UserDisconnect (username);
			Message ack = new Message ();
			ack.SetMessageType ("replicate");
			ack.SetDestinationUsers (replyTo);
			ack.SetSourceUserName (m_server.UserName);
			ack.PushString (username);
			ack.PushString ("user_disconnect_ack");	
			m_server.m_sendReceiveMiddleLayer.Send (ack);
		}
		
		private void HandleSequenceNumberReplication (string number, string replyTo) 
		{			
			m_server.m_sequenceNumberService.SetSequenceNumber (Int32.Parse (number));				
			Message ack = new Message ();
			ack.SetMessageType ("replicate");
			ack.SetDestinationUsers (replyTo);
			ack.SetSourceUserName (m_server.UserName);
			ack.PushString (number);
			ack.PushString ("sequencenumber_ack");	
			m_server.m_sendReceiveMiddleLayer.Send (ack);			
		}
		

		private void Block ()
		{			
			//This thread will block here until the reset event is sent.
			m_oSignalEvent.WaitOne();
			m_oSignalEvent.Reset ();
		}
	}
}
