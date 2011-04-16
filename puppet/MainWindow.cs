using System;
using System.Collections.Generic;
using Gtk;

public partial class MainWindow : Gtk.Window
{

	// external
	private puppet.PuppetMaster m_puppetMaster;
	
	// gui elements
	protected Gtk.ListStore m_userStore;
	
	// data sources
	
	
	public MainWindow () : base(Gtk.WindowType.Toplevel)
	{
		Build ();
		
		// add column to tree
		treeViewUsers.AppendColumn ("Users", new Gtk.CellRendererText (), "text", 0);
		
		// set model for tree		
		m_userStore = new Gtk.ListStore(typeof (string));
		treeViewUsers.Model = m_userStore;
		this.ShowAll ();
	}
	
	public void SetPuppetMaster (puppet.PuppetMaster master) 
	{
		m_puppetMaster = master;
		
		m_puppetMaster.RegisterNotificationSubscriber (new puppet.ReceiveNotificationsCallbackType 
		                                               (NotificationUpdate));
		
		
		/*
		Gtk.TreeViewColumn userColumn = new Gtk.TreeViewColumn ();
		userColumn.Title = "User";
		Gtk.CellRendererText userColumnCell = new Gtk.CellRendererText ();
		userColumn.PackStart (userColumnCell, true);
		userColumn.SetCellDataFunc (userColumnCell, new Gtk.TreeCellDataFunc (RenderUser));
		
		
		Gtk.TreeViewColumn statusColumn = new Gtk.TreeViewColumn ();
		statusColumn.Title = "Status";
		Gtk.CellRendererText statusColumnCell = new Gtk.CellRendererText ();
		statusColumn.PackStart (statusColumnCell, true);
		statusColumn.SetCellDataFunc (statusColumnCell, new Gtk.TreeCellDataFunc (RenderStatus));
		
		
		m_userStore = new Gtk.ListStore (typeof (string));		
		
		treeViewUsers.Model = m_userStore;
		treeViewUsers.AppendColumn (userColumn);
		//treeViewUsers.AppendColumn (statusColumn);
		
		ShowAll ();
		*/
	}
	
	protected void RenderUser (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		puppet.UserStatus us = (puppet.UserStatus) model.GetValue (iter, 0);
		(cell as Gtk.CellRendererText).Text = us.UserName;
	}
	
	protected void RenderStatus (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
	{
		puppet.UserStatus us = (puppet.UserStatus) model.GetValue (iter, 0);
		if (us.Status.Equals ("offline"))
		{
			(cell as Gtk.CellRendererText).Foreground = "red";
		}
		else 
		{
			(cell as Gtk.CellRendererText).Foreground = "darkgreen";
		}
		(cell as Gtk.CellRendererText).Text = us.Status;
	}
		
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{		
		Application.Quit ();
		a.RetVal = true;
	}
	
	protected virtual void OnButtonStepClicked (object sender, System.EventArgs e)
	{		
		
		m_puppetMaster.Step ();
	}
	
	protected virtual void OnButtonPlayClicked (object sender, System.EventArgs e)
	{
		m_puppetMaster.Play ();
	}
	
	public void NotificationUpdate (puppet.NotificationEventArgs msg) 
	{		
		Console.WriteLine ("MSG: {0}", msg.Notification);
		if (msg.UserName != null) 
		{
			if (msg.Notification.StartsWith ("REGISTERED")) 
			{
				//puppet.UserStatus u = new puppet.UserStatus (msg.UserName, "online");				
				m_userStore.AppendValues (msg.UserName);		
			}
			else if (msg.Notification.StartsWith ("DISCONNECT")) 
			{								
				// TODO: mark user disconnected				
				/*foreach (object[] o in m_userStore)
				{
					if (o[0].Equals (msg.UserName))
					{
						o[1] = "offline";
					}					
				}*/				
			}
		} 
		
		Gtk.Application.Invoke (delegate {
			TextIter iterator = textMain.Buffer.EndIter;
			textMain.Buffer.Insert(ref iterator, String.Format ("\n{0}", msg.Notification));
			textMain.ScrollToIter(textMain.Buffer.EndIter, 0, false, 0, 0);					
		});	
	}
	
	protected virtual void OnButtonExitClicked (object sender, System.EventArgs e)
	{
		Application.Quit ();
	}
		
}