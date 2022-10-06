/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Threading;
using System.Collections;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// This class provides a thread worker to upload files in when using UploadDirectoryClient class.
	/// </summary>
	internal class UploadDirectoryWorker
	{
		private DirectoryMovementClient client;
		private Thread worker;
		private bool closed = false;
		private int tcpBufferSize = 256 * 1024;
		private long transferredBytes = 0;
		/// <summary>
		/// i
		/// </summary>
		public int i = 0;
		private ArrayList errors;
		private bool ignoreExceptions;
		private UploadDirectoryClient manager;
		private bool finished = false;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of DownloadDirectoryWorker class.
		/// </summary>
		/// <param name="client">A DirectoryMovementClient instance.</param>
		/// <param name="manager">The manager that manages and controls execution of this thread worker.</param>
		/// <param name="tcpBufferSize">Specifies both TCP Window and file read/write buffer sizes.</param>
		/// <param name="errors">An reference for storing thrown system exceptions from the DotDFS server side or local system.</param>
		/// <param name="ignoreExceptions">Specifies whether the program must throw any dropped exception by the server and ending run or must ignore any thrown exception.</param>
		public UploadDirectoryWorker(DirectoryMovementClient client, UploadDirectoryClient manager, int tcpBufferSize, ref ArrayList errors, bool ignoreExceptions)
		{
			if(client == null)
				throw new ArgumentNullException("client is null.");
			if(manager == null)
				throw new ArgumentNullException("manager is null.");
			if(errors == null)
				throw new ArgumentNullException("errors is null.");
			if(tcpBufferSize > 0)
				this.tcpBufferSize = tcpBufferSize;
			this.errors = errors;
			this.client = client;
			this.ignoreExceptions = ignoreExceptions;
			this.manager = manager;
			this.worker = new Thread(new ThreadStart(this.WorkerProc));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Runs this instance thread worker.
		/// </summary>
		public void Run()
		{
			if(!worker.IsAlive)
				worker.Start();
		}
		//**************************************************************************************************************//
		/*/// <summary>
		/// Uploads a local file to remote DotDFS server through a single TCP connection.
		/// </summary>
		/// <param name="localFilename">The local file name.</param>
		/// <param name="remoteFilename">The remote file name.</param>
		/// <param name="offset">The point relative to origin from which to begin file transferring.</param>
		/// <param name="length">The maximum number of bytes to transfer.</param>
		/// <param name="tcpBufferSize">Specifies both TCP Window and file read/write buffer sizes.</param>
		/// <param name="transferredBytes">Finds value of current transferred bytes to remote DotDFS server.</param>
		public void UploadFile(string localFilename, string remoteFilename, long offset, long length, int tcpBufferSize, ref long transferredBytes)
		{
			if(localFilename == null)
				throw new ArgumentNullException("localFilename is null.");
			if(remoteFilename == null)
				throw new ArgumentNullException("remoteFilename is null.");
			if(tcpBufferSize <= 0)
				tcpBufferSize = 256 * 1024;
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset can not be negative.");
			this.localFilename = localFilename;
			this.remoteFilename = remoteFilename;
			this.offset = offset;
			this.length = length;
			this.tcpBufferSize = tcpBufferSize;
			this.transferredBytes = transferredBytes;
			if(!this.worker.IsAlive)
			{
				this.worker.Start();
				return ;
			}
			available = false;
			newTransfer = true;
		}*/
		//**************************************************************************************************************//
		/// <summary>
		/// Implements the thread worker procedure.
		/// </summary>
		private void WorkerProc()
		{
			while(true)
			{
				if(closed)
					break;
				string localFile = (string)manager.DequeueNewFile();
				if(localFile == null)
					break;
				try 
				{ 
					i++;
					string relativeFilePath = localFile.Substring(manager.LocalDirectoryPath.Length);
					string remoteAbsolutePath = String.Format(@"{0}{1}", manager.RemoteDirectoryPath ,relativeFilePath);
					client.UploadFile(localFile, remoteAbsolutePath, 0, -1, tcpBufferSize, ref transferredBytes);
					manager.AddCurrentCompletedFileCount();
				}
				catch(ObjectDisposedException e) { finished = true; Close(); throw e; }
				catch(Exception e) 
				{ 
					errors.Add(e);
					if(!this.ignoreExceptions)
					{
						finished = true;
						Close(); 
						return ;
					}
				}
			}
			finished = true;
			/*while(true)
			{
				if(closed)
					break;
				i++;
				finished = false;
				try { client.UploadFile(localFilename, remoteFilename, offset, length, tcpBufferSize, ref transferredBytes); }
				catch(ObjectDisposedException e) { finished = true; Close(); throw e; }
				catch(Exception e) 
				{ 
					finished = true;
					errors.Add(e);
					if(!this.ignoreExceptions)
					{
						Close(); 
						return ;
					}
				}
				finished = true;
				newTransfer = false;
				ResetNeededVariables();
				available = true;
				while(true)
				{
					if(closed)
						return ;
					if(newTransfer)
						break;
					Thread.Sleep(1);
				}
			}
			finished = true;*/
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets number of transferred bytes for this thread worker;
		/// </summary>
		public long TransferredBytes
		{
			get
			{
				return transferredBytes;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets whether this worker has finished its work.
		/// </summary>
		public bool IsFinished
		{
			get
			{
				return finished;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public void Close()
		{
			if(this.worker == null || this.client == null)
				throw new ObjectDisposedException("could not access to a disposed object.");
			else
			{
				this.closed = true;
				this.worker.Abort();
				this.worker = null;
				this.client.Close();
				this.client = null;
			}
		}
		//**************************************************************************************************************//
	}
}