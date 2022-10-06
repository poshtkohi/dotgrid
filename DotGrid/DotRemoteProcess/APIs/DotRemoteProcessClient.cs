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
using System.Diagnostics;
using System.Net.Sockets;

using DotGrid;
using DotGrid.DotSec;
using DotGrid.Shared.Enums;
using DotGrid.Shared.Headers;
using DotGrid.Serialization;

namespace DotGrid.DotRemoteProcess
{ 
	/// <summary>
	/// States a remote process on DotGridRemoteProcessServer. This class provides both remote job submission and remote process management for remote processes.
	/// </summary>
	public sealed class DotRemoteProcessClient : IDisposable
	{
		private ProcessStartInfo info;
		private string[] dependencies;
		private SecureBinaryReader reader;
		private SecureBinaryWriter writer;
		private bool secure = true;
	    private RijndaelEncryption rijndael;
		private bool disposed = false;
		private bool IsStartedRemoteProcess = false;
		private bool FirstStart = true;
		private int tcpBufferSize = 0;// 64KB  for none secure and 32KB for secure connections
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the DotRemoteProcessClient class without job submission support. Use this constructor only for remote processes management.
		/// </summary>
		/// <param name="DotGridRemoteProcessServerAddress">dotGridThread server address.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination dotDfs server.</param>
		/// <param name="Secure">Determines secure or secureless connection.</param>
		public DotRemoteProcessClient(string DotGridRemoteProcessServerAddress, NetworkCredential nc, bool Secure)
		{
			DotRemoteProcessClientInitialize(null, dependencies, DotGridRemoteProcessServerAddress, nc, Secure);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the DotRemoteProcessClient class with job submission support. Dont use this constructor for remote processes management.
		/// </summary>
		/// <param name="info">Specifies ProcessStartInfo for local remote process to upload to remote server.</param>
		/// <param name="dependencies">File name dependencies for this process.</param>
		/// <param name="DotGridRemoteProcessServerAddress">dotGridThread server address.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination dotDfs server.</param>
		/// <param name="Secure">Determines secure or secureless connection.</param>
		public DotRemoteProcessClient(ProcessStartInfo info, string[] dependencies, string DotGridRemoteProcessServerAddress, NetworkCredential nc, bool Secure)
		{
			DotRemoteProcessClientInitialize(info, dependencies, DotGridRemoteProcessServerAddress, nc, Secure);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the DotRemoteProcessClient class with job submission support. Dont use this constructor for remote processes management.
		/// </summary>
		/// <param name="info">Specifies ProcessStartInfo for local remote process to upload to remote server.</param>
		/// <param name="DotGridRemoteProcessServerAddress">dotGridThread server address.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination dotDfs server.</param>
		/// <param name="Secure">Determines secure or secureless connection.</param>
		public DotRemoteProcessClient(ProcessStartInfo info, string DotGridRemoteProcessServerAddress, NetworkCredential nc, bool Secure)
		{
			DotRemoteProcessClientInitialize(info, null, DotGridRemoteProcessServerAddress, nc, Secure);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a this DotRemoteProcessClient instance.
		/// </summary>
		/// <param name="info">Specifies ProcessStartInfo for local remote process to upload to remote server.</param>
		/// <param name="dependencies">File name dependencies for this process.</param>
		/// <param name="DotGridRemoteProcessServerAddress">dotGridThread server address.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination dotDfs server.</param>
		/// <param name="Secure">Determine secure or secureless connection.</param>
		private void DotRemoteProcessClientInitialize(ProcessStartInfo info, string[] dependencies, string DotGridRemoteProcessServerAddress, NetworkCredential nc, bool Secure)
		{
			if(DotGridRemoteProcessServerAddress == null)
				throw new ArgumentNullException("You must state a DotGridRemoteProcessServerAddress for the remote server.");
			if(nc == null)
				throw new ArgumentNullException("nc can not be null.");
			this.secure = Secure;
			byte[] buffer = AuthenticationHeaderBuilder(nc.UserName, nc.Password);
			IPHostEntry hostEntry = Dns.Resolve(DotGridRemoteProcessServerAddress);
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
			if(Send((byte)DotGridThreadServerMode.DotGridRemoteProcessServerMode) == -1)
			{
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			this.info = info;
			this.dependencies = dependencies;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Starts (or reuses) the remote process resource that is specified by the StartInfo property of this DotRemoteProcessClient component and associates it with the component.
		/// </summary>
		/// <returns>true if a remote process resource is started; false if no new remote process resource is started (for example, if an existing remote process is reused).</returns>
		/// <remarks>Use this overload to start a process resource and associate it with the current DotRemoteProcessClient component. The return value true indicates that a new remote process resource was started. If the remote process resource specified by the FileName member of the RemoteProcessStartInfo property is already running on the remote computer, no additional remote process resource is started. Instead, the running remote process resource is reused and false is returned.</remarks>
		public bool Start()
		{
			if(disposed)
				throw new ObjectDisposedException("Can not access to a disposed object.");
			if(info == null)
				return false;
			if(FirstStart)
			{
				FirstStart = false;
				if(!IsStartedRemoteProcess)
				{
					if(Send((byte)DotRemoteProcessMethod.Start) == -1)
					{
						ConnectionClose();
						throw new ObjectDisposedException("The remote server closed the connection."); 
					}
					SendObject(new RemoteProcessStartInfo(this.info, this.dependencies));
					ExceptionResponse();
					switch(ReceiveByte())
					{
						case 0:
							IsStartedRemoteProcess = false;
							return false;
						case 1:
							IsStartedRemoteProcess = true;
							return true;
						default:
							ConnectionClose(); 
							throw new ArgumentException("The server replied an unknown value for Start method.");
					}
				}
				else return true;
			}
			else
			{
				if(!IsStartedRemoteProcess)
				{
					if(Send((byte)DotRemoteProcessMethod.Start) == -1)
					{
						ConnectionClose();
						throw new ObjectDisposedException("The remote server closed the connection."); 
					}
					ExceptionResponse();
					switch(ReceiveByte())
					{
						case 0:
							IsStartedRemoteProcess = false;
							return false;
						case 1:
							IsStartedRemoteProcess = true;
							return true;
						default:
							ConnectionClose(); 
							throw new ArgumentException("The server replied an unknown value for Start method.");
					}
				}
				else return false;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Immediately stops the associated remote process.
		/// </summary>
		/// <remarks>Kill forces a termination of the remote process, while CloseMainWindow only requests a termination. When a process with a graphical interface is executing, its message loop is in a wait state. The message loop executes every time a Windows message is sent to the process by the operating system. Calling CloseMainWindow sends a request to close to the main window, which, in a well-formed application, closes child windows and revokes all running message loops for the application. The request to exit the process by calling CloseMainWindow does not force the application to quit. The application can ask for user verification before quitting, or it can refuse to quit. To force the application to quit, use the Kill method. The behavior of CloseMainWindow is identical to that of a user closing an application's main window using the system menu. Therefore, the request to exit the process by closing the main window does not force the application to quit immediately. Data edited by the process or resources allocated to the process can be lost if you call Kill. Kill causes an abnormal process termination and should be used only when necessary. CloseMainWindow enables an orderly termination of the process and closes all windows, so it is preferable for applications with an interface. If CloseMainWindow fails, you can use Kill to terminate the process. Kill is the only way to terminate processes that do not have graphical interfaces.You can call Kill and CloseMainWindow only for processes that are running on the local computer. You cannot cause processes on remote computers to exit. You can only view information for processes running on remote computers.</remarks>
		public void Kill()
		{
			if(disposed)
				throw new ObjectDisposedException("Can not access to a disposed object.");
			if(info == null)
				return ;
			if(Send((byte)DotRemoteProcessMethod.Kill) == -1)
			{
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			IsStartedRemoteProcess = false;
			ExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Discards any information about the associated remote process that has been cached inside the remote process component.
		/// </summary>
		/// <remarks>After Refresh is called, the first request for information about each property causes the remote process component to obtain a new value from the associated remote process.When a DotRemoteProcessClient component is associated with a remote process resource, the property values of the DotRemoteProcessClient are immediately populated according to the status of the associated remote process. If the information about the associated remote process subsequently changes, those changes are not reflected in the DotRemoteProcessClient component's cached values. The DotRemoteProcessClient component is a snapshot of the remote process resource at the time they are associated. To view the current values for the associated remote process, call the Refresh method.</remarks>
		public void Refresh()
		{
			if(disposed)
				throw new ObjectDisposedException("Can not access to a disposed object.");
			if(info == null)
				return ;
			if(Send((byte)DotRemoteProcessMethod.Refresh) == -1)
			{
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			ExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Frees and disconnects all the resources that are associated with this component from the DotRemoteProcessClient server,
		/// </summary>
		public void Close()
		{
			if(disposed)
				throw new ObjectDisposedException("Can not access to a disposed object.");
			ConnectionClose();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets remote process information.
		/// </summary>
		public RemoteProcessInfo ProcessInfo
		{
			get
			{
				if(disposed)
					throw new ObjectDisposedException("Can not access to a disposed object.");
				if(info == null)
					return null;
				if(Send((byte)DotRemoteProcessMethod.RemoteProcessInfo) == -1)
				{
					ConnectionClose();
					throw new ObjectDisposedException("The remote server closed the connection."); 
				}
				ExceptionResponse();
				return (RemoteProcessInfo)ReceiveObject();
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets termination time of this remote process instance.
		/// </summary>
		public DateTime ExitTime
		{
			get
			{
				if(disposed)
					throw new ObjectDisposedException("Can not access to a disposed object.");
				if(info == null)
					return DateTime.Now;
				if(Send((byte)DotRemoteProcessMethod.ExitTime) == -1)
				{
					ConnectionClose();
					throw new ObjectDisposedException("The remote server closed the connection."); 
				}
				ExceptionResponse();
				return (DateTime)ReceiveObject();
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public RemoteProcessInfo[] GetRemoteProcesses()
		{
			if(disposed)
				throw new ObjectDisposedException("Can not access to a disposed object.");
			if(Send((byte)DotRemoteProcessMethod.GetRemoteProcesses) == -1)
			{
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			ExceptionResponse();
			return (RemoteProcessInfo[]) ReceiveObject();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Immediately stops the associated remote process with the processId.
		/// </summary>
		/// <param name="processID">the unique identifier for the associated remote process.</param>
		public void KillById(int processID)
		{
			if(disposed)
				throw new ObjectDisposedException("Can not access to a disposed object.");
			if(Send((byte)DotRemoteProcessMethod.KillById) == -1)
			{
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			SendObject(processID);
			ExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets information of associated remote processes by the processId.
		/// </summary>
		/// <param name="processID">the unique identifier for the associated remote process.</param>
		/// <returns>A RemoteProcessInfo that represents the remote process information by the processId running on the remote computer.</returns>
		public RemoteProcessInfo GetRemoteProcessInfoById(int processID)
		{
			if(disposed)
				throw new ObjectDisposedException("Can not access to a disposed object.");
			if(Send((byte)DotRemoteProcessMethod.GetRemoteProcessInfoById) == -1)
			{
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			SendObject(processID);
			ExceptionResponse();
			return (RemoteProcessInfo) ReceiveObject();
		}
		//**************************************************************************************************************//
		/*/// <summary>
		/// Returns a new DotRemoteProcessClient component, given the identifier of a process on the remote computer.
		/// </summary>
		/// <param name="RemoteProcessId">The system-unique identifier of a remote process resource.</param>
		/// <param name="DotGridRemoteProcessServerAddress">dotGridThread server address.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination dotDfs server.</param>
		/// <param name="Secure">Determines secure or secureless connection.</param>
		/// <returns>Returns a new DotRemoteProcessClient component, given the identifier of a remote process on the remote computer.</returns>
		public static DotRemoteProcessClient GetRemoteProcessById(int RemoteProcessId, string DotGridRemoteProcessServerAddress, NetworkCredential nc, bool Secure)
		{
			DotRemoteProcessClient process = new DotRemoteProcessClient(null, DotGridRemoteProcessServerAddress, nc, Secure);
			process.GetRemoteProcessByIdInternal(RemoteProcessId);
			return process;
		}*/
		//**************************************************************************************************************//
		/*/// <summary>
		/// Initialize new Process component, given the identifier of a process on the remote computer.
		/// </summary>
		/// <param name="RemoteProcessId">The system-unique identifier of a remote process resource.</param>
		private void GetRemoteProcessByIdInternal(int RemoteProcessId)
		{
			if(Send((byte)DotRemoteProcessMethod.GetRemoteProcessById) == -1)
			{
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			SendObject(RemoteProcessId);
			ExceptionResponse();
		}*/
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
				ExceptionResponse();
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
			if(response == -1) { ConnectionClose(); throw new ObjectDisposedException("The remote server closed the connection."); }
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
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						break;
					case (byte)EMode.INT16:
					{
						int b0 = ReceiveByte();
						if(b0 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b1 = ReceiveByte();
						if(b1 == -1) { ConnectionClose(); throw new ObjectDisposedException("The remote server closed the connection."); }
						n = (b0 << 8) | b1;
						break;
					}
					case (byte)EMode.INT24:
					{
						int b0 = ReceiveByte();
						if(b0 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection.");
						}
						int b1 = ReceiveByte();
						if(b1 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b2 = ReceiveByte();
						if(b2 == -1) 
						{ 
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
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b1 = ReceiveByte();
						if(b1 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b2 = ReceiveByte();
						if(b2 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b3 = ReceiveByte();
						if(b3 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						n = (b0 << 24) | (b1  << 16) | (b2  << 8) | b3;
						break;
					}
					default: 
					{ 
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
		/// <returns>Return received buffer data, if there aren't no data then null will be returned.</returns>
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