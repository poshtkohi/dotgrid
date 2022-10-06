using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Collections;

using DotGrid.Net;
using DotGrid.DotDfs;
using DotGrid.Shared.Headers.DotDFS;
using DotGrid.Shared.Enums.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for Session.
	/// </summary>
	internal class SessionClient
	{
		private FileStream fs;
		private Thread worker;
		private long written = 0;
		private ArrayList sockets;
		private FileTransferInfo info;
		private ArrayList qwrite;
		private int timeout = 30 * 1000; // 60s timeout
		private int MaxQueueWorkerSize = 20;
		private IComparer comparer; //
		long lastOffset = 0;
		long lastLength = 0;
		long j = 0;  // seek numbers
		long k = 0;  // total writes
		private bool secure = false;
		private bool exited = false;
		//**************************************************************************************************************//
		public SessionClient(DotGridSocket socket, FileTransferInfo transferInfo)
		{
			sockets = new ArrayList();
			sockets.Add(socket);
			info = transferInfo;
			if(info.ParallelSize > 1)
			{
				qwrite = new ArrayList();
				comparer = new QWriteCompare();
				if(info.ParallelSize == 10)
					MaxQueueWorkerSize = 10;
				if(info.ParallelSize < 10)
					MaxQueueWorkerSize /= 2;
				if(info.FileSize <= info.ParallelSize * MaxQueueWorkerSize * info.BufferSize)
					MaxQueueWorkerSize = 1;
			}
			this.secure = socket.IsSecure;
			//Console.WriteLine(secure);
			worker = new Thread(new ThreadStart(WorkerProc));
			worker.Start();
			//Console.WriteLine("max {0}", MaxQueueWorkerSize);
		}
		//**************************************************************************************************************//
		public long DiskSeekNumbers
		{
			get
			{
				return this.j;
			}
		}
		//**************************************************************************************************************//
		public long TotalDiskWrites
		{
			get
			{
				return this.k;
			}
		}
		//**************************************************************************************************************//
		private void WorkerProc()
		{
			if(WaitForAllConnections() == -1) { WorkerExit(); throw new Exception("Connection timeout."); }
			try { OpenFileHandle(); }  catch(Exception e) { WorkerExit(); throw e; }
			while(true)
			{
				if(sockets.Count == 0 || written >= info.FileSize)
					break;
				if(info.ParallelSize == 1)
				{
					DotGridSocket sock = (DotGridSocket)sockets[0];
					FileTransferModeHearderInfo infoRead = null;
					try 
					{ 
						if(info.Mode == TransferMode.DotDFS)
							infoRead = ReadFileBlock(sock);
						else
							infoRead = ReadFileBlockGridFTPMode(sock);
					}
					catch(ObjectDisposedException e) { WorkerExit(); throw e; }
					catch(Exception e) { SendExceptionToOneSocketAndClose(e, sock) ; continue; }
					if(infoRead == null) { RemoveSocketFromArrayList(sock); continue; /*break*/ } // end file block transfer from a client stream connection
					try
					{
						if(infoRead == null) 
						{
							WriteToFile(null);
							RemoveSocketFromArrayList(sock); continue;
						} // end file block transfer from a client stream connection
						else WriteToFile(infoRead);
					}
					catch(Exception e) { WorkerExit(); throw e; }
					//WriteNoException(sock);
				}
				else
				{
					ArrayList alRead = MakeReadSocketArrayList();
					if(alRead == null)
						break;
					try { Socket.Select(alRead, null, null, 0/*alRead.Count*/); }
					catch { }
					for(int i = 0 ; i < alRead.Count ; i++)
					{
						//Console.WriteLine("hell");
						Socket s = (Socket)alRead[i];
						int index = FindSocketIndex(s);
						if(index == -1) { s.Close(); continue; }
						DotGridSocket sock = (DotGridSocket)sockets[index];
						FileTransferModeHearderInfo infoRead = null;
						try 
						{ 
							if(info.Mode == TransferMode.DotDFS)
								infoRead = ReadFileBlock(sock); 
							else
								infoRead = ReadFileBlockGridFTPMode(sock); 
						}
						catch(ObjectDisposedException e) { WorkerExit(); throw e; }
						catch(Exception e) { WorkerExit(); throw e;  }
						if(infoRead == null) { RemoveSocketFromArrayList(sock); continue; /*break*/ } // end file block transfer from a client stream connection
						try
						{
							if(infoRead == null) 
							{
								WriteToFile(null);
								RemoveSocketFromArrayList(sock); continue;
							} // end file block transfer from a client stream connection
							else WriteToFile(infoRead);
							/*k++;
								if(infoRead.SeekValue != lastOffset + lastLength)
								{
									//Console.WriteLine("h");
									fs.Seek(infoRead.SeekValue, SeekOrigin.Begin);
									fs.Write(infoRead.Data, 0, infoRead.Data.Length);
									j++;
								}
								else fs.Write(infoRead.Data, 0, infoRead.Data.Length);
								lastOffset = infoRead.SeekValue;
								lastLength = infoRead.Data.Length;
								written += infoRead.Data.Length;
								//fs.Flush();*/
						}
						catch(Exception e) { WorkerExit(); throw e;  }
						WriteNoException(sock);
					}
				}
			}
			Console.WriteLine("Seek Number: {0}, Total Writes: {1}", j , k);
			WorkerExit();
			return;
		}
		//**************************************************************************************************************//
		private void WriteToFile(FileTransferModeHearderInfo infoRead)
		{
			if(info.ParallelSize == 1 || MaxQueueWorkerSize == 1)
			{
				if(infoRead == null)
					return;
				k++;
				if(infoRead.SeekValue != lastOffset + lastLength)
				{
					fs.Seek(infoRead.SeekValue, SeekOrigin.Begin);
					fs.Write(infoRead.Data, 0, infoRead.Data.Length);
					j++;
				}
				else fs.Write(infoRead.Data, 0, infoRead.Data.Length);
				lastOffset = infoRead.SeekValue;
				lastLength = infoRead.Data.Length;
				written += infoRead.Data.Length;
			}
			else
			{
				//Console.WriteLine("hel");
				if(infoRead != null)
				{
					qwrite.Add(infoRead);
					if(qwrite.Count == MaxQueueWorkerSize)
					{
						//Console.WriteLine(qwrite.Count);
						qwrite.Sort(comparer);
						for(int v = 0 ; v < qwrite.Count ; v++)
						{
							k++;
							FileTransferModeHearderInfo temp = (FileTransferModeHearderInfo)qwrite[v];
							if(temp.SeekValue != lastOffset + lastLength)
							{
								fs.Seek(temp.SeekValue, SeekOrigin.Begin);
								fs.Write(temp.Data, 0, temp.Data.Length);
								j++;
							}
							else fs.Write(temp.Data, 0, temp.Data.Length);
							lastOffset = temp.SeekValue;
							lastLength = temp.Data.Length;
							written += temp.Data.Length;
						}
						qwrite.Clear();
					}
					//else qwrite.Add(infoRead);
				}
				else
				{
					if(qwrite.Count != 0)
					{
						qwrite.Sort(comparer);
						for(int v = 0 ; v < qwrite.Count ; v++)
						{
							k++;
							FileTransferModeHearderInfo temp = (FileTransferModeHearderInfo)qwrite[v];
							if(temp.SeekValue != lastOffset + lastLength)
							{
								fs.Seek(temp.SeekValue, SeekOrigin.Begin);
								fs.Write(temp.Data, 0, temp.Data.Length);
								j++;
							}
							else fs.Write(temp.Data, 0, temp.Data.Length);
							lastOffset = temp.SeekValue;
							lastLength = temp.Data.Length;
							written += temp.Data.Length;
						}
						qwrite.Clear();
					}
				}
			}
		}
		//**************************************************************************************************************//
		private FileTransferModeHearderInfo ReadFileBlock(DotGridSocket socket)
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
		}
		//**************************************************************************************************************//
		private FileTransferModeHearderInfo ReadFileBlockGridFTPMode(DotGridSocket socket)
		{
			byte b = socket.ReadByte();
			if(b == 0)
				return null; // meaning end read.
			byte[] val1 = socket.Read(8);
			byte[] val2 = socket.Read(8);
			if(val1.Length != 8 && val2.Length != (int)8)
				throw new ArgumentException("Bad format for FileTransferModeHeader.");
			long seekValue = (long)LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val1);
			long readValue = (long)LongValueHeader.GetLongNumberFromBytesForGridFTPMode(val2);
			byte[] buffer = new byte[readValue];
			int n = socket.Read(buffer, buffer.Length);
			if(n != buffer.Length)
				throw new ArgumentException("Bad format for FileTransferModeHeader.");
			return new FileTransferModeHearderInfo(seekValue, buffer);
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
		}
		//**************************************************************************************************************//
		private void SendExceptionToOneSocketAndClose(Exception e, DotGridSocket socket)
		{

			try { socket.WriteException(e); RemoveSocketFromArrayList(socket); }
			catch { RemoveSocketFromArrayList(socket); }
		}
		//**************************************************************************************************************//
		private ArrayList MakeReadSocketArrayList()
		{
			if(sockets.Count == 0)
				return null;
			DotGridSocket[] socks = (DotGridSocket[])this.sockets.ToArray(typeof(DotGridSocket));
			ArrayList alRead = new ArrayList();
			for(int i = 0 ; i < sockets.Count ; i++)
				alRead.Add(socks[i].BaseSocket);
			//socks = null;
			return alRead;
			/*ArrayList alRead = new ArrayList();
			for(int i = 0 ; i < sockets.Count ; i++)
				alRead.Add(((DotGridSocket)sockets[i]).BaseSocket);
			return alRead;*/
		}
		//**************************************************************************************************************//
		private void RemoveSocketFromArrayList(DotGridSocket socket)
		{
			int i = FindSocketIndex(socket.BaseSocket);
			if(i != -1)
			{
				((DotGridSocket)sockets[i]).Close();
				sockets.RemoveAt(i);
			}
		}
		//**************************************************************************************************************//
		private void OpenFileHandle()
		{
			fs = new FileStream(info.WriteFileName, FileMode.Create, FileAccess.Write, FileShare.None);
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
		private void Close()
		{
			if(!exited)
			{
				worker.Abort();
				WorkerExit();
			}
		}
		//**************************************************************************************************************//
		private void WorkerExit()
		{
			exited = true;
			try
			{
				if(sockets.Count != 0)
					for(int i = 0 ; i < sockets.Count ; i++)
						((DotGridSocket)sockets[i]).Close();
				if(sockets.Count != 0) 
					sockets.Clear();
				if(qwrite != null)
					qwrite.Clear();
				if(fs != null)
					fs.Close();
				sockets = null;
				qwrite = null;
				sockets = null;
				fs = null;
				worker = null;
			}
			catch { }
			GC.Collect();
			Console.WriteLine("WorkerExit()"); //
		}
		//**************************************************************************************************************//
	}
}