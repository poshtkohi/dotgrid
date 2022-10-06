/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/


using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Collections;

using DotGrid.Net;
using DotGrid.DotDfs;
using DotGrid.Shared.Headers.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// This class implements the DotDFS protocol interpretor for Upload Request from client side in FTSM mode. 
	/// </summary>
	internal class SessionClientUploadRequest
	{
		private FileStream fs;
		//private Thread worker;
		private long written = 0;
		private ArrayList sockets;
		private Hashtable sessions;
		private FileTransferInfo info;
		private int timeout = 30 * 1000; // 60s timeout
		long lastOffset = 0;
		int lastLength = 0;
		int j = 0;  // seek numbers
		int k = 0;  // total writes
		private bool secure = false;
		private bool memmoryToMemoryTests = false;
		private byte[] buffer;//
		private byte[] val1;//
		private byte[] val2;//
		private DotGridSocket sock;//
		private int n; //
		private ArrayList connections;
		private ArrayList alRead;
		//private int _Available = 0;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of SessionClientUploadRequest class.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="Sessions"></param>
		/// <param name="transferInfo"></param>
		/// <param name="connections"></param>
		public SessionClientUploadRequest(DotGridSocket socket, ref Hashtable Sessions, FileTransferInfo transferInfo, ref ArrayList connections)
		{
			if(sessions == null)
				sessions = Sessions;
			sessions.Add(transferInfo.GUID, this);
			if(sockets == null)
				sockets = new ArrayList();
			sockets.Add(socket);
			info = transferInfo;
			if(this.connections == null)
				this.connections = connections;
			/*if(info.ParallelSize > 1)
			{
				qwrite = new ArrayList();
				comparer = new QWriteCompare();
				if(info.ParallelSize == 10)
					MaxQueueWorkerSize = 10;
				if(info.ParallelSize < 10)
					MaxQueueWorkerSize /= 2;
				if(info.FileSize <= info.ParallelSize * MaxQueueWorkerSize * info.tcpBufferSize)
					MaxQueueWorkerSize = 1;
			}*/
			if(info.WriteFileName.ToLower().Trim().IndexOf("/dev/null", 0) >= 0) // for memroy-to-memory tests
				memmoryToMemoryTests = true;
			else memmoryToMemoryTests = false;
			memmoryToMemoryTests = memmoryToMemoryTests; //
			//Console.WriteLine("memmoryToMemoryTests: "  + memmoryToMemoryTests); //
			this.secure = socket.IsSecure;
			//Console.WriteLine(secure);
			//worker = new Thread(new ThreadStart(WorkerProc));
			//worker.Start();
			buffer = new byte[info.tcpBufferSize];//
			val1 = new byte[8];//
			val2 = new byte[8];//
			//socket.BaseSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, info.tcpBufferSize);//
			socket.BaseSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, info.tcpBufferSize);//

			Console.WriteLine("DotDfs Server Revision 1.4");
			//Console.WriteLine("max {0}", MaxQueueWorkerSize);
			Console.WriteLine("tcpBufferSize {0}", info.tcpBufferSize);
		}
		//**************************************************************************************************************//
		public void Run()
		{
			Console.WriteLine("---------------------------------------------------------");
			Console.WriteLine("New DotDFS file transfer session.");
			if(WaitForAllConnections() == -1) { WorkerExit(); return ; }
			/*if(this.secure)   //secure session management
			{
				Console.WriteLine("secure session");
				SecureQueueWrite qwrite;
				try { qwrite = new SecureQueueWrite(info); } 
				catch(Exception e) { SendOneExceptionToAllSocketsAndClose(e); WorkerExit(); return; }
				secureWorkers = new SecureWorkerProc[info.ParallelSize];
				for(int i = 0 ; i < secureWorkers.Length ; i++)
					secureWorkers[i] = new SecureWorkerProc(ref qwrite, (DotGridSocket)sockets[i], info.Mode);
				for(int i = 0 ; i < secureWorkers.Length ; i++)
					secureWorkers[i].Run();
				int workerExited = 0;
				while(true)
				{
					if(workerExited == info.ParallelSize)
						break;
					for(int i = 0 ; i < secureWorkers.Length ; i++)
					{
						if(secureWorkers[i] != null)
						{
							if(secureWorkers[i].IsExited)
							{
								workerExited++;
								secureWorkers[i].Close();
								secureWorkers[i] = null;
							}
						}
					}
					Thread.Sleep(1);
				}
				qwrite.Close();
				sockets.Clear();
				WorkerExit();
				return ;
			}
			else //non-secure session management
			{
				Console.WriteLine("non-secure session");*/
			//Console.WriteLine("hi: " + sockets.Count);
			if(OpenFileHandle() == -1) { WorkerExit(); return ; }//Console.WriteLine(info.FileSize);
			long seekValue = 0;
			int readValue = 0;
			Socket s;
			alRead = new ArrayList();
			//Console.WriteLine("sockets: "  + sockets.Count);//
			//Console.WriteLine("file opened");
			while(true)
			{
				if(sockets.Count == 0 /*|| written >= info.FileSize*/)
					break;
				if(info.ParallelSize == 1)
				{
					sock = (DotGridSocket)sockets[0];
					try 
					{ 
						if(ReadFileBlockGridFTPMode(sock, ref seekValue, ref readValue))
						{ 
							RemoveSocketFromArrayList(sock); 
							break; 
						}
					}
					catch(ObjectDisposedException) { WorkerExit(); return; }
					catch(Exception e) { SendExceptionToOneSocketAndClose(e, sock) ; goto End; }
					try { WriteToFile(seekValue, readValue); }
					catch(Exception e) { SendExceptionToOneSocketAndClose(e, sock) ; goto End; }
					try { sock.WriteNoException(); }
					catch { WorkerExit(); return; }
				}
				else
				{
					if(sockets.Count == 0)
						break;
					MakeReadSocketArrayList();

					try { Socket.Select(alRead, null, null, 0); }
					catch { goto End; }
					//Socket.Select(alRead, null, null, alRead.Count);
					for(int i = 0 ; i < alRead.Count ; i++)
					{

						s = (Socket)alRead[i];
						int index = FindSocketIndex(s);
						if(index == -1) { s.Close(); continue; }
						sock = (DotGridSocket)sockets[index];
						//Console.WriteLine("count " + alRead.Count);
						try 
						{ 
							if(ReadFileBlockGridFTPMode(sock, ref seekValue, ref readValue))
							{
								RemoveSocketFromArrayList(sock); 
								//goto here;
								continue; 
							}
						}
						catch(ObjectDisposedException) { goto End;/*WorkerExit(); return;*/ }
						catch(Exception e) { SendExceptionToOneSocketAndClose(e, sock) ; continue; }
						try { WriteToFile(seekValue, readValue); }
						catch(Exception e) { SendExceptionToOneSocketAndClose(e, sock) ; continue; }
						try { sock.WriteNoException(); }
						catch { RemoveSocketFromArrayList(sock); continue;  }
					}
				}
				//Thread.Sleep(1);
			}
		End:
			Console.WriteLine("Seek Number: {0}, Total Writes: {1}", j , k);
			WorkerExit();
			return;
			//}
		}
		//**************************************************************************************************************//
		private void WriteToFile(long seekValue, int readValue)
		{
			//Console.WriteLine("offset: {0}, length: {1}", (int)seekValue, (int)readValue);
			k++;
			if(seekValue != lastOffset + lastLength)
			{
				fs.Seek(seekValue, SeekOrigin.Begin);
				fs.Write(buffer, 0, (int)readValue);
				j++;
			}
			else 
			{
				fs.Write(buffer, 0, (int)readValue);
				//for(int i = 0 ; i < (int) readValue; i++)
				//	Console.Write(buffer[i]);
			}
			lastOffset = seekValue;
			lastLength = readValue;
			written += readValue;
		}
		//**************************************************************************************************************//
		/*private FileTransferModeHearderInfo ReadFileBlockDotDFSMode(DotGridSocket socket)
		{
			LongMode _l1;
			LongMode _l2;
			byte b = socket.ReadByte();
			if(b == 0)
				return null; // meaning end read.
			_l1 = (LongMode)(b & 0x0F);
			_l2 = (LongMode)((b & 0xF0) >> 4);
			if(!Enum.IsDefined(typeof(LongMode), _l1) && !Enum.IsDefined(typeof(LongMode), _l2))
				throw new ArgumentOutOfRangeException("Not supported LongMode for FileTransferModeHeader.");
			byte[] val1 = socket.Read((int)_l1);
			byte[] val2 = socket.Read((int)_l2);
			if(val1.Length != (int)_l1 && val2.Length != (int)_l2)
				throw new ArgumentException("Bad format for FileTransferModeHeader.");
			long seekValue = (long)LongValueHeader.GetLongNumberFromBytes(val1);
			long readValue = (long)LongValueHeader.GetLongNumberFromBytes(val2);
			byte[] buffer = new byte[readValue];
			int n = socket.Read(buffer, buffer.Length);
			if(n != buffer.Length)
				throw new ArgumentException("Bad format for FileTransferModeHeader.");
			return new FileTransferModeHearderInfo(seekValue, buffer);
		}*/
		//**************************************************************************************************************//
		private bool ReadFileBlockGridFTPMode(DotGridSocket socket, ref long seekValue, ref int readValue)
		{
			if(socket.IsSecure)
			{
				if(socket.ReadByte() == 0)
					return true; // meaning end read.
				if(socket.Read(val1, val1.Length) != val1.Length)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				if(socket.Read(val2, val2.Length) != val2.Length)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				seekValue = (long)LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val1);
				readValue = (int)LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val2);
				if(readValue > info.tcpBufferSize)
					throw new ArgumentOutOfRangeException(string.Format("readValue in the header can not be greater than {0}.", info.tcpBufferSize));
				//byte[] buffer = new byte[readValue];
				n = socket.Read(buffer, readValue);
				if(n != readValue)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				return false;
			}
			else
			{
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
				readValue = (int)LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val2);
				if(readValue > info.tcpBufferSize)
					throw new ArgumentOutOfRangeException(string.Format("readValue in the header can not be greater than {0}.", info.tcpBufferSize));
				//byte[] buffer = new byte[readValue];
				n = ReadFromOriginialSocket(socket.BaseSocket, buffer, 0, readValue);
				if(n != readValue)
					throw new ArgumentException("Bad format for FileTransferModeHeader.");
				return false;
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
		public void AddNewClientStream(DotGridSocket socket)
		{
			if(socket.BaseSocket != null)
				sockets.Add(socket);
		}
		//**************************************************************************************************************//
		private void WriteNoException(DotGridSocket socket)
		{
			try { socket.WriteNoException(); }
			catch { RemoveSocketFromArrayList(socket); }
		}
		//**************************************************************************************************************//
		private void SendOneExceptionToAllSocketsAndClose(Exception e)
		{
			DotGridSocket[] socks = (DotGridSocket[])this.sockets.ToArray(typeof(DotGridSocket));
			for(int i = 0 ; i < socks.Length ; i++)
			{
				try { socks[i].WriteException(e); RemoveSocketFromArrayList(socks[i]); }
				catch { RemoveSocketFromArrayList(socks[i]); }
			}
			for(int j = 0 ; j < socks.Length ; j++)
			{
				for(int i = 0 ; i < connections.Count ; i++)
				{
					Socket temp = (Socket)(((object[])connections[i])[1]);
					if(temp != null)
						if(temp == socks[j].BaseSocket)
						{
							connections.Remove(connections[i]);
							break;
						}
				}
			}
		}
		//**************************************************************************************************************//
		private void SendExceptionToOneSocketAndClose(Exception e, DotGridSocket socket)
		{

			try { socket.WriteException(e); RemoveSocketFromArrayList(socket); }
			catch { RemoveSocketFromArrayList(socket); }
		}
		//**************************************************************************************************************//
		private void MakeReadSocketArrayList()
		{
			if(sockets.Count == 0)
			{
				sockets.Clear();
			    return ;
			}
			//DotGridSocket[] socks = (DotGridSocket[])this.sockets.ToArray(typeof(DotGridSocket));
			alRead.Clear();
			for(int i = 0 ; i < sockets.Count ; i++)
			{
				/*try
				{*/
					//_Available = socks[i].BaseSocket.Available; 
					alRead.Add(((DotGridSocket)sockets[i]).BaseSocket);
				/*}
				catch
				{
					RemoveSocketFromArrayList(socks[i]);
				}*/
			}
			//socks = null;
			//return alRead;
		}
		//**************************************************************************************************************//
		private void RemoveSocketFromArrayList(DotGridSocket socket)
		{
			int i = FindSocketIndex(socket.BaseSocket);
			if(i != -1)
			{
				((DotGridSocket)sockets[i]).Close();
				sockets.RemoveAt(i);
				for(int j = 0 ; j < connections.Count ; j++)
				{
					Socket temp = (Socket)(((object[])connections[j])[1]);
					if(temp != null)
						if(temp == socket.BaseSocket)
						{
							connections.Remove(connections[j]);
							break;
						}
				}
			}
		}
		//**************************************************************************************************************//
		private int OpenFileHandle()
		{
			try 
			{
				int p = info.WriteFileName.LastIndexOf("/");
				if(p < 0)
					p = info.WriteFileName.LastIndexOf("\\");
				if(p > 0)
				{
					string directory = info.WriteFileName.Substring(0, p);
					if(!Directory.Exists(directory))
						Directory.CreateDirectory(directory);
				}
				//fs = new FileStream(info.WriteFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, DotGrid.Constants.blockSize); 
				fs = new FileStream(info.WriteFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None); 
				if(fs.Length != info.FileSize)
				{
					fs.SetLength(info.FileSize);
					fs.Seek(0, SeekOrigin.Begin);
				}
				/*fs.SetLength(0);
				fs.Seek(0, SeekOrigin.Begin);
				fs.SetLength(info.FileSize);
				fs.Seek(0, SeekOrigin.Begin);*/
				return 0; 
			}
			catch(Exception e)
			{
				for(int i = 0 ; i < sockets.Count ; i++)
				{
					try { ((DotGridSocket)sockets[i]).WriteException(e); }
					catch { }
				}
				return -1;
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
		private int WaitForAllConnections()
		{
			int _timeout = 0;
			while(true)
			{
				_timeout++;
				if(sockets.Count == info.ParallelSize)
					return 0;
				if(_timeout == this.timeout)
					return -1;
				Thread.Sleep(1);
			}
		}
		//**************************************************************************************************************//
		private void WorkerExit()
		{
			try
			{
				for(int j = 0 ; j < sockets.Count ; j++)
				{
					for(int i = 0 ; i < connections.Count ; i++)
					{
						Socket temp = (Socket)(((object[])connections[i])[1]);
						if(temp != null)
							if(temp == ((DotGridSocket)sockets[j]).BaseSocket)
							{
								connections.Remove(connections[i]);
								break;
							}
					}
				}
				if(sockets.Count != 0)
					for(int i = 0 ; i < sockets.Count ; i++)
						((DotGridSocket)sockets[i]).Close();
				if(sockets.Count != 0) 
					sockets.Clear();
				if(fs != null)
					fs.Close();
				sessions.Remove(info.GUID);
				alRead.Clear();
				alRead = null;
				sockets = null;
				fs = null;
				info = null;
				//worker = null;
				val1 = val2 = buffer = null;//
			}
			catch { }
			Console.WriteLine("End session."); //
			GC.Collect();
		}
		//**************************************************************************************************************//
	}
}