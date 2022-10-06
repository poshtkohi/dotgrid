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
	/// This class provides upload capabilities a file to destination DotDFS server supporting paralell TCP connections to increase throughput in when large file transfers.
	/// </summary>
	public class FileTransferUpload
	{
		private bool secure;
		private string guid;
		private int parallel;
		private FileStream fs = null;
		private int tcpBufferSize;
		private string readFilename;
		private ArrayList sockets;
		private NetworkCredential nc;
		private string remoteFilename;
		private string dotDfsServerAddress;
		//private TransferMode mode;
		private RijndaelEncryption rijndael;
		private bool memmoryToMemoryTests = false;
		private bool closed = false;
		private ArrayList errors;
		private DotGridSocket __sock;
		private byte[] buffer;//
		private byte[] val1;//
		private byte[] val2;//
		private DotGridSocket sock;//
		private int n; //
		private DateTime t1;
		private DateTime t2;
		private ArrayList alWrite;
		private long written = 0;
		private int k = 0;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of FileTransferUpload class.
		/// </summary>
		/// <param name="readFilename">The local file name to transfer. For memory-to-memory tests the readFilename must be set to /dev/zero only in Linux and Unix operating systems.</param>
		/// <param name="remoteFilename">The remote file name for writing to it. For memory-to-memory tests the remoteFilename must be set to /dev/null only in Linux and Unix operating systems.</param>
		/// <param name="parallel">Number of parallel TCP Connections.</param>
		/// <param name="tcpBufferSize">Specifies both TCP Window and file read/write buffer sizes.</param>
		/// <param name="dotDfsServerAddress">Provides credentials for password-based authentication schemes to destination DotDFS server.</param>
		/// <param name="nc"></param>
		/// <param name="secure">Determines secure or secureless connection based on DotGrid.DotSec transfer layer security.</param>
		/*<param name="mode"></param>
		  <param name="MaxQueueWorkerSize"></param>*/
		public FileTransferUpload(string readFilename, string remoteFilename, int parallel, int tcpBufferSize/*, int MaxQueueWorkerSize*/, string dotDfsServerAddress, NetworkCredential nc, bool secure/*, TransferMode mode*/)
		{
			if(readFilename == null)
				throw new ArgumentNullException("readFilename is a null reference.");
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
			/*if(!Enum.IsDefined(typeof(TransferMode), mode))
				throw new ArgumentException("mode is'nt supported.");*/
			this.readFilename = readFilename;
			this.remoteFilename = remoteFilename;
			this.parallel = parallel;
			this.tcpBufferSize = tcpBufferSize;
			this.dotDfsServerAddress = dotDfsServerAddress;
			this.nc = nc;
			this.secure = secure;
			if(readFilename.ToLower().Trim().IndexOf("/dev/zero", 0) >= 0 && remoteFilename.ToLower().Trim().IndexOf("/dev/null", 0) >= 0) // for memroy-to-memory tests
				memmoryToMemoryTests = true;
			//this.fs = new FileStream(readFilename, FileMode.Open, FileAccess.Read, FileShare.None, DotGrid.Constants.blockSize);
			this.fs = new FileStream(readFilename, FileMode.Open, FileAccess.Read, FileShare.None);
			//qread = new QueueRead(ref this.fs, tcpBufferSize, parallel/*, MaxQueueWorkerSize*/, memmoryToMemoryTests);
			guid = Guid.NewGuid().ToString();
			//this.mode = mode;
			rijndael = new RijndaelEncryption(128); // a random 128 bits rijndael shared key
			errors = new ArrayList();

			Console.WriteLine("DotDfs Client Revision 1.3");
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
				//socks[i].SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, tcpBufferSize);//
				socks[i].Connect(new IPEndPoint (ip, 2799));
				SecureBinaryReader reader = null;
				SecureBinaryWriter writer = null;
				PublicKeyAuthentication(socks[i], ref reader, ref writer);
				sockets.Add(new DotGridSocket(socks[i], reader, writer));
			}
			Console.WriteLine("sockets num:" + sockets.Count);
			for(int i = 0 ; i < sockets.Count ; i++)
			{
				FileTransferInfo info;
				if(memmoryToMemoryTests) // for memory-to-memory tests
					info = new FileTransferInfo(guid, remoteFilename, long.MaxValue, parallel, tcpBufferSize);
				else
					info = new FileTransferInfo(guid, remoteFilename, fs.Length, parallel, tcpBufferSize);
				try
				{
					__sock = (DotGridSocket)sockets[i];
					__sock.WriteByte((byte)TransferChannelMode.SingleFileTransferUploadFromClient);
					__sock.WriteObject(info);
					__sock.CheckExceptionResponse();
				}
				catch(Exception e) { CloseInternal(); throw e; }
			}
			long offsetSeek = 0;
			val1 = new byte[8];
			val2 = new byte[8];
			buffer = new byte[tcpBufferSize];
			//DotGridSocket sock;
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
			/*if(timer != null)
				timer.Change(0, 1000);*/

			alWrite = new ArrayList();
			Socket s;
			while(true)
			{
				//here:
				if(sockets.Count == 0)
					goto End;

				MakeReadSocketArrayList();

				if(sockets.Count == 0)
					goto End;

				try { Socket.Select(null, alWrite, null, 0); }
				catch { goto End; }
				for(int i = 0 ; i < alWrite.Count ; i++)
				{
					//Console.WriteLine(i);
					/*if(sockets.Count == 1)
							sock = (DotGridSocket)sockets[0];
						else
						{*/
					//Console.WriteLine("count " + alWrite.Count);
					s = (Socket)alWrite[i];
					int index = FindSocketIndex(s);
					if(index == -1) { s.Close(); continue; }
					sock = (DotGridSocket)sockets[index];
					//}
					if(!sock.BaseSocket.Connected)
						goto End;
					try { ReadFromFile(ref buffer, ref offsetSeek); }
					//ReadFromFile(ref buffer, ref offsetSeek);
					catch(Exception e) { CloseInternal(); /*Console.WriteLine(String.Format("con: {0},{1},written:{2}", i, _IsSentLastFileBlock, written));*/throw e; }
					if(!sock.BaseSocket.Connected)
						goto End;
					if(offsetSeek == -1)
					{
						try 
						{ 
							/*sock.BaseStream.WriteByte(1);
									sock.BaseStream.WriteByte(0);
									goto End;*/
							//sock.WriteNoException(); 
							sock.WriteByte(0);
							//RemoveSocketFromArrayList(sock);
							//goto End;
						} 
						catch { /*goto End;*/ RemoveSocketFromArrayList(sock); }
					}
					else
					{
						if(!sock.BaseSocket.Connected)
							goto End;
						if(WriteFileBlockGridFTPMode(sock, offsetSeek, buffer) == -1)
							goto End;
					}
				}
			}
			//}
			End:
				t2 = DateTime.Now;
			CloseInternal();
			return;
			/*OneStream s = new OneStream(ref qread, remoteFilename, guid, fs.Length, parallel, tcpBufferSize, dotDfsServerAddress, nc, secure);
			s.Run();*/
			//workers = new OneStreamUpload[parallel];
			//s[0] = new OneStream(ref qread, remoteFilename, guid, fs.Length, parallel, tcpBufferSize, dotDfsServerAddress, nc, secure);
			/*try 
			{
				for(int i = 0 ; i < workers.Length ; i++)
				{
					if(memmoryToMemoryTests) // for memory-to-memory tests
						workers[i] = new OneStreamUpload(ref qread, remoteFilename, guid, long.MaxValue, parallel, tcpBufferSize, dotDfsServerAddress, nc, secure, ref rijndael, memmoryToMemoryTests, ref errors);
					else
						workers[i] = new OneStreamUpload(ref qread, remoteFilename, guid, fs.Length, parallel, tcpBufferSize, dotDfsServerAddress, nc, secure, ref rijndael, memmoryToMemoryTests, ref errors);
					
				}
				for(int i = 0 ; i < workers.Length ; i++)
					workers[i].Run();
			}
			catch(Exception e)
			{
				CloseInternal();
				throw e;
			}
			if(!memmoryToMemoryTests)
			{
				while(CurrentTransferredBytes != fs.Length && !closed)
				{
					if(errors.Count > 0)
					{
						CloseInternal();
						throw (Exception)errors[0];
					}
					Thread.Sleep(1);
				}
			}
			else
			{
				while(CurrentTransferredBytes != long.MaxValue  && !closed) // for memory-to-memory tests
				{
					if(errors.Count > 0)
					{
						CloseInternal();
						throw (Exception)errors[0];
					}
					Thread.Sleep(1);
				}
			}*/
			//qread.Close();
			//s.Run();
		}
		//**************************************************************************************************************//
		private void MakeReadSocketArrayList()
		{
			if(sockets.Count == 0)
				return ;
			//DotGridSocket[] socks = (DotGridSocket[])this.sockets.ToArray(typeof(DotGridSocket));
			if(alWrite.Count != 0)
				alWrite.Clear();
			for(int i = 0 ; i < sockets.Count ; i++)
			{
				/*try
				{
				*/
					//_Available = socks[i].BaseSocket.Available; 
					alWrite.Add(((DotGridSocket)sockets[i]).BaseSocket);
				/*}
				catch
				{
					RemoveSocketFromArrayList((DotGridSocket)sockets[i]);
				}*/
			}
			//socks = null;
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
		private void CloseInternal()
		{
			//Close();
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
						((DotGridSocket)sockets[i]).Close();
						sockets[i] = null;
					}
			sockets.Clear();
			sockets = null;
			buffer = val1 = val2 = null;
			try {fs.Close();}
			catch{}
			GC.Collect();
		}
		//**************************************************************************************************************//
		private void RemoveSocketFromArrayList(DotGridSocket socket)
		{
			int i = FindSocketIndex(socket.BaseSocket);//Console.WriteLine("sock index: " + i);
			if(i != -1)
			{
				try{((DotGridSocket)sockets[i]).Close();} 
				catch{}
				sockets.RemoveAt(i);
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the current transferred bytes of the local file to remote DotDfs server.
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
		private int WriteFileBlockGridFTPMode(DotGridSocket socket, long offsetSeek, byte[] buffer)
		{
			LongValueHeader.GetBytesOfLongNumberForGridFTPMode(ref val1, (ulong)offsetSeek);
			LongValueHeader.GetBytesOfLongNumberForGridFTPMode(ref val2, (ulong)buffer.Length);
			try
			{
				if(socket.IsSecure)
				{
					socket.WriteByte(1);// signaling the client about arrival of new file block.
					socket.Write(val1, 0, val1.Length);
					socket.Write(val2, 0, val2.Length);
					socket.Write(buffer, 0, buffer.Length);
					written += buffer.Length;
					socket.CheckExceptionResponse();
				}
				else
				{
					socket.BaseStream.WriteByte(1);// signaling the server about arrival of new file block.
					socket.BaseStream.Write(val1, 0, val1.Length);
					socket.BaseStream.Write(val2, 0, val2.Length);
					socket.BaseStream.Write(buffer, 0, buffer.Length);
					written += buffer.Length;
					socket.CheckExceptionResponse();
				}
				return 0;
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Read a file block.
		/// </summary>
		/// <returns></returns>
		public void ReadFromFile(ref byte[] buffer, ref long offsetSeek)
		{
			n = fs.Read(buffer, 0, buffer.Length);
			if(n <= 0)
			{
				offsetSeek = -1;
				return ;
			}
			k++;
			//Console.WriteLine("n: " + n);
			if(n < buffer.Length)
			{
				byte[] temp = new byte[n];
				Array.Copy(buffer, 0, temp, 0, n);
				buffer = temp;
				temp = null;
				offsetSeek = fs.Position - n;
				return ;
			}
			else 
			{
				offsetSeek = fs.Position - n;
				return ;
			}
		}
		//**************************************************************************************************************//
		private int ReadFromOriginialSocket(Socket socket, byte[] array, int offset, int count)
		{
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