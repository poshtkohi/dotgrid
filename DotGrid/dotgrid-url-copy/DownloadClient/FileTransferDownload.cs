using System;
using System.IO;
using System.Net;
using System.Collections;

using DotGrid.DotSec;
using DotGrid.Shared.Enums.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for DotDfsFileTransfer.
	/// </summary>
	public class FileTransferDownload
	{
		private bool secure;
		private string guid;
		private int parallel;
		private FileStream fs;
		private int BufferSize;
		//private QueueReadTest qread;
		private string writeFilename;
		//private OneStreamUploadTest[] workers;
		private NetworkCredential nc;
		private string remoteFilename;
		private string dotDfsServerAddress;
		private TransferMode mode;
		private RijndaelEncryption rijndael;
		//**************************************************************************************************************//
		public FileTransferDownload(string writeFilename, string remoteFilename, int parallel, int BufferSize, int MaxQueueWorkerSize, string dotDfsServerAddress, NetworkCredential nc, bool secure, TransferMode mode)
		{
			if(writeFilename == null)
				throw new ArgumentNullException("writeFilename is a null reference.");
			if(remoteFilename == null)
				throw new ArgumentNullException("remoteFilename is a null reference.");
			if(parallel <= 0)
				throw new ArgumentOutOfRangeException("parallel parameter can not be negative or zero.");
			if(BufferSize <= 0)
				throw new ArgumentOutOfRangeException("BufferSize parameter can not be negative or zero.");
			if(dotDfsServerAddress == null)
				throw new ArgumentNullException("dotDfsServerAddress is a null reference.");
			if(nc == null)
				throw new ArgumentNullException("nc is a null reference.");
			if(!Enum.IsDefined(typeof(TransferMode), mode))
				throw new ArgumentException("mode is'nt supported.");
			this.writeFilename = writeFilename;
			this.remoteFilename = remoteFilename;
			this.parallel = parallel;
			this.BufferSize = BufferSize;
			this.dotDfsServerAddress = dotDfsServerAddress;
			this.nc = nc;
			this.secure = secure;
			this.fs = new FileStream(writeFilename, FileMode.Open, FileAccess.Read, FileShare.None);
			//read = new QueueReadTest(ref this.fs, BufferSize, parallel, MaxQueueWorkerSize);
			guid = Guid.NewGuid().ToString();
			this.mode = mode;
			rijndael = new RijndaelEncryption(128); // a random 128 bits rijndael shared key
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public void RunTrasnsfer()
		{
			/*OneStream s = new OneStream(ref qread, remoteFilename, guid, fs.Length, parallel, BufferSize, dotDfsServerAddress, nc, secure);
			s.Run();*/
			/*workers = new OneStreamUploadTest[parallel];
			//s[0] = new OneStream(ref qread, remoteFilename, guid, fs.Length, parallel, BufferSize, dotDfsServerAddress, nc, secure);
			for(int i = 0 ; i < workers.Length ; i++)
				workers[i] = new OneStreamUploadTest(ref qread, remoteFilename, guid, fs.Length, parallel, BufferSize, dotDfsServerAddress, nc, secure, ref rijndael, mode);
			for(int i = 0 ; i < workers.Length ; i++)
				workers[i].Run();*/
			//qread.Close();
			//s.Run();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public void Close()
		{
			//qread.Close();
			/*if(workers != null)
				for(int i = 0 ; i < workers.Length ; i++)
					workers[i].Close();*/
			GC.Collect();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public long CurrentTransferredBytes
		{
			get
			{
				long written = 0;
				/*if(workers != null)
				{
					for(int i = 0 ; i < workers.Length; i++)
						if(workers[i] != null)
							written += workers[i].WrittenSize;
				}*/
				return written;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public double TotalElapsedTime
		{
			get
			{
				/*if(workers != null)
				{
					if(workers.Length == 1)
						return (workers[0].EndTime - workers[0].StartTime).TotalMilliseconds;
					else
						return (MaxTime(workers) - MinTime(workers)).TotalMilliseconds;
				}
				else*/ return 0;
			}
		}
		//**************************************************************************************************************//
	}
}