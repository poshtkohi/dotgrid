/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Net;
using System.Threading;
using System.Reflection;

namespace DotGrid.DotThreading
{
	/// <summary>
	/// Executes a thread collection like a multi-threaded application.
	/// </summary>
	public sealed class ThreadCollectionClient
	{
		private DotGridThreadClient thread;
		/// <summary>
		/// Initializes a new instance of the ThreadCollectionClient class.
		/// </summary>
		/// <param name="starts">A ThreadStart collection delegate that references the methods to be invoked when this thread collection begins executing.</param>
		/// <param name="modules">States modules that are depended to start parameter for convenient remote assembly loading.</param>
		/// <param name="DotGridThreadServerAddress">DotGridThreadClient server address.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination dotDfs server.</param>
		/// <param name="Secure">Determine secure or secureless connection.</param>
		public ThreadCollectionClient(ThreadStart[] starts, Module[] modules, string DotGridThreadServerAddress, NetworkCredential nc, bool Secure)
		{
			ThreadCollectionExecutor tce = new ThreadCollectionExecutor(starts);
			this.thread = new DotGridThreadClient(new ThreadStart(tce.Run), modules, DotGridThreadServerAddress, nc, Secure);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Causes the operating system to change the state of the current instance to ThreadState.Running
		/// </summary>
		public void Start()
		{
			this.thread.Start();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Raises a ThreadAbortException in the thread on which it is invoked, to begin the process of terminating the thread. Calling this method usually terminates the thread.
		/// </summary>
		public void Abort()
		{
			this.thread.Abort();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a value indicating the execution status of the current thread.
		/// </summary>
		public bool IsAlive
		{
			get
			{
				return this.thread.IsAlive;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets returned objects by server achieved by remote threads execution. 
		/// </summary>
		public object[] ReturnedObjects
		{
			get
			{
				if(this.thread.ReturnedObject != null)
				{
					ThreadCollectionExecutor tce = (ThreadCollectionExecutor)this.thread.ReturnedObject;
					object[] objs = new object[tce.Starts.Length];
					for(int i = 0 ; i < objs.Length ; i++)
						objs[i] = tce.Starts[i].Target;
					return objs;
				}
				else return null;
			}
		}
		//**************************************************************************************************************//
	}

}
