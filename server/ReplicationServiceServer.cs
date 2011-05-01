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
		
		// constants
		private const int FAIL_TOLERANCE = 2;
		
		// members
		private Server m_server;
		private List<string> m_replicationList;
		private PingTimer m_pingTimer;
		private Thread m_pingTimerThread;
		private Dictionary<string, bool> m_serverStatus;
		private Dictionary<string, int> m_serverStatusFailCount;
		private Dictionary<string, string> m_serverUriToServerNameMap;
		
		// buffer
		private string m_masterReplyBuffer;
				
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
			m_serverStatus = new Dictionary<string, bool> ();
			m_serverStatusFailCount = new Dictionary<string, int> ();
			m_serverUriToServerNameMap = new Dictionary<string, string> ();
			m_replicationList = new List<string> ();
		}
		
		public void SetServer (Server server) 
		{
			if (server != null) 
			{
				m_server = server;
			}
			
			// register messages to listen too
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("replicate",
			                                                           new ReceiveCallbackType (Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("ping",
			                                                           new ReceiveCallbackType (Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("whoismaster",
			                                                           new ReceiveCallbackType (Receive));						
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("whoismaster_ack",
			                                                           new ReceiveCallbackType (Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("sync_usertable",
			                                                           new ReceiveCallbackType (Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("sync_usertable_ack",
			                                                           new ReceiveCallbackType (Receive));
			
			// TODO: This map should be done elsewhere and less statically
			m_serverUriToServerNameMap.Add (m_server.ServerList[0], "central-1");			
			m_serverUriToServerNameMap.Add (m_server.ServerList[1], "central-2");
			m_serverUriToServerNameMap.Add (m_server.ServerList[2], "central-3");
						
		}
		
		/**
		 * Bootstrap the server
		 */
		public void Start () 
		{									
			// determine who is master
			CurrentMaster = DetermineMaster ();					
			
			if (!IsMaster) 
			{
				DebugInfo ("Synchronizing with Master {0}", CurrentMaster);
				SynchronizeUserTable (); // includes sequence number
				DebugInfo ("Synchronized with master.");		
			}
			
			// I am alive and ready
			UpdateServerStatus (m_server.UserName, true);			
			DebugInfo ("[{0}] Replication Service activated. Master is: {1}", m_server.UserName, CurrentMaster);
			
			/* 
			 * Start failure detector - also manages replication list
			 * FD properties: 
			 *  - detects server crash
			 *  - does not detect slow responses
			 */
			StartPingService();
		}
		
		public void Stop ()
		{
			m_pingTimer.Stop ();
		}
		
		public void SendImNotMasterMessage (Message msg)
		{
			DebugInfo ("Asking {0} to resend to {1}", msg.GetSourceUserName (), CurrentMaster);
			Message m = new Message ();
			m.SetMessageType ("resend");
			m.SetDestinationUsers (msg.GetSourceUserName ());
			m.MessageForResending = msg;
			m.SetSourceUserName (m_server.UserName);
			m.PushString (m_server.m_userTableService.Lookup (CurrentMaster));
			m_server.m_sendReceiveMiddleLayer.Send (m,msg.GetSourceUri (), msg.GetSourceUserName ());
		}
		
		public void ReplicateUserConnect (string username, string uri) 
		{
			if (IsMaster) 
			{
				DebugInfo ("Sending UserConnect replication request");
				Message m = new Message ();
				m.PushString (uri);
				m.PushString (username);
				m.PushString ("user_connect");
				DistributeReplicationMessage (m);
			}
		}
				
		public void ReplicateUserDisconnect (string username) 
		{
			if (IsMaster) 
			{
				DebugInfo ("Sending UserDisconnect replication request");
				Message m = new Message ();
				m.PushString (username);
				m.PushString ("user_disconnect");
				DistributeReplicationMessage (m);
			}			
		}
		
		public void ReplicateSequenceNumber (long number) 
		{
			if (IsMaster) 
			{
				DebugInfo ("Sending sequence number ({0}) replication request", number);			
				Message m = new Message ();
				m.PushString (number.ToString ());
				m.PushString ("sequencenumber"); // subtyped message								
				DistributeReplicationMessage (m);
			}
		}
		
		public void PrintReplicationList ()
		{
			Console.WriteLine ("ReplicationList: (Master is {0})", CurrentMaster);
			foreach (string server in m_replicationList) 
			{
				Console.WriteLine ("> {0}", server);
			}
		}
		

		public void Receive (ReceiveMessageEventArgs e) 
		{							
			Message m = e.m_message;
			
			if (m.GetMessageType ().Equals ("replicate")) 
			{
				string subtype = m.PopString ();				
				
#region replicate
				if (subtype.Equals ("sequencenumber")) 
				{
					DebugLogic ("Got sequence number replication request from {0}", m.GetSourceUserName ());
					HandleSequenceNumberReplication (m.PopString (), m.GetSourceUserName ());					
				}
				else if (subtype.Equals ("sequencenumber_ack")) 
				{							
					DebugLogic ("ACK replication request: SequenceNumber");
								
					m_oSignalEvent.Set ();					
				}
				else if (subtype.Equals ("user_connect"))
				{
					DebugLogic ("Got user_connect replication request for {0}", m.GetSourceUserName ());
					string username = m.PopString ();
					string uri = m.PopString ();
					HandleUserConnectReplication (username, uri, m.GetSourceUserName ());
				}
				else if (subtype.Equals ("user_connect_ack"))
				{
					DebugLogic ("ACK replication request: UserConnect");
					m_oSignalEvent.Set ();
				}
				else if (subtype.Equals ("user_disconnect"))
				{
					DebugLogic ("Got user_disconnect replication request for {0}", m.GetSourceUserName ());
					string username = m.PopString ();
					HandleUserDisconnectReplication (username, m.GetSourceUserName ());
				}
				else if (subtype.Equals ("user_disconnect_ack"))
				{
					DebugLogic ("ACK replication request: UserDisconnect");
					m_oSignalEvent.Set ();
				}
#endregion
			} 
			else if (m.GetMessageType ().Equals ("whoismaster"))
			{		
				string reply; 
				Message ack = new Message ();
				m.SetMessageType ("whoismaster_ack");
				m.SetDestinationUsers (m.GetSourceUserName ());
				m.SetSourceUserName (m_server.UserName);
				
				// TODO: only reply if we've bootstrapped
				if (String.IsNullOrEmpty (CurrentMaster)) 
				{
					m.PushString ("empty");
					reply = "empty";
				}
				else 
				{
					m.PushString (CurrentMaster);
					reply = CurrentMaster;
				}
				
				m_server.m_sendReceiveMiddleLayer.Send (m);
				DebugInfo ("Got question: Who is master? Replying: {0}", reply);
			}
			else if (m.GetMessageType ().Equals ("whoismaster_ack"))
			{
				m_masterReplyBuffer = m.PopString ();
				if (m_masterReplyBuffer.Equals ("empty"))
				{
					DebugFatal ("There are other servers alive, but I could not determine who is master.\n" +
						"Try starting me again.");
				}
				m_oSignalEvent.Set ();
			}
			else if (m.GetMessageType ().Equals ("sync_usertable"))
			{
				if (IsMaster)
				{
					DebugLogic ("Got sync request from {0}", m.GetSourceUserName ());
					// TODO: need to lock usertable and pause replying to connect/disconnect requests
					Message reply = PackUserTable();
					reply.SetMessageType ("sync_usertable_ack");
					reply.SetDestinationUsers (m.GetSourceUserName ());
					reply.SetSourceUserName (m_server.UserName);			
					m_server.m_sendReceiveMiddleLayer.Send (reply);	
				}
			}
			else if (m.GetMessageType ().Equals ("sync_usertable_ack"))
			{
				UnPackUserTable (m);
				DebugLogic ("Installing usertable from current master.");
				m_oSignalEvent.Set ();
			}
		
		}
		
		private void UnPackUserTable (Message ack) 
		{			
			int size = Int32.Parse (ack.PopString ());
			for (int i = 0; i < size; i++)
			{
				string[] tuple = ack.PopString ().Split (',');
				m_server.m_userTableService.UserConnect (tuple[0], tuple[1]);				
			}			
		}
		
		private void SynchronizeUserTable ()
		{
			Message m = new Message ();
			m.SetMessageType ("sync_usertable");
			m.SetDestinationUsers (CurrentMaster);
			m.SetSourceUserName (m_server.UserName);
			m_server.m_sendReceiveMiddleLayer.Send (m);
			Block ();
		}
		
		private Message PackUserTable ()
		{
			Message m = new Message ();			
			foreach (string user in m_server.m_userTableService.UserTable.Keys)
			{
				m.PushString (String.Format ("{0},{1}", user,
				                             m_server.m_userTableService.UserTable[user]));				
			}
			m.PushString (m_server.m_userTableService.UserTable.Count.ToString ());
			return m;
		}
				
		
		private void DistributeReplicationMessage (Message m) 
		{
			if (m_replicationList.Count == 0)
				return;
			
			lock (this) 
			{
				foreach (string replicationServer in m_replicationList)
				{		
					Message replMsg = (Message) m.Clone ();
					replMsg.SetMessageType ("replicate");
					replMsg.SetSourceUserName (m_server.UserName);
					replMsg.SetDestinationUsers (replicationServer);					
					m_server.m_sendReceiveMiddleLayer.Send (replMsg);
				}
				Block ();
			}
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
		
		
		private void StartPingService () 
		{			
			m_pingTimer = new PingTimer ();
			m_pingTimer.Interval = 1000;
			m_pingTimer.OnPing += Ping;			
			m_pingTimerThread = m_pingTimer.Start ();			
		}
		
		private void Ping ()
		{
			foreach (string server in m_server.ServerList)
			{
				string serverName = m_serverUriToServerNameMap[server];
				
				if (!serverName.Equals (m_server.UserName))
				{					
					try 
					{
						Message m = new Message ();
						m.SetSourceUserName (m_server.UserName);
						m.SetMessageType ("ping");
						m.SetDestinationUsers (serverName);
						m_server.m_sendReceiveMiddleLayer.UnreliableSend (m, server);									
						
						// if server was previously unavailable
						//   add to replication list
						if (!m_serverStatus[serverName])
						{
							AddToReplicationList (serverName);
						}
						
						UpdateServerStatus (serverName, true);
					}
					catch (Exception)
					{
						DebugInfo ("Could not reach: {0}", server);
						
						// update serverStatus list
						UpdateServerStatus (serverName, false);		
						RemoveFromReplicationList (serverName);
											
						// if server is master, update current master
						if (serverName.Equals (CurrentMaster) && m_serverStatusFailCount[serverName] >= FAIL_TOLERANCE) 
						{
							ChooseNewMaster (serverName);
							
							// if I became master..? 
							if (IsMaster) 
							{
								DebugInfo ("I am master");
								RemoveFromReplicationList (m_server.UserName);
							}
						} 
						
					}
				}
			}
		}
		
		private string DetermineMaster () 
		{
			foreach (string serverUri in m_server.ServerList)
			{
				string serverName = m_serverUriToServerNameMap[serverUri];
				if (!serverName.Equals (m_server.UserName))
				{
					try {
						Message m = new Message ();
						m.SetMessageType ("whoismaster");
						m.SetDestinationUsers (serverName);
						m.SetSourceUserName (m_server.UserName);
						m_server.m_sendReceiveMiddleLayer.UnreliableSend (m, serverUri);
						
						Block ();
						
						if (m_masterReplyBuffer.Equals ("empty"))
						{
							continue; // ask next server
						}
						
						return m_masterReplyBuffer;
					}
					catch (Exception) // UnreliableSend throws exception if dest is not reachable
					{
						continue;
					}
				}
			}
			
			DebugLogic ("I'm the only server alive. I am master.");			
			return m_server.UserName;			
		}
			
		/*
		 * Deterministic but non-scalable way of electing a new master
		 */
		private void ChooseNewMaster (string oldMaster) 
		{
			if (oldMaster.Equals ("central-1")) 
			{
				CurrentMaster = "central-2";		
			}
			else if (oldMaster.Equals ("central-2"))
			{
				CurrentMaster = "central-3";
			}
			else if (oldMaster.Equals ("central-3"))
			{
				CurrentMaster = "central-1";
			}		
			
			DebugInfo ("New master is {0}, old was {1}", CurrentMaster, oldMaster);
		}
		
		private void AddToReplicationList (string server) 
		{
			m_replicationList.Add (server);
		}
		
		private void RemoveFromReplicationList (string server) 
		{
			m_replicationList.Remove (server);
		}
		
		private void UpdateServerStatus (string server, bool status) 
		{
			if (m_serverStatus.ContainsKey (server))
			{
				m_serverStatus[server] = status;
				
				if (!status) 
				{
					m_serverStatusFailCount[server]++;
				}
				else 
				{
					m_serverStatusFailCount[server] = 0;
				}
			} 
			else
			{
				m_serverStatus.Add (server, status);
				m_serverStatusFailCount.Add (server, 0);
			}
		}
	}
	
	class PingTimer
	{
		public delegate void Ping ();
		
		public int Interval;
		public Ping OnPing;
		
		Thread m_pingThread;
		volatile bool m_stop;
		
		public void Run ()
		{
			while (!m_stop)
			{
				Thread.Sleep (Interval);
				OnPing ();
			}
		}
		
		public Thread Start () 
		{			
			m_stop = false;
			m_pingThread = new Thread (new ThreadStart (Run));
			m_pingThread.IsBackground = true;
			m_pingThread.Start ();
			return m_pingThread;
		}
		
		public void Stop () 
		{
			m_stop = true;
			m_pingThread.Join (1000);
			m_pingThread.Abort ();
		}
	}

}
