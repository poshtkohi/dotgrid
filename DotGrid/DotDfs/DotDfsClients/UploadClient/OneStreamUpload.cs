/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections;
using System.Net.Sockets;

using DotGrid.Net;
using DotGrid.DotSec;
using DotGrid.DotDfs;
using DotGrid.Shared.Enums;
using DotGrid.Shared.Headers;
using DotGrid.Shared.Headers.DotDFS;
using DotGrid.Shared.Enums.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for OneStreamUpload.
	/// </summary>
	internal class OneStreamUpload
	{
		private string guid;
		private bool secure;
		private DateTime t1;
		private DateTime t2;
		private long fileSize;
		private int parallel;
		private Thread worker;
		private long written;
		private int tcpBufferSize;
		private QueueRead qread;
		private NetworkCredential nc;
		//private DotGridSocket socket;
		private string remoteFilename;
		private string dotDfsServerAddress;
		private bool closed = false;
		private bool exited = false;
		//private int timeout = 15 * 1000; // 15s timeout
		private RijndaelEncryption rijndael;
		private SecureBinaryReader reader;
		private SecureBinaryWriter writer;
		private bool memmoryToMemoryTests;
		private ArrayList errors;
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="qread"></param>
		/// <param name="remoteFilename"></param>
		/// <param name="guid"></param>
		/// <param name="fileSize"></param>
		/// <param name="parallel"></param>
		/// <param name="tcpBufferSize"></param>
		/// <param name="dotDfsServerAddress"></param>
		/// <param name="nc"></param>
		/// <param name="secure"></param>
		/// <param name="rijndael"></param>
		/// <param name="memmoryToMemoryTests"></param>
		/// <param name="errors"></param>
		public OneStreamUpload(ref QueueRead qread, string remoteFilename, string guid, long fileSize, int parallel, int tcpBufferSize, string dotDfsServerAddress, NetworkCredential nc, bool secure, ref RijndaelEncryption rijndael, bool memmoryToMemoryTests, ref ArrayList errors)
		{
			if(qread == null)
				throw new ArgumentNullException("qread is a null reference.");
			if(guid == null)
				throw new ArgumentNullException("guid is a null reference.");
			if(nc == null)
				throw new ArgumentNullException("nc is a null reference.");
			if(fileSize <= 0)
				throw new ArgumentOutOfRangeException("fileSize parameter can not be negative or zero.");
			if(parallel <= 0)
				throw new ArgumentOutOfRangeException("parallel parameter can not be negative or zero.");
			if(tcpBufferSize <= 0)
				throw new ArgumentOutOfRangeException("tcpBufferSize parameter can not be negative or zero.");
			if(dotDfsServerAddress == null)
				throw new ArgumentNullException("dotDfsServerAddress is a null reference.");
			if(nc == null)
				throw new ArgumentNullException("nc is a null reference.");
			/*if(rijndael == null)
				throw new ArgumentNullException("rijndael is a null reference.");*/
			this.qread = qread;
			this.remoteFilename = remoteFilename;
			this.guid = guid;
			this.fileSize = fileSize;
			this.parallel = parallel;
			this.tcpBufferSize = tcpBufferSize;
			this.dotDfsServerAddress = dotDfsServerAddress;
			this.nc = nc;
			this.secure = secure;
			this.rijndael = rijndael;
			this.memmoryToMemoryTests = memmoryToMemoryTests;
			this.errors = errors;
			IPHostEntry hostEntry = Dns.Resolve (dotDfsServerAddress);
			IPAddress ip = hostEntry.AddressList[0];
			Socket sock = new Socket (ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			if(sock == null)
				throw new ObjectDisposedException("Could not instantiate from Socket class.");
			sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, tcpBufferSize);//
			sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, tcpBufferSize);//
			sock.Connect(new IPEndPoint (ip, 2799));
			//socket = new DotGridSocket(sock, new SecureBinaryReader(ns, null, System.Text.Encoding.ASCII), new SecureBinaryWriter(new NetworkStream(sock, FileAccess.ReadWrite), null, System.Text.Encoding.ASCII));
			PublicKeyAuthentication(sock);
			DotGridSocket socket = new DotGridSocket(reader, writer);
			FileTransferInfo info = new FileTransferInfo(guid, remoteFilename, fileSize, parallel, tcpBufferSize);
			try
			{
				socket.WriteByte((byte)TransferChannelMode.SingleFileTransferUploadFromClient);
				socket.WriteObject(info);
				socket.CheckExceptionResponse();
			}
			catch(Exception e) { WorkerExit(); throw e; }
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public long WrittenSize
		{
			get
			{
				return written;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public DateTime StartTime
		{
			get
			{
				return this.t1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public DateTime EndTime
		{
			get
			{
				return this.t2;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		private void WorkerProc()
		{
			DotGridSocket socket = new DotGridSocket(reader, writer);
			t1 = DateTime.Now;
			byte[] seekValue = new byte[8];
			byte[] readValue = new byte[8];
			byte[] buffer = new byte[tcpBufferSize];
			long offsetSeek = 0;
			try
			{
				while(true)
				{
					if(closed)
					{
						exited = true;
						return ;
					}
					qread.Read(ref buffer, ref offsetSeek);
					if(offsetSeek == -1)
					{
						t2 = DateTime.Now;
						//Console.WriteLine(t2);
						break;
					}
					/*if(mode == TransferMode.DotDFS)
					{
						byte[] seekValue = LongValueHeader.GetBytesOfLongNumber((ulong)infoRead.OffsetSeek);
						byte[] readValue = LongValueHeader.GetBytesOfLongNumber((ulong)infoRead.Length);
						byte b = (byte)(seekValue.Length | (readValue.Length << 4));
						if(closed)
							break;
						byte[] buffer = new byte[1 + seekValue.Length + readValue.Length + infoRead.Length];
						buffer[0] = b;
						Array.Copy(seekValue, 0, buffer, 1, seekValue.Length);
						Array.Copy(readValue, 0, buffer, 1 + seekValue.Length, readValue.Length);
						Array.Copy(infoRead.Buffer, 0, buffer, 1 + seekValue.Length + readValue.Length, infoRead.Length);
						socket.Write(buffer);
						socket.Write(seekValue, 0, seekValue.Length);
						socket.Write(readValue, 0, readValue.Length);
						socket.Write(infoRead.Buffer, 0, infoRead.Length);
					}
					else
					{*/
					LongValueHeader.GetBytesOfLongNumberForGridFTPMode(ref seekValue, (ulong)offsetSeek);
					LongValueHeader.GetBytesOfLongNumberForGridFTPMode(ref readValue, (ulong)buffer.Length);
					if(closed)
						break;
					/*byte[] buffer = new byte[1 + seekValue.Length + readValue.Length + infoRead.Length];
					buffer[0] = 1;
					Array.Copy(seekValue, 0, buffer, 1, seekValue.Length);
					Array.Copy(readValue, 0, buffer, 1 + seekValue.Length, readValue.Length);
					Array.Copy(infoRead.Buffer, 0, buffer, 1 + seekValue.Length + readValue.Length, infoRead.Length);
					socket.Write(buffer);
					//buffer = null;*/
					//Console.WriteLine("offset: {0}, length: {1}", offsetSeek, buffer.Length);
					socket.WriteByte(1);// signaling the server about arrival of new file block.
					socket.Write(seekValue, 0, seekValue.Length);
					socket.Write(readValue, 0, readValue.Length);
					socket.Write(buffer, 0, buffer.Length);
					//}
					written += buffer.Length;
					socket.CheckExceptionResponse();
				}
				if(closed)
				{
					exited = true;
					return ;
				}
				buffer = seekValue = readValue = null;
				socket.WriteByte(0); // signaling the server about finalization of transferring file blocks.
				//double elapsed = (t2 - t1).TotalMilliseconds;
				//Console.WriteLine("Seconds: {0} ", elapsed/1000);
				//Console.WriteLine("Real Average Speed(MBytes/s): {0} MBytes/s", (fileSize/(1024*1024))/(elapsed / 1000)   );
				//qread.Close();
			}
			catch(Exception e) 
			{
				//qread.Close();
				errors.Add(e);
				WorkerExit();
				//throw e;
			}
			exited = true;
			WorkerExit();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public void Run()
		{
			worker = new Thread(new ThreadStart(WorkerProc));
			worker.Start();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		private void WorkerExit()
		{
			//t2 = DateTime.Now;
			try
			{
				if(this.reader != null)
					this.reader.Close();
				this.reader = null;
				this.writer = null;
			}
			catch { }
			worker = null;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public void Close()
		{
			//try { if(this.socket != null)  this.socket.Close(); } catch {}
			//this.socket = null;
			//int _timeout = 0;
			closed = true;
			/*while(!exited)
			{
				_timeout++;
				if(_timeout == this.timeout)
				{
					try { worker.Abort(); } catch { }
					break;
				}
				Thread.Sleep(1);
			}*/
			if(this.reader != null)
				this.reader.Close();
			this.reader = null;
			this.writer = null;
			exited = exited; //
			if(worker != null)
				worker.Abort();
			//WorkerExit();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sock"></param>
		private void PublicKeyAuthentication(Socket sock)
		{
			NetworkStream ns = new NetworkStream(sock, FileAccess.ReadWrite, true);
			reader = new SecureBinaryReader(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			DotGridSocket socket = new DotGridSocket(reader, writer);
			byte[] buffer;
			try { buffer = socket.Read(3 + 128);  } /*public-key.Length + modulus.Length */ 
			catch(Exception e) { Close(); throw e; }
			//Console.WriteLine(buffer.Length);
			if(buffer.Length > 3 + 128 || buffer.Length < 128 + 1)
			{
				Close(); 
				throw new ArgumentOutOfRangeException("The server replied the RSAPublicHeader in bad format."); 
			}
			else
			{
				byte[] e = null;
				switch(buffer.Length)
				{
					case 131 /*128 + 3*/://  for Microsoft .NET RSA implementation. Microsoft generates e in public key (e,n) with 3 bytes.
						e = new byte[3];
						e[0] = buffer[0];
						e[1] = buffer[1];
						e[2] = buffer[2];
						break;
					case 130/*128 + 2*/://for other RSA implementation
						e = new byte[2];
						e[0] = buffer[0];
						e[1] = buffer[1];
						break;
					case 129/*128 + 1*/://  for MONO .NET RSA implementation. MONO generates e in public key (e,n) with 3 bytes.
						e = new byte[1];
						e[0] = buffer[0];
						break;
					default:
						Close(); 
						throw new ArgumentOutOfRangeException("Client implementation does'nt support public key in (e,n) with greater than 3 bytes.");
				}
				byte[] modulus = new byte[128];
				Array.Copy(buffer,e.Length , modulus, 0, modulus.Length); 
				RSA rsa = new RSA(e, modulus); // server RSA public key
				SharedKeyHeader skh = new SharedKeyHeader(secure, rsa, rijndael);
				try { socket.Write(skh.Buffer); }   
				catch(Exception ee) { Close(); throw ee; }
				try { socket.CheckExceptionResponse(); } 
				catch(Exception ee) { Close(); throw ee; }
			}
			reader = new SecureBinaryReader(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			writer = new SecureBinaryWriter(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			//this.socket = new DotGridSocket(sock, rijndael);
			socket = new DotGridSocket(reader, writer);
			buffer = AuthenticationHeaderBuilder(nc.UserName, nc.Password);
			try { socket.Write(buffer); } 
			catch(Exception ee) { Close(); throw ee; } //sends AuthenticationHeader
			byte response;
			try { response = socket.ReadByte(); }  
			catch(Exception ee) { Close(); throw ee; }
			switch(response) //considers reponse for authorization.
			{
				case (byte)ClientAuthenticationError.OK:
					break;
				case (byte)ClientAuthenticationError.NO:
				{
					Close();
					throw new System.Security.SecurityException("Username or Password is wrong.");
				}
				case (byte)ClientAuthenticationError.BAD:
				{
					Close();
					throw new ArgumentException("Bad format for n bytes of username and m bytes of password (buffer.Length != 2+n+m and buffer.Length less than 2)");
				}
				default :
				{
					Close();
					throw new ArgumentException("The server replied with an unrecognized code for login state.");
				}
			}
			if(!secure)
			{
				reader = new SecureBinaryReader(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
				writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
				//this.socket = new DotGridSocket(sock, null);
			}
			//reader = null;
			//writer = null;
			buffer = null;
			GC.Collect();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Builds AuthenticationHeader filed for user login.
		/// </summary>
		/// <param name="username">Username of the user.</param>
		/// <param name="password">Password of the user.</param>
		/// <returns>bytes of AuthenticationHeader.</returns>
		private byte[] AuthenticationHeaderBuilder(string username, string password)
		{
			if(username == null || password == null)
				throw new Exception("Username or Password is empty.");
			if(username.Length > 256 || password.Length > 256)
				throw new Exception("Username or Password length can not be more than 256 characters.");
			byte[] temp = new byte[2 +  username.Length + password.Length];
			temp[0] = (byte)username.Length;
			temp[1] = (byte)password.Length;
			InsertStringToBuffer(username + password, temp, 2);
			return temp;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Insert the str to buffer with the initial index of buffer.
		/// </summary>
		/// <param name="str">Desried string for inserting to buffer, str must be ASCFII String.</param>
		/// <param name="buffer">Buffer stream.</param>
		/// <param name="index">Starting position in buffer for inserting str onto buffer.</param>
		private void InsertStringToBuffer(string str, byte[] buffer, int index)
		{
			if(str == null)
				return ;
			if(buffer.Length - index < str.Length)
				throw new Exception("Length of str is greater than buffer length.");
			if(index < 0)
				throw new Exception("index can not be negative.");
			for(int i = 0 ; i < str.Length ; i++)
				buffer[i + index] = (byte)str[i];
			return ;
		}
		//**************************************************************************************************************//
		/*private ReadInfo ReadFileBlock()
		{
			if(qread.Count != 0)
				return qread.Read();
			else
			{
				if(qread.Locked)
				{
					while(qread.Locked)
						Thread.Sleep(1);
				}
				qread.Locked = true;
				ReadInfo info = qread.Read();
				qread.Locked = false;
				return info;
			}
		}*/
		//**************************************************************************************************************//
	}
}