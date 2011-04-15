using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using common;

namespace client
{

	public class StressTestClient
	{
		private int m_currentPortOffset;
		private const int START_PORT = 20000;
		private const int TOCREATE = 20;
		private const int REQUESTS = 50;
		
		public List<Thread> SpawnedThreads {
			get;
			set;
		}

		public StressTestClient ()
		{
			m_currentPortOffset = 0;
			SpawnedThreads = new List<Thread> ();
		}
		
		public void Run ()
		{			
			Console.WriteLine ("Starting {0} clients", TOCREATE);
			
			if (!ConfigReader.ReadFile ("/home/archie/repos/radical/client/stress.config"))
					Environment.Exit (0);
			
			for (int i = 0; i < TOCREATE; i++)
			{
				Thread.Sleep (100);
				Console.WriteLine ("Starting: {0}", i);	
				Spawner stc = new Spawner ();
				stc.callback = this;
				Thread thread = new Thread(new ThreadStart (stc.SpawnClient));
				thread.IsBackground = true;
				thread.Start ();
				SpawnedThreads.Add (thread);				
			}
			
			
		}
		
		public void UpdateCounter ()
		{
			lock (this)
			{
				m_currentPortOffset++;
			}
		}
		
		class Spawner {		
			private Random randomizer; 
			public StressTestClient callback
			{
				get;
				set;
			}
			
			public Spawner () 
			{
				randomizer = new Random ();
			}
			
			public void SpawnClient () 
			{
				string user = String.Format ("user-{0}", START_PORT+callback.m_currentPortOffset);
				
				Console.WriteLine ("Creating client: {0}", user);
				Client client = new Client ();
				client.LoadConfig (user, START_PORT+callback.m_currentPortOffset);
				
				callback.UpdateCounter ();
				
				client.InitClient ();
				
				Console.WriteLine ("Connecting: {0}", user);
				client.Connect ();

				for (int i = 0; i < REQUESTS; i++) 
				{
					Thread.Sleep (randomizer.Next (1000));
					Console.WriteLine ("Ask Sequence number: {0}", user);				
					client.GetSequenceNumber ();
				}
				
				Thread.Sleep (randomizer.Next (1000));
				Console.WriteLine ("Disconnecting: {0}", user);
				client.Disconnect ();
				
				Console.WriteLine ("Stopped client: {0}", user);
				client.Stop ();
				
			}
		}
		
	}
}
