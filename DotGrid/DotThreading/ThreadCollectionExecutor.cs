/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.DotThreading
{
	/// <summary>
	/// Executes a thread collection requested by client.
	/// </summary>
	[Serializable]
	public class ThreadCollectionExecutor
	{
		private System.Threading.ThreadStart[] starts;
		//[NonSerialized]  private System.Threading.Thread[] threads = null;
		private bool IsAborted;
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="starts"></param>
		public ThreadCollectionExecutor(System.Threading.ThreadStart[] starts)
		{
			this.starts = starts;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Runs the thread collection.
		/// </summary>
		public void Run()
		{
			System.Threading.Thread[] threads = new System.Threading.Thread[this.starts.Length];
			for(int i = 0 ; i < threads.Length ; i++)
			{
				threads[i] = new System.Threading.Thread(starts[i]);
				threads[i].Start();
			}
			/*int n = 0;
			while(true)
			{
				if(IsAborted)
				{
					Abort(threads);
					threads = null;
					return ;
				}
				for(int i = 0 ; i < threads.Length ; i++)
					if(threads[i].IsAlive)
						n++;
				if(n == threads.Length)
					break;
				System.Threading.Thread.Sleep(1);
				//Console.WriteLine(n);
			}*/
			int n = 0;
			while(true)
			{
				if(IsAborted)
				{
					Abort(threads);
					threads = null;
					GC.Collect();//
					return ;
				}
				for(int i = 0 ; i < starts.Length ; i++)
				{
					if(threads[i] != null)
					{
						if(threads[i].ThreadState == System.Threading.ThreadState.Stopped /*|| threads[i].ThreadState == System.Threading.ThreadState.Aborted*/)
						{
							threads[i] = null;
							n++;
						}
					}
					/*if(threads[i] != null)
						if((bool)starts[i].Target.GetType().GetMethod("get_IsEndedExecution").Invoke(starts[i].Target, null) == true)
						{
							threads[i] = null;
							n++;
						}*/
				}
				if(n == starts.Length)
				{
					threads = null;
					GC.Collect();//
					return ;
				}
				System.Threading.Thread.Sleep(1);
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="IsAborted"></param>
		public void SetIsAborted(ref bool IsAborted)
		{
			this.IsAborted = IsAborted;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="threads"></param>
		static private void Abort(System.Threading.Thread[] threads)
		{
			if(threads != null)
				for(int i = 0 ; i < threads.Length ; i++)
					if(threads[i] != null)
					{
						threads[i].Abort();
						//threads[i].Join();
						threads[i] = null;
					}
			//GC.Collect();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public System.Threading.ThreadStart[] Starts
		{
			get
			{
				return this.starts;
			}
		}
		//**************************************************************************************************************//
	}
}