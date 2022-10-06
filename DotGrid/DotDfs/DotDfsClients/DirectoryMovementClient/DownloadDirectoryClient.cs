/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections;

using DotGrid.DotDfs;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// This class extends DirectoryMovementClient class and add download capabilities of a directory tree from DotDFS server to local path via using parallel TCP connections.
	/// </summary>
	public class DownloadDirectoryClient
	{
		private string localDirectoryPath;
		private string remoteDirectoryPath;
		private string dotDfsServerAddress;
		private int parallel = 1;
		private int tcpBufferSize = 256 * 1024;
		private NetworkCredential nc;
		private bool secure;
		private bool ignoreExceptions;
		private bool ignoreReplace;
		private ArrayList errors = new ArrayList();
		private DirectoryMovementClient[] clients;
		private DownloadDirectoryWorker[] workers;
		private Queue remoteFiles = new Queue();
		private bool locked = false;
		private bool closed = false;
		private int totalFileCount = 0;
		private int currentCompletedFileCount = 0;
		private DateTime t1;
		private DateTime t2;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of DownloadDirectoryClient class.
		/// </summary>
		/// <param name="localDirectoryPath">The local directory path name to download the remote directory tree to it located on remote DotDFS server.</param>
		/// <param name="remoteDirectoryPath">The remote directory path name to download.</param>
		/// <param name="dotDfsServerAddress">The remote DotDFS server address.</param>
		/// <param name="parallel">Number of parallel TCP Connections.</param>
		/// <param name="tcpBufferSize">Specifies both TCP Window and file read/write buffer sizes.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination DotDFS server.</param>
		/// <param name="secure">Determines secure or secureless connection based on DotGrid.DotSec transfer layer security.</param>
		/// <param name="ignoreReplace">Specifies whether the program must ignore replacing available files in server side or must not ignore.</param>
		/// <param name="ignoreExceptions">Specifies whether the program must throw any dropped exception by the server and ending run or must ignore any thrown exception.</param>
		public DownloadDirectoryClient(string localDirectoryPath, string remoteDirectoryPath, string dotDfsServerAddress, int parallel, int tcpBufferSize, NetworkCredential nc, bool secure, bool ignoreReplace, bool ignoreExceptions)
		{
			if(localDirectoryPath == null)
				throw new ArgumentNullException("localDirectoryPath is null.");
			if(remoteDirectoryPath == null)
				throw new ArgumentNullException("remoteDirectoryPath is null.");
			if(dotDfsServerAddress == null)
				throw new ArgumentNullException("dotDfsServerAddress is null.");
			if(parallel > 1)
				this.parallel = parallel;
			if(tcpBufferSize > 0)
				this.tcpBufferSize = tcpBufferSize;
			if(nc == null)
				throw new ArgumentNullException("nc is null.");
			this.localDirectoryPath = localDirectoryPath;
			this.remoteDirectoryPath = remoteDirectoryPath;
			this.dotDfsServerAddress = dotDfsServerAddress;
			this.nc = nc;
			this.secure = secure;
			this.ignoreReplace = ignoreReplace;
			this.ignoreExceptions = ignoreExceptions;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Runs the directory tree transfer.
		/// </summary>
		public void Run()
		{
			if(workers != null)
				return ;
			workers = new DownloadDirectoryWorker[parallel];
			clients = new DirectoryMovementClient[parallel];
			for(int i = 0 ; i < this.parallel ; i++)
			{
				clients[i] = new DirectoryMovementClient(dotDfsServerAddress, nc, secure);
				workers[i] = new DownloadDirectoryWorker(clients[i], this, tcpBufferSize, ref errors, ignoreExceptions);
			}
			this.t1 = DateTime.Now;
			TravellMainRemoteDirectory();//
			TravelAllRemoteDirectories(remoteDirectoryPath);
			totalFileCount = remoteFiles.Count;
			if(remoteFiles.Count > 0)
			{
				for(int i = 0 ; i < workers.Length ; i++)
					workers[i].Run();
				while(true)   // Wait till all workers are finished their transfers.
				{
					if(closed)
						break;
					if(remoteFiles.Count == 0)
					{
						int n = 0;
						for(int i = 0 ; i < workers.Length ; i++)
							if(workers[i].IsFinished)
								n++;
						if(n == workers.Length)
							break;
					}
					Thread.Sleep(1);
				}
			}
			this.t2 = DateTime.Now;
			for(int i = 0 ; i < workers.Length ; i++)
				workers[i].Close();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the starting time of the directory tree transfer. 
		/// </summary>
		private DateTime StartTime
		{
			get
			{
				return this.t1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the final time of the directory tree transfer.
		/// </summary>
		private DateTime EndTime
		{
			get
			{
				return this.t2;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the total elapsed time in this directory tree transfer session.
		/// </summary>
		public double TotalElapsedTime
		{
			get
			{
				if(workers != null)
					return (EndTime - StartTime).TotalMilliseconds;
				else return 0;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Dequeues a new file from queue to transfer by thread workers.
		/// </summary>
		/// <returns>A new file name to transfer.</returns>
		public string DequeueNewFile()
		{
			if(locked)
			{
				while(locked)
				{
					if(closed)
						return null;
					Thread.Sleep(1);
				}
			}
			locked = true;
			if(remoteFiles.Count == 0)
			{
				locked = false;
				return null;
			}
			else 
			{
				string newFile = (string) remoteFiles.Dequeue();
				locked = false;
				return newFile;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets current number of transferred bytes in this directory tree transfer session.
		/// </summary>
		public long CurrentTransferredBytes
		{
			get
			{
				if(workers == null)
					return 0;
				else
				{
					long transferred = 0;
					for(int i = 0 ; i < workers.Length ; i++)
						transferred += workers[i].TransferredBytes;
					return transferred;
				}
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Making increment a unit to current completed file count. This method only must be used by thread workers instantiated of DownloadDirectoryWorker class.
		/// </summary>
		public void AddCurrentCompletedFileCount()
		{
			currentCompletedFileCount++;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets percentage of current completed directory tree transfer session.
		/// </summary>
		public int CompletedPercentage
		{
			get
			{
				if(totalFileCount == 0 || currentCompletedFileCount == 0)
					return 0;
				else return (int)(((float)currentCompletedFileCount/(float)totalFileCount) * 100);
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets local directory path of this instance.
		/// </summary>
		public string LocalDirectoryPath
		{
			get
			{
				return this.localDirectoryPath;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets remote directory path of this instance.
		/// </summary>
		public string RemoteDirectoryPath
		{
			get
		    {
				return this.remoteDirectoryPath;
		    }
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a collection of thrown server or client side exceptions. If there are no any exception, null will be returned. 
		/// </summary>
		public Exception[] ThrownExceptions
		{
			get
			{
				if(errors.Count > 0)
					return (Exception[]) errors.ToArray(typeof(Exception));
				else return null;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Closes all workers threads and connections to DotDFS server. Calling this method will cause that all consumed system recourses are released.
		/// </summary>
		public void Close()
		{
			closed = true;
			if(workers == null)
				throw new ObjectDisposedException("could not access to a disposed object.");
			for(int i = 0 ; i < workers.Length ; i++)
			{
				try { workers[i].Close(); }
				catch { }
			}
			workers = null;
			clients = null;
		}
		//**************************************************************************************************************//
		private void TravellMainRemoteDirectory()
		{
			string[] files = clients[0].GetFiles(remoteDirectoryPath);
			if(files != null)
				for(int i = 0 ; i < files.Length ; i++)
					remoteFiles.Enqueue(files[i]);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Travels the remote directory tree recursively. 
		/// </summary>
		/// <param name="path">The remote directory path name to travel.</param>
		private void TravelAllRemoteDirectories(string path)
		{
			string[] files;
			string[] dirs = clients[0].GetDirectories(path);
			foreach(string dir in dirs)
			{
				string relativeDirectory = dir.Substring(remoteDirectoryPath.Length);
				string localAbsolutePath = String.Format(@"{0}{1}", localDirectoryPath ,relativeDirectory);
				files = clients[0].GetFiles(dir);
				if(files != null)
					for(int i = 0 ; i < files.Length ; i++)
						remoteFiles.Enqueue(files[i]);
				Directory.CreateDirectory(localAbsolutePath);
				TravelAllRemoteDirectories(dir);
			}
		}
		//**************************************************************************************************************//
	}
}