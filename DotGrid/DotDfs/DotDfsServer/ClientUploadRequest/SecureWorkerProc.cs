/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Threading; 

using DotGrid.Net;
using DotGrid.DotDfs;

namespace DotDfs.FileTransferServer
{
	/// <summary>
	/// Summary description for SecureThreadProc.
	/// </summary>
	public class SecureWorkerProc
	{
		private DotGridSocket socket;
		private TransferMode mode;
		private SecureQueueWrite qwrite;
		private bool exited = false;
		private bool closed = false; 
		private Thread worker;
		private int timeout = 10 * 1000; // 10s timeout;
		//**************************************************************************************************************//
		public SecureWorkerProc(ref SecureQueueWrite qwrite, DotGridSocket sock, TransferMode mode)
		{
			this.mode = mode;
			this.socket = sock;
			this.qwrite = qwrite;
			worker = new Thread(new ThreadStart(WorkerProc));
		}
		//**************************************************************************************************************//
		private void WorkerProc()
		{
			while(true)
			{
				if(closed)
					break;
				try
				{
					FileTransferModeHearderInfo infoRead;
					if(mode == TransferMode.DotDFS) 
						infoRead = ReadFileBlock();
					else
						infoRead = ReadFileBlockGridFTPMode();
					if(infoRead == null) 
					{
						qwrite.WriteToFile(null);
						break;
					} // end file block transfer from a client stream connection
					else qwrite.WriteToFile(infoRead);
				}
				catch(ObjectDisposedException) { break; }
				catch(Exception e) { try { socket.WriteException(e); } catch { } break; }
				try { socket.WriteNoException(); } catch { break ; }
			}
			exited = true;
		}
		//**************************************************************************************************************//
		public void Run()
		{
			if(worker == null)
				throw new ObjectDisposedException("could not run the disposed worker object.");
			else 
				worker.Start();
		}
		//**************************************************************************************************************//
		public bool IsExited
		{
			get 
			{
				return exited;
			}
		}
		//**************************************************************************************************************//
		public void Close()
		{
			closed = true;
			int _timeout = 0;
			while(!exited)
			{
				if(_timeout >= timeout)
				{
					try { worker.Abort(); }  catch { }
					break;
				}
				_timeout++;
				Thread.Sleep(1);
			}
			try { if(socket.BaseSocket != null) socket.Close(); } catch { }
		}
		//**************************************************************************************************************//
		private FileTransferModeHearderInfo ReadFileBlock()
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
		private FileTransferModeHearderInfo ReadFileBlockGridFTPMode()
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
	}
}
