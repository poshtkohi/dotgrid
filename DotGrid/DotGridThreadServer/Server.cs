using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Runtime.Remoting;
using System.Runtime.Serialization;

using DotGrid;
using DotGrid.DotSec;
using DotGrid.DotThreading;
using DotGrid.DotDfs;
using DotGrid.Shared.Enums;
using DotGrid.Shared.Enums.DotDFS;
using DotGrid.Shared.Headers;
using DotGrid.DotRemoteProcess;
using DotGrid.DotSec.Permission;
using DotGrid.Serialization;

namespace DotGridThreadServer
{
	/// <summary>
	/// Implements DotGridThreadServer and DotGridRemoteProcessServer daemons.
	/// </summary>
	public class Server
	{
		private ArrayList connections;
		private Thread thread;
		private Thread processorThread = null;
		private SecureBinaryReader reader;
		private SecureBinaryWriter writer;
		private Socket client;
		private NetworkCredential nc;
		private ThreadInfo info;
		private RemoteProcessStartInfo startInfo;
		private string RootDirectory;
		private AppDomain domain = null;
		private bool isEndedThreadExecution = false;
		private bool IsAborted = false;
		private bool IsStartedMainThread = false;
		private bool WithoutStartMethod = false;
		private bool isOccuredUnhandeldException = false;
		private bool IsStartedRemoteProcess = false;
		private Process remoteProcess = null;
		private RSA ServerRSA;
		private RijndaelEncryption rijndael;
		private bool secure = true;
		private int BufferSize = 0;// 64KB  for none secure and 32KB for secure connections
		//private static readonly int HeaderSize = 1024;
		//**************************************************************************************************************//
		/// <summary>
		/// Construct a server instance.
		/// </summary>
		/// <param name="Client">Client socket.</param>
		/// <param name="Connections">Live connections.</param>
		public Server(Socket Client, ref ArrayList Connections)
		{
			this.connections = Connections;
			this.connections.Add(this);
			this.client = Client;
			this.thread = new Thread(new ThreadStart(this.ThreadProc));
			this.thread.Start();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Mange new DotGridThread session with the underlying thread OS.
		/// </summary>
		private void ThreadProc()
		{
			this.nc = new NetworkCredential("alireza", "furnaces2002"); ////
			ServerRSA = new RSA(); // generates a random server RSA public-private key
			NetworkStream ns = new NetworkStream(this.client, FileAccess.ReadWrite, false);
			//this.client.Blocking = false; //only for MONO .NET 1.1.6
			//--------Public-Key Authentication----------------------------
			if(!PublicKeyAuthentication(ns))  { ThreadExit(); return ; }
			//----------------ClientAuthentication-------------------------
			if(!ClientAuthentication(ns)) { ThreadExit(); return ; }
			//-------------------------------------------------------------
			if(!secure)
			{
				this.reader = new SecureBinaryReader(ns, null, System.Text.Encoding.ASCII);
				this.writer = new SecureBinaryWriter(ns, null, System.Text.Encoding.ASCII);
				BufferSize  = 64 * 1024;
			}
			int ServerMode = ReceiveByte();
			if(ServerMode == -1){ ThreadExit(); return ; }
			//Console.WriteLine((DotGridThreadServerMode)ServerMode);
			switch((DotGridThreadServerMode)ServerMode)
			{
				//---------------DotGridThreadServer Mode------------------
				case DotGridThreadServerMode.DotGridThreadServerMode:
				{
					try 
					{
						object obj = ReceiveObject();
						if(obj == null) { ThreadExit(); return ; }
						info = (ThreadInfo)obj;
						obj = null;
					}
					catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); ThreadExit(); return ; }
					//RootDirectory = @"C:\files\" + nc.UserName + @"\" + info.GUID + @"\";////
					RootDirectory = AppDomain.CurrentDomain.BaseDirectory + "dot-thread-" + info.GUID + @"\";
					//RootDirectory = AppDomain.CurrentDomain.BaseDirectory + info.GUID + @"\";
					try { AssemblyConfigure(info); }
					catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); ThreadExit(); return ; }
					if(Send((byte) eXception.NO) == -1) { ThreadExit(); return ; }
					//----------------Main Session---------------------------------
					while(true)
					{
						if(isEndedThreadExecution){ ThreadExit(); return ; }
						int b = ReceiveByte();
						if(b == -1) { IsAborted = true; ThreadExit(); return ; } // timeout or closed connection
						if(b != -1)
						{
							switch(b)
							{
								case 1: //ThreadStart
									this.processorThread = new Thread(new ThreadStart(this.ThreadStart));
									this.processorThread.Start();
									IsStartedMainThread = true;
									break;
								case 2: // ThreadAbort
								{
									this.IsAborted = true;
									ThreadExit();
									return ;
								}
								default:
								{
									Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not Supported method."))).Buffer);
									ThreadExit();
									return ;
								}
							}
						}
					}
				}
				//---------------DotGridRemoteProcessServer Mode-----------
				case DotGridThreadServerMode.DotGridRemoteProcessServerMode:
				{
					//----------------Main Session---------------------------------
					while(true)
					{
						int b = ReceiveByte();
						if(b == -1) { ThreadExit(); return ; } // timeout or closed connection
						if(b != -1)
						{
							switch((DotRemoteProcessMethod)b)
							{
								case DotRemoteProcessMethod.Start: //Start
								{
									if(Start() == -1) { ThreadExit(); return ; }
									break;
								}
								case DotRemoteProcessMethod.Kill: //Kill
								{
									if(Kill() == -1) { ThreadExit(); return ; }
									break;
								}
								case DotRemoteProcessMethod.Refresh: //Refresh
								{
									if(Refresh() == -1) { ThreadExit(); return ; }
									break;
								}
								case DotRemoteProcessMethod.RemoteProcessInfo: //get_ProcessInfo
								{
									if(get_ProcessInfo() == -1) { ThreadExit(); return ; }
									break;
								}
								case DotRemoteProcessMethod.ExitTime: //get_ExitTime
								{
									if(get_ExitTime() == -1) { ThreadExit(); return ; }
									break;
								}
								/*case Method.GetRemoteProcessById: //GetRemoteProcessById
								{
									WithoutStartMethod = true;
									if(GetRemoteProcessById() == -1) { ThreadExit(); return ; }
									break;
								};*/
								case DotRemoteProcessMethod.GetRemoteProcesses: //GetRemoteProcesses
								{
									WithoutStartMethod = true;
									if(GetRemoteProcesses() == -1) { ThreadExit(); return ; }
									break;
								}
								case DotRemoteProcessMethod.KillById: //KillById
								{
									WithoutStartMethod = true;
									if(KillById() == -1) { ThreadExit(); return ; }
									break;
								}
								case DotRemoteProcessMethod.GetRemoteProcessInfoById: //GetRemoteProcessInfoById
								{
									WithoutStartMethod = true;
									if(GetRemoteProcessInfoById() == -1) { ThreadExit(); return ; }
									break;
								}
								default:
								{
									Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not Supported method."))).Buffer);
									ThreadExit();
									return ;
								}
							}
						}
					}
				}
				//---------------------------------------------------------
				default:
				{
					ThreadExit();
					return ;
				}
				//---------------------------------------------------------
			}
		}
		//------------------------------------DotRemoteProcessServer methods-------------------------------------------------//
		/// <summary>
		/// Starts (or reuses) the process resource that is specified by the StartInfo property of this Process component and associates it with the component.
		/// </summary>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int Start()
		{
			/*if(IsStartedRemoteProcess)
			{
				if(Send((byte) eXception.NO) == -1) return -1;
				if(Send(1) == -1) return -1;
				return 0;
			}*/
			if(this.remoteProcess == null)
			{
				if(!IsStartedRemoteProcess)
				{
					try 
					{
						object obj = ReceiveObject();
						if(obj == null)
							return -1;
						startInfo = (RemoteProcessStartInfo)obj;
						obj = null;
					}
					catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
					//----------------------------------------------------------
					RootDirectory = AppDomain.CurrentDomain.BaseDirectory + "remote-process-" + startInfo.GUID + @"\";
					//RootDirectory = AppDomain.CurrentDomain.BaseDirectory + startInfo.GUID + @"\";
					if(startInfo.FileName != null)
					{
						//if(!Directory.Exists(RootDirectory))
						Directory.CreateDirectory(RootDirectory);
						if(startInfo.Dependencies != null)
						{
							for(int i = 0 ; i < startInfo.Dependencies.Length ; i++)
							{
								string temp = FindMainFileName(startInfo.Dependencies[i]);
								try{ FileCopy(RootDirectory + temp, startInfo.Dependencies[i]); }
								catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
							}
						}
						string filename = FindMainFileName(startInfo.FileName);
						//Console.WriteLine(filename);
						try{ FileCopy(RootDirectory + filename, startInfo.FileName); }
						catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
						remoteProcess = new Process();
						ProcessStartInfo start = new ProcessStartInfo();
						start.FileName = RootDirectory + filename;
						start.Arguments = startInfo.Arguments;
						start.Verb = startInfo.Verb;
						remoteProcess.StartInfo = start;
						try
						{
							if(remoteProcess.Start())
							{
								if(Send((byte) eXception.NO) == -1) return -1;
								if(Send(1) == -1) return -1; // meaning started.
							}
							else 
							{
								if(Send((byte) eXception.NO) == -1) return -1;
								if(Send(0) == -1) return -1; // meaning not started.
							}
							return 0;
						}
						catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
					}
					else
					{
						Send((new ExceptionHandlingHeader(new ArgumentNullException("FileName can not be null."))).Buffer);
						return 0;
					}
				}
				else return 0;
			}
			else
			{
				try
				{
					if(remoteProcess.Start())
					{
						if(Send((byte) eXception.NO) == -1) return -1;
						if(Send(1) == -1) return -1; // meaning started.
					}
					else 
					{
						if(Send((byte) eXception.NO) == -1) return -1;
						if(Send(0) == -1) return -1; // meaning not started.
					}
					return 0;
				}
				catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
			}
		}
		//--------------------------------------------------------
		/// <summary>
		/// Immediately stops the associated process.
		/// </summary>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int Kill()
		{
			if(this.remoteProcess != null)
			{
				try 
				{ 
					if(!this.remoteProcess.HasExited)
					{
						this.remoteProcess.Kill();
						int timeout = 0; // timeout for process termination.
						while(true)
						{
							if(timeout > 1000*60) // 1 min timeout.
							{
								if(Send((new ExceptionHandlingHeader(new System.ComponentModel.Win32Exception(1, "The associated process could not be terminated."))).Buffer) == -1) return -1;
								return 0;
							}
							if(this.remoteProcess.HasExited)
								break;
							timeout++;
							System.Threading.Thread.Sleep(1);
						}
						if(!WithoutStartMethod)
							Directory.Delete(RootDirectory, true);
					}
					if(Send((byte) eXception.NO) == -1) return -1;
					return 0;
				}
				catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
			}
			else
			{
				if(Send((byte) eXception.NO) == -1) return -1;
				return 0;
			}
		}
		//--------------------------------------------------------
		/// <summary>
		/// Discards any information about the associated process that has been cached inside the process component.
		/// </summary>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int Refresh()
		{
			if(this.remoteProcess != null)
			{
				try 
				{ 
					this.remoteProcess.Refresh(); 
					if(Send((byte) eXception.NO) == -1) return -1;
					return 0;
				}
				catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
			}
			else
			{
				if(Send((byte) eXception.NO) == -1) return -1;
				return 0;
			}
		}
		//--------------------------------------------------------
		/// <summary>
		/// Sends RemoteProcessInfo of this DotRemoteProcess to client.
		/// </summary>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int get_ProcessInfo()
		{
			try
			{
				RemoteProcessInfo info = new RemoteProcessInfo(this.remoteProcess);
				if(Send((byte) eXception.NO) == -1) return -1;
				if(SendObject(info) == -1) return -1;
				return 0;
			}
			catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
		}
		//--------------------------------------------------------
		/// <summary>
		/// Sends exit time of this DotRemoteProcess to client.
		/// </summary>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int get_ExitTime()
		{
			try
			{
				DateTime exit = remoteProcess.ExitTime;
				if(Send((byte) eXception.NO) == -1) return -1;
				if(SendObject(exit) == -1) return -1;
				return 0;
			}
			catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
		}
		//--------------------------------------------------------
		/*/// <summary>
		/// Initialize a new DotRemoteProcess component, and associates it with the existing process resource that client specifies.
		/// </summary>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int GetRemoteProcessById()
		{
			try
			{
				object processId = ReceiveObject();
				if(processId == null)
					return -1;
				this.remoteProcess = Process.GetProcessById((int)processId);
				if(Send((byte) eXception.NO) == -1) return -1;
				return 0;
			}
			catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
		}*/
		//--------------------------------------------------------
		/// <summary>
		/// Sends an array of new RemoteProcessInfo components and associates them with existing process resources.
		/// </summary>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int GetRemoteProcesses()
		{
			try
			{
				Process[] processes = Process.GetProcesses();
				RemoteProcessInfo[] remoteProcessInfos = new RemoteProcessInfo[processes.Length];
				for(int i = 0 ; i < remoteProcessInfos.Length ; i++)
				{
					remoteProcessInfos[i] = new RemoteProcessInfo(processes[i]);
					processes[i].Close();
				}
				if(Send((byte) eXception.NO) == -1) return -1;
				if(SendObject(remoteProcessInfos) == -1) return -1;
				processes = null;
				remoteProcessInfos = null;
				return 0;
			}
			catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
		}
		//--------------------------------------------------------
		/// <summary>
		/// Immediately stops the associated process with the processId.
		/// </summary>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int KillById()
		{
			try
			{
				object processId = ReceiveObject();
				if(processId == null)
					return -1;
				Process[] processes = Process.GetProcesses();
				bool found = false;
				for(int i = 0 ; i < processes.Length ; i++)
					if(processes[i].Id == (int)processId)
					{
						processes[i].Kill();
						found = true;
					}
				for(int i = 0 ; i < processes.Length ; i++)
					processes[i].Close();
				if(found)
				{
					if(Send((byte) eXception.NO) == -1) return -1;
				}
				else
				{
					if(Send((new ExceptionHandlingHeader(new ArgumentException(String.Format("The process specified by the processId {0} parameter is not running. The identifier might be expired.", (int)processId)))).Buffer) == -1) return -1;
				}
				return 0;
			}
			catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
		}
		//--------------------------------------------------------
		/// <summary>
		/// Sends information of associated remote processes by the processId.
		/// </summary>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int GetRemoteProcessInfoById()
		{
			try
			{
				object processId = ReceiveObject();
				if(processId == null)
					return -1;
				Process[] processes = Process.GetProcesses();
				RemoteProcessInfo processInfo = null;
				for(int i = 0 ; i < processes.Length ; i++)
					if(processes[i].Id == (int)processId)
						processInfo = new RemoteProcessInfo(processes[i]);
				for(int i = 0 ; i < processes.Length ; i++)
					processes[i].Close();
				if(processInfo != null)
				{
					if(Send((byte) eXception.NO) == -1) return -1;
				}
				else
				{
					if(Send((new ExceptionHandlingHeader(new ArgumentException(String.Format("The process specified by the processId {0} parameter is not running. The identifier might be expired.", (int)processId)))).Buffer) == -1) return -1;
				}
				if(SendObject(processInfo) == -1) return -1;
				processInfo = null;
				return 0;
			}
			catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); return -1; }
		}
		//--------------------------------------------------------
		/// <summary>
		/// Gets the main filename in Windows or Linux file systems.
		/// </summary>
		/// <param name="pathFilename">The complete filename path.</param>
		/// <returns>The main file name,(for example: test.exe)</returns>
		private string FindMainFileName(string pathFilename)
		{
			string filename;
			int p = pathFilename.LastIndexOf(@"\");
			if(p < 0)
				p =  pathFilename.LastIndexOf("/");
			if(p < 0)
				filename = pathFilename;
			else filename = pathFilename.Substring(p + 1);
			return filename;
		}
		//------------------------------------DotGridThreadServer methods----------------------------------------------------//
		/// <summary>
		/// Start the requested client threads.
		/// </summary>
		private void ThreadStart()
		{
			AppDomainSetup setupInfo = new AppDomainSetup();
			setupInfo.ApplicationName = nc.UserName + "/" +info.GUID;
			setupInfo.ApplicationBase = RootDirectory;
			//setupInfo.PrivateBinPath = AppDomain.CurrentDomain.BaseDirectory;
			domain = AppDomain.CreateDomain(nc.UserName, null, setupInfo);
			ThreadExecutor executor = (ThreadExecutor) domain.CreateInstanceFromAndUnwrap(AppDomain.CurrentDomain.BaseDirectory + "DotGrid.dll", "DotGrid.DotThreading.ThreadExecutor");
			try 
			{ 
				Permissions per = null;
				/*per.Assertion = true;
				per.BindingRedirects = true;
				per.ControlAppDomain = true;
				per.ControlDomainPolicy = true;
				per.ControlEvidence = true;
				per.ControlPrincipal = true;
				per.ControlPolicy = true;
				per.ControlThread = true;
				per.Execution = true;
				per.Infrastructure = true;
				per.RemotingConfiguration = true;
				per.SerializationFormatter = true;
				per.SkipVerification = true;
				per.UnmanagedCode = true;*/
				byte[] result = executor.ExecuteThread(info.Start, info.Obj, ref this.IsAborted, per);
				if(isOccuredUnhandeldException) { ThreadExit(); return ; }
				if(Send((byte) eXception.NO) == -1) { ThreadExit(); return ; }
				byte[] buffer = new byte[4 + result.Length];  // Length + Object
				buffer[0] = (byte)((result.Length & 0xFF000000) >> 24);
				buffer[1] = (byte)((result.Length & 0x00FF0000) >> 16);
				buffer[2] = (byte)((result.Length & 0x0000FF00) >> 8);
				buffer[3] = (byte) (result.Length & 0x000000FF);
				Array.Copy(result, 0, buffer, 4, buffer.Length - 4);
				result = null;
				Write(buffer, 0, buffer.Length);
				//Send(buffer, 0, buffer.Length); 
				buffer = null;
				this.isEndedThreadExecution = true;
				return ;
			}
			catch(System.Security.SecurityException ee)
			{
				Send((new ExceptionHandlingHeader(ee)).Buffer); 
				this.isEndedThreadExecution = false;
				return ;
			}
			catch(Exception ee) 
			{
				Send((new ExceptionHandlingHeader(ee)).Buffer); 
				this.isEndedThreadExecution = false;
				return ;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Configure Assembly runtime environment for running requested remote thread.
		/// </summary>
		/// <param name="info">Information about remote thread request.</param>
		private void AssemblyConfigure(ThreadInfo info)
		{
			//if(!Directory.Exists(RootDirectory))
				Directory.CreateDirectory(RootDirectory);
			//if(!File.Exists(RootDirectory + "DotGrid.dll"))
				File.Copy(AppDomain.CurrentDomain.BaseDirectory + "DotGrid.dll", RootDirectory + "DotGrid.dll");
			for(int i = 0 ; i < info.Modules.Length ; i++)
			{
				string localFileName = RootDirectory + info.Modules[i].ScopeName;
				//if(!File.Exists(localFileName))
					FileCopy(localFileName, info.Modules[i].FullyQualifiedName);
				/*else
				{
					if(info.Modules[i].DateCreate != new FileInfo(localFileName).CreationTime)
					{
						Console.WriteLine("new copy");
						File.Delete(localFileName);
						FileCopy(localFileName, info.Modules[i].FullyQualifiedName);
					}
				}*/
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Copies an existing file to a new file. Overwriting a file of the same name is not allowed.
		/// </summary>
		/// <param name="localFileName">Local file name to copy.</param>
		/// <param name="remoteFileName">Reomte file name.</param>
		private void FileCopy(string localFileName, string remoteFileName)
		{
			DotDfsFileStream fs1 = new DotDfsFileStream(remoteFileName, FileMode.Open, FileAccess.Read, 
				FileShare.Read, PathEncoding.UTF8, FindClientAddress(), this.nc, false);

			//Console.WriteLine(fs1.Name);
			FileStream fs2 = new FileStream(localFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
			byte[] buffer = new byte[64*1024]; // default 128KB buffer size.
			int n = 0;
			while(true)
			{
				n = fs1.Read(buffer, 0, buffer.Length);
				if(n <= 0)
					break;
				fs2.Write(buffer, 0, n);
			}
			fs1.Close();
			fs2.Close();
			buffer = null;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the client IP address.
		/// </summary>
		/// <returns>Client IP Address.</returns>
		private string FindClientAddress()
		{
			return IPAddress.Parse(((IPEndPoint)this.client.RemoteEndPoint).Address.ToString()).ToString();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Exits current client connection and release all system resources.
		/// </summary>
		private void ThreadExit()
		{
			this.IsAborted = true;
			if(this.client != null)
			{
				try { this.client.Close(); }
				catch {}
			}
			if(this.reader != null)
			{
				try { this.reader.Close(); }
				catch {}
			}
			this.client = null;
			this.reader = null;
			this.writer = null;
			if(this.processorThread != null)
				if(!this.isEndedThreadExecution)
					this.processorThread.Abort();
			this.processorThread = null;
			this.thread = null;
			//--------------------------------
			if(this.domain != null)
			{
				try 
				{
					AppDomain.Unload(this.domain);
					CompleteDeleteDirectory(true);
				}
				catch{} // must log to DB
			}
			else
			{
				if(!IsStartedMainThread)
				{
					try { CompleteDeleteDirectory(true); }
					catch{}// must log to DB
				}
			}
			//--------------------------------
			if(remoteProcess != null)
			{
				try { remoteProcess.Close(); }
				catch{}// must log to DB
			}
			this.connections.Remove(this);
			GC.Collect();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Deletes all files and directories in the RootDirectory path.
		/// </summary>
		/// <param name="FromRemoteProcess">True, if server mode is RemoteProcess.</param>
		private void CompleteDeleteDirectory(bool FromRemoteProcess)
		{
			if(FromRemoteProcess)
			{
				string[] files = Directory.GetFiles(RootDirectory);
				foreach(string file in files)
					File.Delete(file);
			}
			Directory.Delete(RootDirectory, true);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Does Public Key Authentication protocol.
		/// </summary>
		/// <param name="ns">The network stream.</param>
		/// <returns>If PublicKeyAuthentication protocol opartions is done without error, true will be returned.</returns>
		private bool PublicKeyAuthentication(NetworkStream ns)
		{
			this.reader = new SecureBinaryReader(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			this.writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			this.BufferSize = 64 * 1024;
			RSAPublicHeader rph = new RSAPublicHeader(ServerRSA);
			if(Send(rph.Buffer) == -1)
				return false;
			byte[] buffer = Receive(4096);
			if(buffer == null) 
				return false;
			if(buffer.Length < 2 + 3 * 16) // Minimum { [(Secure,encryption)].Length + [len].Length + [(key,iv,md5hash)].Length }
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad RSAPublicHeader format."))).Buffer) == -1) 
					return false;
				return false;
			}
			switch((Encryption)(buffer[0] & 0x0F))
			{
				case Encryption.RIJNDAEL:
					//Console.WriteLine(Encryption.RIJNDAEL);//
					break;
					/*case Encryption.T3DES:
						*/
				default:
					if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not supported encryption algorithm."))).Buffer) == -1) 
						return false;
					return false;
			}
			switch((buffer[0] & 0xF0) >> 4)
			{
				case 0:
					this.secure = false;
					break;
				case 1:
					this.secure = true;
					break;
				default:
					if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not supported Secure field in RSAPublic header."))).Buffer) == -1) 
						return false;
					return false;
			}
			int len = (int)buffer[1];
			byte[] temp = new byte[len];
			Array.Copy(buffer, 2, temp, 0, len);
			try { temp = ServerRSA.DecryptData(temp); }
			catch
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad format for RSA inputs."))).Buffer) == -1) 
					return false;
				return false;
			}
			if(temp.Length != 3 * 16)
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad format for RSA inputs."))).Buffer) == -1) 
					return false;
				return false;
			}
			byte[] KeyIv = new byte[2 * 16];
			byte[] hash = new byte[16];
			Array.Copy(temp, 0, KeyIv, 0, KeyIv.Length);
			Array.Copy(temp, KeyIv.Length, hash, 0, hash.Length);
			byte[] newHash = new MD5().MD5hash(KeyIv);
			for(int i = 0 ; i < newHash.Length ; i++)
				if(newHash[i] != hash[i])
				{
					if(Send((new ExceptionHandlingHeader(new System.Security.SecurityException("The hash with the data is wrong."))).Buffer) == -1) 
						return false;
					return false;
				}
			hash = temp = buffer = newHash = null;
			byte[] key = new byte[16];
			byte[] iv = new byte[16];
			Array.Copy(KeyIv, 0, key, 0, key.Length);
			Array.Copy(KeyIv, key.Length, iv, 0, iv.Length);
			rijndael = new RijndaelEncryption(key, iv);
			key = iv = null;
			if(Send((byte)eXception.NO) == -1) 
				return false;
			return true;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Authenticates the user in login state.
		/// </summary>
		/// <param name="ns">The network stream.</param>
		/// <returns>If uer is authorized, true will return.</returns>
		private bool ClientAuthentication(NetworkStream ns)
		{
			this.reader = new SecureBinaryReader(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			this.writer = new SecureBinaryWriter(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			BufferSize = 32 * 1024;
			byte[] buffer = Receive(4096);
			if(buffer == null)
				return false;
			if(buffer == null || buffer.Length < 2)
			{
				SendErrorMessage((byte)DotGrid.Shared.Enums.ClientAuthenticationError.BAD);
				return false;
			}
			int n = buffer[0]; //length of username
			int m = buffer[1]; //length of passwword
			if(buffer.Length != 2 + n + m)
			{
				SendErrorMessage((byte)DotGrid.Shared.Enums.ClientAuthenticationError.BAD);
				return false;
			}
			string username = System.Text.ASCIIEncoding.ASCII.GetString(buffer, 2, n);
			string password = System.Text.ASCIIEncoding.ASCII.GetString(buffer, 2 + n , m);
			//Console.WriteLine("\n\nuser: {0}\npass: {1}", username, password);//
			if(username == this.nc.UserName && password == this.nc.Password)
			{
				if(SendErrorMessage((byte)DotGrid.Shared.Enums.ClientAuthenticationError.OK) == -1) 
					return false;
				return true;
			}
			else
			{
				SendErrorMessage((byte)DotGrid.Shared.Enums.ClientAuthenticationError.NO);
				return false;
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
				return null;
			if(buffer.Length != 4)
				throw new ArgumentOutOfRangeException("Bad format for Object Header."); 
			int size = (buffer[0] << 24) | (buffer[1]  << 16) | (buffer[2]  << 8) | buffer[3]; // Object Length
			if(size <= 0)
				throw new ArgumentOutOfRangeException("Bad format for Object Header."); 
			buffer = new byte[size];
			if(Read(buffer, buffer.Length) == -1) return null;
			return SerializeDeserialize.DeSerialize(buffer);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sends an object to network stream.
		/// </summary>
		/// <param name="obj">Favorite object for sending to network stream.</param>
		/// <returns>-1 for occured, 0 for no occured exception.</returns>
		private int SendObject(object obj)
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
				return -1;
			else return 0;
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
			if(count <= BufferSize)
			{
				return ReadInternal(array, 0, count);
			}
			else
			{
				int temp = 0;
				int sum = 0;
				int i = 0;
				int a = count / BufferSize;
				int q = count % BufferSize;
				while(true)
				{
					if((temp = ReadInternal(array, BufferSize*i, BufferSize)) == -1) 
					{ 
						return -1;
					}
					if(temp == 0)
						break;
					sum += temp;
					i++;
					if(q != 0 && i == a)
					{
						if((temp = ReadInternal(array, BufferSize*i, q)) == -1) 
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
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		private int Write(byte[] array, int offset, int count)
		{
			if(count <= BufferSize)
			{
				return Send(array, offset, count);
			}
			else
			{
				int i = 0;
				int a = array.Length / BufferSize;
				int q = array.Length % BufferSize;
				while(true)
				{
					if(Send(array, BufferSize*i, BufferSize) == -1)
						return -1;
					i++;
					if(q != 0 && i == a)
					{
						if(Send(array, BufferSize*i, q) == -1)
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
		/// Send error or success messages to dfsClient.
		/// </summary>
		/// <param name="error">Error code.</param>
		private int SendErrorMessage(byte error)
		{
			return Send(error);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a maximum of n bytes from the current network stream into buffer and return it.
		/// </summary>
		/// <param name="n">states n bytes for reading from network stream</param>
		/// <returns>Return received buffer data, if there arent no data then null will be returned.</returns>
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
		/// <param name="buffer">buffer for writting to network stream.</param>
		/// <returns>If any errors occured, -1 will be returned otherwise 0.</returns>
		private int Send(byte[] buffer)
		{
			try
			{
				this.writer.Write(buffer, 0, buffer.Length);
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
		/// <param name="buffer">buffer for writting to network stream.</param>
		/// <returns>If any errors occured, -1 will be returned otherwise 0.</returns>
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
		/// <param name="buffer">buffer for writting to network stream.</param>
		/// <param name="offset">The starting point in buffer at which to begin writing. </param>
		/// <param name="count">The number of bytes to write.</param>
		/// <returns>If any errors occured, -1 will be returned otherwise 0.</returns>
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