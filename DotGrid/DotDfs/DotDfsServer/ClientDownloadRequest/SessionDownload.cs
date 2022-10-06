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
using DotGrid.Shared.Enums;
using DotGrid.Shared.Headers.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for Session.
	/// </summary>
	internal class SessionClientDownloadRequest
	{
		private FileStream fs;
		//private Thread worker;
		private long written = 0;
		private ArrayList sockets;
		private Hashtable sessions;
		private FileTransferInfo info;
		private int timeout = 30 * 1000; // 60s timeout
		int k = 0;  // total reads
		private bool secure = false;
		private bool memmoryToMemoryTests = false;
		private byte[] buffer;//
		private byte[] val1;//
		private byte[] val2;//
		private DotGridSocket sock;//
		private int n; //
		private ArrayList connections;
		private ArrayList alWrite;
		//private int _Available;
		//private bool _IsSendLastFileBlock = false;
		//**************************************************************************************************************//
		public SessionClientDownloadRequest(DotGridSocket socket, ref Hashtable Sessions, FileTransferInfo transferInfo, ref ArrayList connections)
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
			if(info.WriteFileName.ToLower().Trim().IndexOf("/dev/zero", 0) >= 0) // for memroy-to-memory tests
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
			socket.BaseSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, info.tcpBufferSize);//
			socket.BaseSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, info.tcpBufferSize);//
			//Console.WriteLine("max {0}", MaxQueueWorkerSize);
		}
		//**************************************************************************************************************//
		public void Run()
		{
			Console.WriteLine("---------------------------------------------------------");
			Console.WriteLine("New DotDFS file transfer session.");
			if(WaitForAllConnections() == -1) { WorkerExit(); return ; }
			if(OpenFileHandle() == -1) { WorkerExit(); return ; }
			long offsetSeek = 0;
			/*if(info.ParallelSize == 1)
			{
				sock = (DotGridSocket)sockets[0];
				Console.WriteLine("bufferSize: " + info.tcpBufferSize);//
				while(true)
				{
					try { ReadFromFile(ref buffer, ref offsetSeek); }
					catch(Exception e){ SendExceptionToOneSocketAndClose(e, sock) ; continue; }
					WriteNoException(sock);
					if(!sock.BaseSocket.Connected)
						goto End;
					if(offsetSeek == -1)
					{
						try { sock.WriteNoException(); sock.WriteByte(0); goto End; } 
						catch { goto End; }
					}
					if(!sock.BaseSocket.Connected)
						goto End;
					if(WriteFileBlockGridFTPMode(sock, offsetSeek, buffer) == -1)
						break;
				}
			}
			else
			{*/
			//Console.WriteLine("s: " + this.sockets.Count);
				Socket s;
				alWrite = new ArrayList();
				while(true)
				{
					//Console.WriteLine("ssss: " + this.sockets.Count);
					if(sockets.Count == 0/* || written >= fs.Length*/)
						break;
					MakeReadSocketArrayList();

					//Console.WriteLine("hi: " + this.sockets.Count);
					/*if(alWrite.Count == 0)
						break;*/
					//Console.WriteLine("alWrite_last: " + this.alWrite.Count);
					//try { DotGridSocket.Select(null, alWrite, null, 0); }
					try { Socket.Select(null, alWrite, null, 0); }
					catch { goto End; }
					//Console.WriteLine("alWrite: " + this.alWrite.Count);
					//Socket.Select(null, alWrite, null, 0);
					for(int i = 0 ; i < alWrite.Count ; i++)
					{
						//Console.WriteLine(i);
						/*if(sockets.Count == 1)
							sock = (DotGridSocket)sockets[0];
						else
						{*/
							s = (Socket)alWrite[i];
							int index = FindSocketIndex(s);
							if(index == -1) { s.Close(); continue; }
							sock = (DotGridSocket)sockets[index];
						//}
						if(!sock.BaseSocket.Connected)
							goto End;
						try { ReadFromFile(ref buffer, ref offsetSeek); }
						catch(Exception e){ SendExceptionToOneSocketAndClose(e, sock) ; continue; }
						if(!sock.BaseSocket.Connected)
							goto End;
						if(offsetSeek == -1)
						{
							try 
							{ 
								/*sock.BaseStream.WriteByte(1);
									sock.BaseStream.WriteByte(0);
									goto End;*/
								sock.WriteNoException(); 
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
					Thread.Sleep(1);
				}
			//}
			End:

			/*while(true)
			{
				Console.WriteLine(sockets.Count);
				if(sockets == null)
					break;
				if(sockets.Count == 0)
					break;

				DotGridSocket[] __socks = (DotGridSocket[])sockets.ToArray(typeof(DotGridSocket));

				for(int i = 0 ; i < __socks.Length ; i++)
				{
					__socks[i].BaseSocket.
					if(__socks[i] == null)
						RemoveSocketFromArrayList(__socks[i]);
					if(__socks[i].BaseSocket == null)
						RemoveSocketFromArrayList(__socks[i]);
					if(!__socks[i].BaseSocket.Connected)
						RemoveSocketFromArrayList(__socks[i]);
					try
					{
						_Available = __socks[i].BaseSocket.Available;
					}
					catch{RemoveSocketFromArrayList(__socks[i]);}
				}
				Thread.Sleep(1);
			}*/
			Console.WriteLine("Total Read: {0}", k);
			WorkerExit();
			return;
			//}
		}
		private static void Select(IList checkRead,IList checkWrite,IList checkError,int microSeconds)
		{
			if(checkRead != null)	
				SelectInternal(checkRead, SelectMode.SelectRead, microSeconds);
			if(checkWrite != null)	
				SelectInternal(checkWrite, SelectMode.SelectWrite, microSeconds);
			if(checkError != null)	
				SelectInternal(checkError, SelectMode.SelectError, microSeconds);
		}

		private static void SelectInternal(IList _IList, SelectMode mode, int microSeconds)
		{
			if(_IList != null)	
			{
				for(int  i = 0 ; i < _IList.Count ; i++)
				{
					if(!((Socket)_IList[i]).Poll(microSeconds, mode))
					{
						_IList.RemoveAt(i);
						i = 0;
					}
				}
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
					socket.WriteNoException();
					socket.WriteByte(1);// signaling the client about arrival of new file block.
					socket.Write(val1, 0, val1.Length);
					socket.Write(val2, 0, val2.Length);
					socket.Write(buffer, 0, buffer.Length);
					written += buffer.Length;
					//socket.CheckExceptionResponse();
				}
				else
				{
					socket.BaseStream.WriteByte((byte)eXception.NO);//
					//Console.WriteLine("length: " +  LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val2));//
					socket.BaseStream.WriteByte(1);// signaling the client about arrival of new file block.
					socket.BaseStream.Write(val1, 0, val1.Length);
					socket.BaseStream.Write(val2, 0, val2.Length);
					socket.BaseStream.Write(buffer, 0, buffer.Length);
					written += buffer.Length;
					//socket.CheckExceptionResponse();
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
				return ;
			//DotGridSocket[] socks = (DotGridSocket[])this.sockets.ToArray(typeof(DotGridSocket));
			if(alWrite.Count != 0)
				alWrite.Clear();
			for(int i = 0 ; i < sockets.Count ; i++)
			{
				try
				{
					//_Available = socks[i].BaseSocket.Available; 
					alWrite.Add(((DotGridSocket)sockets[i]).BaseSocket);
				}
				catch
				{
					RemoveSocketFromArrayList((DotGridSocket)sockets[i]);
				}
			}
			//socks = null;
		}
		//**************************************************************************************************************//
		private void RemoveSocketFromArrayList(DotGridSocket socket)
		{
			int i = FindSocketIndex(socket.BaseSocket);//Console.WriteLine("sock index: " + i);
			if(i != -1)
			{
				try{((DotGridSocket)sockets[i]).Close();} catch{}
				Socket temp;
				sockets.RemoveAt(i);
				for(int j = 0 ; j < connections.Count ; j++)
				{
					temp = (Socket)(((object[])connections[j])[1]);
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
				fs = new FileStream(info.WriteFileName, FileMode.Open, FileAccess.Read, FileShare.None, DotGrid.Constants.blockSize); 
				//fs = new FileStream(info.WriteFileName, FileMode.Open, FileAccess.Read, FileShare.None); 
				/*if(fs.Length != info.FileSize)
				{
					fs.SetLength(info.FileSize);
					fs.Seek(0, SeekOrigin.Begin);
				}*/
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
				if(alWrite != null)
					alWrite.Clear();
				alWrite = null;
				sessions.Remove(info.GUID);
				sockets = null;
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
