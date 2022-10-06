using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace DotGridThreadServer
{
	/// <summary>
	/// Main class for DotGridThread daemon.
	/// </summary>
	class MainServer
	{
		//**************************************************************************************************************//
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Console.WriteLine("DotGridThread and DotGridDotRemoteProcess Servers Version 1.\nAll rights reserved to DotGrid team (c) 2002-2009.\nalireza.poshtkohi@gmail.com\n");
			Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPEndPoint hostEndPoint = new IPEndPoint(IPAddress.Any, 3798);
			sock.Bind(hostEndPoint);
			sock.Listen(5);
			ArrayList connections = new ArrayList();
			while(true)
			{
				Socket s = sock.Accept();
				//s.Blocking = false; // only for MONO .NET 1.1
				if(s != null)
				{
					Console.WriteLine("new connection.");
					Server temp = new Server(s, ref connections);
				}
				GC.Collect();
			}
		}
		//**************************************************************************************************************//
	}
}