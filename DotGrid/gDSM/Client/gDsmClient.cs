/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

using DotGrid.Net;
using DotGrid.DotSec;
using DotGrid.Shared.Enums.gDsm;

namespace DotGrid.gDSM
{
	/// <summary>
	/// Summary description for gDsmClient.
	/// </summary>
	public class gDsmClient
	{
		private object obj;
		private string guid;
		private string manager;
		private string[] nodes;
		private FileAccess access;
		private DotGridSocket socket;
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="access"></param>
		/// <param name="manager"></param>
		/// <param name="nodes"></param>
		public gDsmClient(object obj, FileAccess access, string manager, string[] nodes)
		{
			if(obj == null)
				throw new ArgumentNullException("obj can not be null");
			if(manager == null)
				throw new ArgumentNullException("manager can not be null");
			if(nodes == null)
				throw new ArgumentNullException("nodes can not be null");
			this.obj = obj;
			this.manager = manager;
			this.nodes = nodes;
			this.access = access;
			IPHostEntry hostEntry = Dns.Resolve (manager);
			IPAddress ip = hostEntry.AddressList[0];
			Socket socket = new Socket (ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(new IPEndPoint (ip, 2698));
			NetworkStream ns = new NetworkStream(socket, FileAccess.ReadWrite, true);
			this.socket = new DotGridSocket(new SecureBinaryReader(ns, null, System.Text.Encoding.ASCII), 
				new SecureBinaryWriter(ns, null, System.Text.Encoding.ASCII));
			this.guid = Guid.NewGuid().ToString();
			this.socket.WriteByte((byte)gDsmMethods.CreateNew);
			this.socket.WriteObject(new SharedObjectInfo(this.obj, this.guid, this.manager, this.nodes, access));
			this.socket.CheckExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets unique ID of this shared object on gDsm.
		/// </summary>
		public string GUID
		{
			get
			{
				return this.guid;
			}
		}
		//**************************************************************************************************************//
	}
}
