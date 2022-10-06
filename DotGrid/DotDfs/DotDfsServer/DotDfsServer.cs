/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Net;
using System.Threading;
using System.Collections;
using System.Net.Sockets;
//using System.Windows.Forms;

using DotGrid.DotSec;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// This class provides running capability of a DotDfs server instance on a machine with a separte worker thread. Attend that installed firewall on remote machine must allow the DotDfs server instance to run. 
	/// </summary>
	public class DotDfsServer
	{
		private Hashtable sessions;
		private ArrayList connections;
		private RSA ServerRSA;
		private Socket sock;
		private Thread worker;
		private bool closed = false;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes an DotDfsServer instance. 
		/// </summary>
		public DotDfsServer()
		{
			Console.WriteLine("DotGrid.DotDFS Server Version 1.\nAll rights reserved to DotGrid team (c) 2005-2006.\n");
			sessions = new Hashtable();
			connections = new ArrayList();
			ServerRSA = new RSA(); // generates a random server RSA public-private key
			worker = new Thread(new ThreadStart(this.WorkerProc));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Start execution of this DotDfs server instance.
		/// </summary>
		public void Start()
		{
			if(!worker.IsAlive)
				worker.Start();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Manages the worker thread.
		/// </summary>
		private void WorkerProc()
		{
			sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint hostEndPoint = new IPEndPoint(IPAddress.Any, 2799);
			//sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
			sock.Bind(hostEndPoint);
			sock.Listen(1000);
			while(true)
			{
				if(closed)
					break;
				Socket s = sock.Accept();
				//s.Blocking = false; // only for MONO .NET 1.1.6 on Windows
				if(s != null)
				{
					//__i++;
					Server info = new Server(s, ref sessions, ref ServerRSA, ref connections);
				}
				//GC.Collect();
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Closes all connected connections to DotDfs server and worker threads.By invoking this method, all consumed system resources will be released.
		/// </summary>
		public void Close()
		{
			if(worker == null)
				throw new ObjectDisposedException("The worker thread has been closed.");
			closed = true;
			//MessageBox.Show("Count: " + connections.Count);
			if(connections.Count > 0)
			{
				for(int i = 0 ; i < connections.Count ; i++)
				{
					object[] temp = (object[])connections[i];
					if(temp != null)
					{
						if(temp[0] != null)
							((Thread)temp[0]).Abort();
						if(temp[1] != null)
							((Socket)temp[1]).Close();
						//MessageBox.Show(i.ToString());
					}
				}
			}
			sock.Close();
			worker.Abort();
			connections.Clear();
			sessions.Clear();
			connections = null;
			sessions = null;
			sock = null;
			worker = null;
			GC.Collect();
		}
		//**************************************************************************************************************//
	}
}