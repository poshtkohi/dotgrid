/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections;

using DotGrid.Net;
using DotGrid.DotSec;
using DotGrid.Shared.Enums;
using DotGrid.Shared.Headers;
using DotGrid.Shared.Enums.DotDFS;

using DotGrid.DotDfs;
using DotGrid.Shared.Headers.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for FileTransferDownload.
	/// </summary>
	public class FileTransferDownload
	{
		private bool secure;
		private string guid;
		private int parallel;
		private FileStream fs;
		private int tcpBufferSize;
		private string writeFilename;
	    private ArrayList sockets = null;
		private NetworkCredential nc;
		private string remoteFilename;
		private string dotDfsServerAddress;
		private RijndaelEncryption rijndael;
		private ArrayList alRead;
		private int n;
		private byte[] buffer;//
		private byte[] val1;//
		private byte[] val2;//
		private long lastOffset = 0;
		private long lastLength = 0;
		private int j = 0;  // seek numbers
		private int k = 0;  // total writes
		private long written = 0;
		private DateTime t1;
		private DateTime t2;
		private bool closed = false;
		//private int _Available = 0;
		//private bool _IsSentLastFileBlock = false;
		private DotGridSocket __sock;
		//private DotGridSocket __SockLastFileBlock = null;
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="writeFilename"></param>
		/// <param name="remoteFilename"></param>
		/// <param name="parallel"></param>
		/// <param name="tcpBufferSize"></param>
		/// <param name="dotDfsServerAddress"></param>
		/// <param name="nc"></param>
		/// <param name="secure"></param>
		public FileTransferDownload(string writeFilename, string remoteFilename, int parallel, int tcpBufferSize, string dotDfsServerAddress, NetworkCredential nc, bool secure)
		{
			if(writeFilename == null)
				throw new ArgumentNullException("writeFilename is a null reference.");
			if(remoteFilename == null)
				throw new ArgumentNullException("remoteFilename is a null reference.");
			if(parallel <= 0)
				throw new ArgumentOutOfRangeException("parallel parameter can not be negative or zero.");
			if(tcpBufferSize <= 0)
				throw new ArgumentOutOfRangeException("tcpBufferSize parameter can not be negative or zero.");
			if(dotDfsServerAddress == null)
				throw new ArgumentNullException("dotDfsServerAddress is a null reference.");
			if(nc == null)
				throw new ArgumentNullException("nc is a null reference.");
			this.writeFilename = writeFilename;
			this.remoteFilename = remoteFilename;
			this.parallel = parallel;
			this.tcpBufferSize = tcpBufferSize;
			this.dotDfsServerAddress = dotDfsServerAddress;
			this.nc = nc;
			this.secure = secure;
			this.fs = new FileStream(writeFilename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, DotGrid.Constants.blockSize);
			//this.fs = new FileStream(writeFilename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
			guid = Guid.NewGuid().ToString();
			rijndael = new RijndaelEncryption(128); // a random 128 bits rijndael shared key
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Runs the file transfer session to the remote DotDfs server.
		/// </summary>
		public void Run()
		{
			if(sockets != null)
				return ;
			sockets = new ArrayList();//new DotGridSocket[parallel];
			IPHostEntry hostEntry = Dns.Resolve(dotDfsServerAddress);
			IPAddress ip = hostEntry.AddressList[0];
			Socket[] socks = new Socket[parallel];
			for(int i = 0 ; i < socks.Length ; i++)
			{
				socks[i] = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				if(socks[i] == null)
					throw new ObjectDisposedException("Could not instantiate from Socket class.");
				socks[i].SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, tcpBufferSize);//
				socks[i].SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, tcpBufferSize);//
				socks[i].Connect(new IPEndPoint (ip, 2799));
				SecureBinaryReader reader = null;
				SecureBinaryWriter writer = null;
				PublicKeyAuthentication(socks[i], ref reader, ref writer);
				sockets.Add(new DotGridSocket(socks[i], reader, writer));
			}
			for(int i = 0 ; i < sockets.Count ; i++)
			{
				FileTransferInfo info = new FileTransferInfo(guid, remoteFilename, -1, parallel, tcpBufferSize);
				try
				{
					__sock = (DotGridSocket)sockets[i];
					__sock.WriteByte((byte)TransferChannelMode.SingleFileTransferDownloadFromClient);
					__sock.WriteObject(info);
					__sock.CheckExceptionResponse();
				}
				catch(Exception e) { CloseInternal(); throw e; }
			}
			long seekValue = 0;
			long readValue = 0;
			val1 = new byte[8];
			val2 = new byte[8];
			buffer = new byte[tcpBufferSize];
			DotGridSocket sock;
			t1 = DateTime.Now;
			/*if(sockets.Length == 1)
			{
				sock = (DotGridSocket)sockets[0];
				while(true)
				{
					//try { sock.CheckExceptionResponse(); }
					//catch(Exception e) { CloseInternal(); throw e; }
					try 
					{ 
						if(ReadFileBlockGridFTPMode(sock, ref seekValue, ref readValue))
							goto End; 
					}
					catch(Exception e) { CloseInternal(); throw e; }
					try { WriteToFile(seekValue, readValue); }
					catch(Exception e) { CloseInternal(); throw e; }
				}
			}
			else
			{*/
				alRead = new ArrayList();
				Socket s;
				while(true)
				{
					//here:
					if(sockets.Count == 0)
						goto End;

					MakeReadSocketArrayList();

					if(sockets.Count == 0)
						goto End;

					try { DotGridSocket.Select(alRead, null, null, 0); }
					catch { goto End; }
					for(int i = 0 ; i < alRead.Count ; i++)
					{
						/*if(sockets.Count == 1)
							sock = (DotGridSocket)sockets[0];
						else
						{*/
							s = (Socket)alRead[i];
							int index = FindSocketIndex(s);
							if(index == -1) { s.Close(); continue; }
							sock = (DotGridSocket)sockets[index];
						//}
						/*if(__SockLastFileBlock != null)
							if(__SockLastFileBlock == sock)
							{
								Console.WriteLine("hello");
								//RemoveSocketFromArrayList(__SockLastFileBlock);
								continue;
							}*/
						try 
						{ 
							if(ReadFileBlockGridFTPMode(sock, ref seekValue, ref readValue))
							{
								//__SockLastFileBlock = sock;
								RemoveSocketFromArrayList(sock);
								continue;
							}
						}
						catch(Exception e) { CloseInternal(); /*Console.WriteLine(String.Format("con: {0},{1},written:{2}", i, _IsSentLastFileBlock, written));*/throw e; }
						try { WriteToFile(seekValue, readValue); }
						catch(Exception e) { CloseInternal(); throw e; }
						/*if(i == alRead.Count - 1)
							if(_IsSentLastFileBlock)
								goto End;*/
					}
					Thread.Sleep(1);
				}
			//}
			End:
				t2 = DateTime.Now;
				Console.WriteLine("\n\nSeek Number: {0}, Total Writes: {1}", j , k);
			    CloseInternal();
			    return;
		}
		//**************************************************************************************************************//
		private void WriteToFile(long seekValue, long readValue)
		{
			if(seekValue < 0)
				return ;
			k++;
			if(seekValue != lastOffset + lastLength)
			{
				fs.Seek(seekValue, SeekOrigin.Begin);
				fs.Write(buffer, 0, (int)readValue);
				j++;
			}
			else 
				fs.Write(buffer, 0, (int)readValue);
			lastOffset = seekValue;
			lastLength = readValue;
			written += readValue;
		}
		//**************************************************************************************************************//
		private void MakeReadSocketArrayList()
		{
			/*alRead.Clear();
			for(int i = 0 ; i < sockets.Length ; i++)
				alRead.Add(sockets[i].BaseSocket);*/
			if(sockets == null)
				return ;
			if(sockets.Count == 0)
				return ;
			alRead.Clear();
			for(int i = 0 ; i < sockets.Count ; i++)
			{
				__sock = (DotGridSocket)sockets[i];
				try
				{
					//_Available = __sock.BaseSocket.Available; 
					alRead.Add(__sock.BaseSocket);
				}
				catch
				{
					RemoveSocketFromArrayList(__sock);
				}
			}
		}
		//**************************************************************************************************************//
		private void RemoveSocketFromArrayList(DotGridSocket socket)
		{
			int i = FindSocketIndex(socket.BaseSocket);
			if(i != -1)
			{
				try{((DotGridSocket)sockets[i]).Close();} 
				catch{}
				sockets.RemoveAt(i);
			}
		}
		//**************************************************************************************************************//
		private int FindSocketIndex(Socket socket)
		{
			for(int i = 0 ; i < sockets.Count ; i++)
				if(((DotGridSocket)sockets[i]).BaseSocket == socket)
					return i;
			return -1;
		}
		//**************************************************************************************************************//
		private bool ReadFileBlockGridFTPMode(DotGridSocket socket, ref long seekValue, ref long readValue)
		{
			/*if(seekValue < 0)
				return true;*/
			if(socket.IsSecure)
			{
				socket.CheckExceptionResponse();
				if(socket.ReadByte() == 0)
					return true; // meaning end read.
				if(socket.Read(val1, val1.Length) != val1.Length)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				if(socket.Read(val2, val2.Length) != val2.Length)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				seekValue = (long)LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val1);
				readValue = (long)LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val2);
				if(readValue > tcpBufferSize)
					throw new ArgumentOutOfRangeException(string.Format("readValue in the header can not be greater than {0}.", tcpBufferSize));
				//byte[] buffer = new byte[readValue];
				n = socket.Read(buffer, (int)readValue);
				if(n != readValue)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				return false;
			}
			else
			{
				socket.CheckExceptionResponse();
				n = socket.BaseStream.ReadByte();
				if(n == 0)
					return true; // meaning end read.
				if(n == -1)
					throw new ObjectDisposedException("The remote endpoint closed the connection");
				if(ReadFromOriginialSocket(socket.BaseSocket, val1, 0, val1.Length) != val1.Length)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				if(ReadFromOriginialSocket(socket.BaseSocket, val2, 0, val2.Length) != val2.Length)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				seekValue = (long)LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val1);
				readValue = (long)LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val2);
				//Console.WriteLine("readValue:" + readValue);
				if(readValue > tcpBufferSize)
					throw new ArgumentOutOfRangeException(string.Format("readValue in the header can not be greater than {0}.", tcpBufferSize));
				//byte[] buffer = new byte[readValue];
				n = ReadFromOriginialSocket(socket.BaseSocket, buffer, 0, (int)readValue);
				//Console.WriteLine("n:" + n);
				if(n != readValue)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				return false;
			}
		}
		//**************************************************************************************************************//
		private int ReadFromOriginialSocket(Socket socket, byte[] array, int offset, int count)
		{
			//Console.WriteLine("offset:{0}, count: {1}", offset, count);
			int m = 0;
			int e = 0;
			while(count - m > 0)
			{
				if((e = socket.Receive(array, offset + m, count - m, 0)) == -1) 
					throw new ObjectDisposedException("the remote endpoint closed the connection.");
				m += e;
			}
			return m;
		}
		//**************************************************************************************************************//
		private void CloseInternal()
		{
			if(sockets != null)
				for(int i = 0 ; i < sockets.Count ; i++)
					if(sockets[i] != null)
					{
						try {((DotGridSocket)sockets[i]).Close();}
						catch{}
					}
			if(fs != null)
				fs.Close();
			sockets = null;
			fs = null;
			val1 = val2 = buffer = null;//
			GC.Collect();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Closes all connections to remote DotDfs server and worker threads.By invoking this method, all consumed system resources will be released.
		/// </summary>
		public void Close()
		{
			if(closed)
				throw new ObjectDisposedException("This object has been closed.");
			closed = true;
			if(sockets != null)
				for(int i = 0 ; i < sockets.Count ; i++)
					if(sockets[i] != null)
					{
						try{((DotGridSocket)sockets[i]).Close();}
						catch {}
					}

			if(fs != null)
				fs.Close();
			sockets = null;
			fs = null;
			val1 = val2 = buffer = null;//
			GC.Collect();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the current transferred bytes of the remote file located at DotDfs server to local file.
		/// </summary>
		public long CurrentTransferredBytes
		{
			get
			{
				return written;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// After finishing the transfer session, gets total elapsed time for the transfer in milliseconds format.
		/// </summary>
		public double TotalElapsedTime
		{
			get
			{
				return (t2 - t1).TotalMilliseconds;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sock"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		private void PublicKeyAuthentication(Socket sock, ref SecureBinaryReader reader, ref SecureBinaryWriter writer)
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
	}
}