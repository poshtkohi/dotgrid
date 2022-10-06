/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;
using System.Reflection;
using System.Net.Sockets;

using DotGrid;
using DotGrid.DotSec;
using DotGrid.Shared.Enums;
using DotGrid.Shared.Headers;
using DotGrid.Serialization;

namespace DotGrid.DotThreading
{ 
	/// <summary>
	/// States a remote thread on DotGridThreadServer. Use this class only for a thread. Don't build much more than one instance of this class for one application, in this cases use the ThreadCollectionClient class.
	/// </summary>
	public sealed class DotGridThreadClient : IDisposable
	{
		private ThreadStart start;
		private Module[] modules;
		private SecureBinaryReader reader;
		private SecureBinaryWriter writer;
		private bool secure = true;
		private object returnedObj = null;
		private bool isAlive = true;
		private Thread thread;
	    private RijndaelEncryption rijndael;
		private bool disposed = false;
		private int tcpBufferSize = 0;// 64KB  for none secure and 32KB for secure connections
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the DotGridThreadClient class.
		/// </summary>
		/// <param name="start">A ThreadStart delegate that references the methods to be invoked when this thread begins executing.</param>
		/// <param name="modules">States modules that are depended to start parameter for convenient remote assembly loading.</param>
		/// <param name="DotGridThreadServerAddress">DotGridThreadClient server address.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination dotDfs server.</param>
		/// <param name="Secure">Determine secure or secureless connection.</param>
		public DotGridThreadClient(ThreadStart start, Module[] modules, string DotGridThreadServerAddress, NetworkCredential nc, bool Secure)
		{
			if(start == null)
				throw new ArgumentNullException("start can not be null.");
			if(modules == null)
				throw new ArgumentNullException("modules can not be null.");
			if(DotGridThreadServerAddress == null)
				throw new ArgumentNullException("You must state a DotGridThreadServerAddress for the remote DotGridThreadClient server.");
			if(nc == null)
				throw new ArgumentNullException("nc can not be null.");
			this.secure = Secure;
			byte[] buffer = AuthenticationHeaderBuilder(nc.UserName, nc.Password);
			IPHostEntry hostEntry = Dns.Resolve(DotGridThreadServerAddress);
			IPAddress ip = hostEntry.AddressList[0];
			Socket socket = new Socket (ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(new IPEndPoint (ip, 3798));
			NetworkStream ns = new NetworkStream(socket, FileAccess.ReadWrite, true);
			//------PublicKeyAuthentication---------------
			rijndael = new RijndaelEncryption(128); // a random 128 bits rijndael shared key
			PublicKeyAuthentication(ns);
			this.reader = new SecureBinaryReader(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			this.writer = new SecureBinaryWriter(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			tcpBufferSize = 32 * 1024;
			if(Send(buffer) == -1) { ConnectionClose(); throw new ObjectDisposedException("The remote server closed the connection."); } //sends AuthenticationHeader
			switch(ReceiveByte()) //considers reponse for authorization.
			{
				case -1:
				{
					ConnectionClose();
					throw new ObjectDisposedException("The remote server closed the connection.");
				}
				case (byte)ClientAuthenticationError.OK:
					break;
				case (byte)ClientAuthenticationError.NO:
				{
					ConnectionClose();
					throw new Exception("Username or Password is wrong.");
				}
				case (byte)ClientAuthenticationError.BAD:
				{
					ConnectionClose();
					throw new Exception("Bad format for n bytes of username and m bytes of password (buffer.Length != 2+n+m and buffer.Length less than 2)");
				}
				default :
				{
					ConnectionClose();
					throw new Exception("The server replied with an unrecognized code for login state.");
				}
			}
			//--------------------------------------------
			if(!Secure)
			{
				this.reader = new SecureBinaryReader(ns, null, System.Text.Encoding.ASCII);
				this.writer = new SecureBinaryWriter(ns, null, System.Text.Encoding.ASCII);
				tcpBufferSize = 256 * 1024;
			}
			if(Send((byte)DotGridThreadServerMode.DotGridThreadServerMode) == -1)
			{
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			this.start = start;
			this.modules = modules;
			InitializeThread();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Creates remote thread.
		/// </summary>
		private void InitializeThread()
		{
			SendObject(new ThreadInfo(start, this.modules));
			ExceptionResponse();
			if(!IsAlive)
			{
				this.returnedObj = null;
				ConnectionClose();
				return ;
			}
			this.thread = new Thread(new ThreadStart(this.StartInternal));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Causes the operating system to change the state of the current instance to ThreadState.Running
		/// </summary>
		public void Start()
		{
			if(disposed)
				throw new ObjectDisposedException("Can not access to a disposed object.");
			this.isAlive = true;
			this.thread.Start();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Raises a ThreadAbortException in the thread on which it is invoked, to begin the process of terminating the thread. Calling this method usually terminates the thread.
		/// </summary>
		public void Abort()
		{
			if(disposed)
				throw new ObjectDisposedException("Can not access to a disposed object.");
			if(IsAlive)
			{
				this.isAlive = false;
				if(!this.thread.IsAlive)
					throw new Exception("Could not abort the aborted or ended thread.");
				if(Send(2) == -1) // means that the remote server must abort execution of this thread instance immedeiately.
				{
					ConnectionClose();
					throw new ObjectDisposedException("The remote server closed the connection.");
				}
				ConnectionClose();
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a value indicating the execution status of the current thread.
		/// </summary>
		public bool IsAlive
		{
			get
			{
				if(this.isAlive)
					return true;
				else return false;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets returned object by server achieved by remote thread execution. 
		/// </summary>
		public object ReturnedObject
		{
			get
			{
				return this.returnedObj;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Executes the remote thread and returns the processed result that states modified received instance.
		/// </summary>
		private void StartInternal()
		{
			if(Send(1) == -1) // means that the remote server must start execution of this thread instance.
			{
				if(!IsAlive)
				{
					this.returnedObj = null;
					ConnectionClose();
					return ;
				}
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection.");
			}
			ExceptionResponse();
			byte[] buffer = Receive(4);
			if(buffer == null)
			{
				if(!IsAlive)
				{
					this.returnedObj = null;
					ConnectionClose();
					return ;
				}
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection.");
			}
			if(buffer.Length != 4)
			{ 
				ConnectionClose();
				throw new ArgumentOutOfRangeException("Bad returned object length by remote server."); 
			}
			int size = (buffer[0] << 24) | (buffer[1]  << 16) | (buffer[2]  << 8) | buffer[3]; // Object Length
			if(size <= 0) 
			{ 
				ConnectionClose();
				throw new ArgumentOutOfRangeException("Object length returned by server can not be negative or zero.");
			}
			//int m = 0, e = 0;
			buffer = new byte[size];
			if(Read(buffer, buffer.Length) == -1)
			{
				if(!IsAlive)
				{
					ConnectionClose();
					throw new ArgumentOutOfRangeException("Bad returned object length by remote server.");
				}
			}
			/*while(size - m > 0)
			{
				if((e = Receive(buffer, m, size - m)) == -1)
					if(!IsAlive)
					{
						ConnectionClose();
						throw new ArgumentOutOfRangeException("Bad returned object length by remote server.");
					}
				m += e;
			}*/
			ConnectionClose();
			this.returnedObj =SerializeDeserialize.DeSerialize(buffer);
			this.isAlive = false;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Does public key authentication.
		/// </summary>
		/// <param name="ns">Network stream.</param>
		private void PublicKeyAuthentication(NetworkStream ns)
		{
			this.reader = new SecureBinaryReader(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			this.writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			tcpBufferSize = 256 * 1024;
			byte[] buffer;
			buffer = Receive(3 + 128); // public-key.Length + modulus.Length
			if(buffer == null)
			{
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			//Console.WriteLine(buffer.Length);
			if(buffer.Length > 3 + 128 || buffer.Length < 128 + 1)
			{
				ConnectionClose(); 
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
						ConnectionClose(); 
						throw new ArgumentOutOfRangeException("Client implementation does'nt support public key in (e,n) with greater than 3 bytes.");
				}
				byte[] modulus = new byte[128];
				Array.Copy(buffer,e.Length , modulus, 0, modulus.Length); 
				RSA rsa = new RSA(e, modulus); // server RSA public key
				SharedKeyHeader skh = new SharedKeyHeader(secure, rsa, rijndael);
				if(Send(skh.Buffer) == -1)
				{ 
					ConnectionClose(); 
					throw new ObjectDisposedException("The remote server closed the connection.");
				}
				//Console.WriteLine("hello");
				ExceptionResponse();
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Receives an object from network stream.
		/// </summary>
		/// <returns>Returned object from network stream.</returns>
		private object ReceiveObject()
		{
			byte[] buffer = Receive(4);
			if(buffer == null)
			{
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			if(buffer.Length != 4)
			{
				ConnectionClose();
				throw new ArgumentOutOfRangeException("The server replied bad format for Object Header."); 
			}
			int size = (buffer[0] << 24) | (buffer[1]  << 16) | (buffer[2]  << 8) | buffer[3]; // Object Length
			if(size <= 0)
			{
				ConnectionClose();
				throw new ArgumentOutOfRangeException("The server replied bad format for Object Header."); 
			}
			buffer = new byte[size];
			if(Read(buffer, buffer.Length) == -1) return -1;
			return SerializeDeserialize.DeSerialize(buffer);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sends an object to network stream.
		/// </summary>
		/// <param name="obj">Favorite object for sending to network stream.</param>
		private void SendObject(object obj)
		{
			byte[] temp = SerializeDeserialize.Serialize(obj);
			byte[] buffer = new byte[4 + temp.Length];  // Length + Object
			buffer[0] = (byte)((temp.Length & 0xFF000000) >> 24);
			buffer[1] = (byte)((temp.Length & 0x00FF0000) >> 16);
			buffer[2] = (byte)((temp.Length & 0x0000FF00) >> 8);
			buffer[3] = (byte) (temp.Length & 0x000000FF);
			Array.Copy(temp, 0, buffer, 4, buffer.Length - 4);
			temp = null;
			if(Write(buffer, 0, buffer.Length) == -1) 
			{ 
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
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
		/// <summary>
		/// Investigates if server reply with an exception response then an exception will be dropped.
		/// </summary>
		private void ExceptionResponse()
		{
			int response = ReceiveByte();
			if(response == -1) 
			{ 
				if(!IsAlive)
				{
					this.returnedObj = null;
					ConnectionClose();
					return ;
				}
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			if((response & 0x0F) == (int)eXception.OK)
			{
				int n = 0;
				byte _EMode = (byte)((response & 0xF0) >> 4);
				switch(_EMode)
				{
					case (byte)EMode.INT8:
						n = ReceiveByte();
						if(n == -1) 
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						break;
					case (byte)EMode.INT16:
					{
						int b0 = ReceiveByte();
						if(b0 == -1) 
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b1 = ReceiveByte();
						if(b1 == -1)
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						n = (b0 << 8) | b1;
						break;
					}
					case (byte)EMode.INT24:
					{
						int b0 = ReceiveByte();
						if(b0 == -1) 
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection.");
						}
						int b1 = ReceiveByte();
						if(b1 == -1) 
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b2 = ReceiveByte();
						if(b2 == -1) 
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						n = (b0 << 16) | (b1  << 8) | b2;
						break;
					}
					case (byte)EMode.INT32:
					{
						int b0 = ReceiveByte();
						if(b0 == -1) 
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b1 = ReceiveByte();
						if(b1 == -1) 
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b2 = ReceiveByte();
						if(b2 == -1) 
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b3 = ReceiveByte();
						if(b3 == -1) 
						{ 
							if(!IsAlive)
							{
								this.returnedObj = null;
								ConnectionClose();
								return ;
							}
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						n = (b0 << 24) | (b1  << 16) | (b2  << 8) | b3;
						break;
					}
					default: 
					{ 
						if(!IsAlive)
						{
							this.returnedObj = null;
							ConnectionClose();
							return ;
						}
						ConnectionClose(); 
						throw new Exception("The server replied on bad state for EMode enum."); 
					}
				}
				if(n <= 0) 
				{ 
					ConnectionClose(); 
					throw new Exception("The exception buffer length replied by server is less than or equal zero."); 
				}
				byte[] buffer = Receive(n);
				if(buffer == null) 
				{ 
					if(!IsAlive)
					{
						this.returnedObj = null;
						ConnectionClose();
						return ;
					}
					ConnectionClose(); 
					throw new ObjectDisposedException("The remote server closed the connection."); 
				}
				if(buffer.Length != n) 
				{
					buffer = null;  
					ConnectionClose(); 
					throw new Exception("The server replied on bad state for exception buffer and ELength field."); 
				}
				try
				{
					throw new Exception("The server has dropped the following exception.", (Exception)SerializeDeserialize.DeSerialize(buffer)); 
				}
				catch(Exception e)
				{
					ConnectionClose();
					throw new Exception("The exception buffer replied by server is in an invalid state.", e);
				}
			}
			if((response & 0x0F) == (int)eXception.NO) 
				return ;
			else 
			{ 
				ConnectionClose(); 
				throw new Exception("The server replied on bad state for exception handling."); 
			}
		}
		//**************************************************************************************************************//
		void IDisposable.Dispose() 
		{
			Dispose(true);
		}
		//**************************************************************************************************************//
		private void Dispose (bool disposing)
		{
			if(disposing && this.reader != null)
				this.reader.Close();
			disposed = true;
			this.reader = null;
			this.writer = null;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Close the connected connection to remote server.
		/// </summary>
		private void ConnectionClose()
		{
			Dispose(true);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads any length buffer from SecureBinaryReader stream.
		/// </summary>
		/// <param name="array">When this method returns, contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		private int Read(byte[] array, int count)
		{
			if(count <= tcpBufferSize)
			{
				return ReadInternal(array, 0, count);
			}
			else
			{
				int temp = 0;
				int sum = 0;
				int i = 0;
				int a = count / tcpBufferSize;
				int q = count % tcpBufferSize;
				while(true)
				{
					if((temp = ReadInternal(array, tcpBufferSize*i, tcpBufferSize)) == -1) 
					{ 
						return -1;
					}
					if(temp == 0)
						break;
					sum += temp;
					i++;
					if(q != 0 && i == a)
					{
						if((temp = ReadInternal(array, tcpBufferSize*i, q)) == -1) 
						{ 
							return -1;
						}
						if(temp == 0) 
							break;
						sum += temp;
						break;
					}
					if(q == 0 && i == a)
						break;
				}
				return sum;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes any length buffer to SecureBinaryReader stream.
		/// </summary>
		/// <param name="array">When this method returns, contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The byte offset in array at which to begin reading.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		private int Write(byte[] array, int offset, int count)
		{
			if(count <= tcpBufferSize)
			{
				return Send(array, offset, count);
			}
			else
			{
				int i = 0;
				int a = array.Length / tcpBufferSize;
				int q = array.Length % tcpBufferSize;
				while(true)
				{
					if(Send(array, tcpBufferSize*i, tcpBufferSize) == -1)
						return -1;
					i++;
					if(q != 0 && i == a)
					{
						if(Send(array, tcpBufferSize*i, q) == -1)
							return -1;
						break;
					}
					if(q == 0 && i == a)
						break;
				}
				return count;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads maximum 32KB or 64KB length buffer from SecureBinaryReader stream.
		/// </summary>
		/// <param name="array">When this method returns, contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The byte offset in array at which to begin reading.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		private int ReadInternal(byte[] array, int offset, int count)
		{
			int m = 0;
			int e = 0;
			while(count - m > 0)
			{
				if((e = Receive(array, offset + m, count - m)) == -1) 
					return -1;
				m += e;
			}
			return m;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a maximum of n bytes from the current network stream into buffer and return it.
		/// </summary>
		/// <param name="n">states n bytes for reading from network stream</param>
		/// <returns>Return received buffer data, if there are'nt no data then null will be returned.</returns>
		private byte[] Receive(int n)
		{
			try
			{
				int m = 0;
				byte[] buffer = new byte[n];
				if((m = this.reader.Read(buffer, 0, buffer.Length)) == 0)
				{
					buffer = null;
					return null;
				}
				else
				{
					byte[] temp = new byte[m];
					for(int i = 0 ; i < m ; i++)
						temp[i] = buffer[i];
					buffer = null;
					return temp;
				}
			}
			catch
			{
				return null;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads count bytes from the stream with offset as the starting point in the byte array.
		/// </summary>
		/// <param name="array">The array to read data into.</param>
		/// <param name="offset">The starting point in the buffer at which to begin reading into the array.</param>
		/// <param name="count">The number of bytes to read.</param>
		/// <returns>The number of characters read into buffer. This might be less than the number of bytes requested if that many bytes are not available, or it might be -1 if there is an error.</returns>
		private int Receive(byte[] array, int offset, int count)
		{
			try
			{
				int m = 0;
				if((m = this.reader.Read(array, offset, count)) == 0)
				{
					return -1;
				}
				else
				{
					return m;
				}
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a bytes from the current network stream into buffer and return it.
		/// </summary>
		/// <returns>Return received buffer data.</returns>
		private int ReceiveByte()
		{
			try
			{
				return (int) this.reader.ReadByte();
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes to the network stream.
		/// </summary>
		/// <param name="buffer">buffer for writing to network stream.</param>
		/// <returns>If any errors occurred, -1 will be returned otherwise 0.</returns>
		private int Send(byte[] buffer)
		{
			try
			{
				this.writer.Write(buffer);
				return 0;
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes to the network stream.
		/// </summary>
		/// <param name="buffer">buffer for writing to network stream.</param>
		/// <returns>If any errors occurred, -1 will be returned otherwise 0.</returns>
		private int Send(byte buffer)
		{
			try
			{
				this.writer.Write(buffer);
				return 0;
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes to the network stream.
		/// </summary>
		/// <param name="buffer">buffer for writing to network stream.</param>
		/// <param name="offset">The starting point in buffer at which to begin writing. </param>
		/// <param name="count">The number of bytes to write.</param>
		/// <returns>If any errors occurred, -1 will be returned otherwise 0.</returns>
		private int Send(byte[] buffer, int offset, int count)
		{
			try
			{
				this.writer.Write(buffer, offset, count);
				return 0;
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
	}
}