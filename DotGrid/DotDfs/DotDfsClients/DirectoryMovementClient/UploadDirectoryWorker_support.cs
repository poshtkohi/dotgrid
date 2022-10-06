/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Threading;

namespace DotGrid.DotDfs
{
	 summary
	 This class provides a thread worker to upload files in when using UploadDirectoryClient class.
	 summary
	public class UploadDirectoryWorker
	{
		private DirectoryMovementClient client;
		private Thread worker;
		private bool exited = false;
		private bool closed = false;
		private string localFilename; 
		private string remoteFilename;
		private long offset;
		private long length;
		private int tcpBufferSize;
		private long transferredBytes;
		public int i = 0;
		public int id;
		
		 summary
		 Initializes a new instance of DirectoryMovementClient class.
		 summary
		 param name=clientA DirectoryMovementClient instance.param
		public UploadDirectoryWorker(DirectoryMovementClient client, int id)
		{
			if(client == null)
				throw new ArgumentNullException(client id null.);
			this.client = client;
			this.worker = new Thread(new ThreadStart(this.WorkerProc));
			this.id = id;
		}
		
		public void Start()
		{
			if(this.worker.ThreadState == ThreadState.Unstarted)
				this.worker.Start();
			if(this.worker.ThreadState == ThreadState.Suspended)
				this.worker.Resume();
		}
		
		public void Stop()
		{
			if(this.worker.ThreadState == ThreadState.Unstarted)
				this.worker.Start();
			if(this.worker.ThreadState == ThreadState.Suspended)
				this.worker.Resume();
		}
		 summary
		 Uploads a local file to remote DotDFS server through a single TCP connection.
		 summary
		 param name=localFilenameThe local file name.param
		 param name=remoteFilenameThe remote file name.param
		 param name=offsetThe point relative to origin from which to begin file transferring.param
		 param name=lengthThe maximum number of bytes to transfer.param
		 param name=tcpBufferSizeSpecifies both TCP Window and file readwrite buffer sizes.param
		 param name=transferredBytesFinds value of current transferred bytes to remote DotDFS server.param
		public void UploadFile(string localFilename, string remoteFilename, long offset, long length, int tcpBufferSize, ref long transferredBytes)
		{
			if(localFilename == null)
				throw new ArgumentNullException(localFilename is null.);
			if(remoteFilename == null)
				throw new ArgumentNullException(remoteFilename is null.);
			if(tcpBufferSize = 0)
				tcpBufferSize = 256  1024;
			if(offset  0)
				throw new ArgumentOutOfRangeException(offset can not be negative.);
			if(length  0)
				throw new ArgumentOutOfRangeException(length can not be negative.);
			this.localFilename = localFilename;
			this.remoteFilename = remoteFilename;
			this.offset = offset;
			this.length = length;
			this.tcpBufferSize = tcpBufferSize;
			this.transferredBytes = transferredBytes;
			if(this.worker.ThreadState == ThreadState.Suspended)
			{
				this.worker.Resume();
				return ;
			}
			if(!this.worker.IsAlive)
			{
				this.worker.Start();
				return ;
			}
			else
			{
				while(true)
				{
					if(worker.ThreadState == ThreadState.Suspended)
						break;
					Thread.Sleep(1);
				}
				worker.Resume();
			}
		}
		
		 summary
		 
		 summary
		private void WorkerProc()
		{
			while(true)
			{
				if(closed)
					break;
				i++;
				Console.WriteLine(i am worker({0})., id);
				this.client.UploadFile(localFilename, remoteFilename, offset, length, tcpBufferSize, ref transferredBytes);
				ResetNeededVariables();
				this.worker.Suspend();
				Console.WriteLine(hello);
			}
		}
		
		 summary
		 Gets whether this worker is available to upload new file.
		 summary
		public bool Available
		{
			get
			{
				if(!this.worker.IsAlive)
					return true;
				if(this.worker.ThreadState == ThreadState.Suspended)
					return true;
				else return false;
			}
		}
		
		private void ResetNeededVariables()
		{
			this.localFilename = null;
			this.remoteFilename = null;
			this.offset = 0;
			this.length = 0;
			this.tcpBufferSize = 256  1024;
			this.transferredBytes = 0;
		}
		
		 summary
		 
		 summary
		public void Close()
		{
			if(this.worker == null  this.client == null)
				throw new ObjectDisposedException(could not access to a disposed object.);
			else
			{
				this.closed = true;
				this.worker.Abort();
				this.worker = null;
				this.client.Close();
				this.client = null;
			}
		}
		
	}
}
