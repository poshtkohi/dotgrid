/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Diagnostics;

namespace DotGrid.DotRemoteProcess
{
	/// <summary>
	/// Specifies remote process information.
	/// </summary>
	[Serializable]
	public class RemoteProcessInfo
	{
		private int _HandleCount;
		private int _HasExited;
		private int _Id;
		private int _NonpagedSystemMemorySize;
		private int _PagedMemorySize;
		private int _PagedSystemMemorySize;
		private int _PeakPagedMemorySize;
		private int _PeakWorkingSet;
		private int _PrivateMemorySize;
		private string _ProcessName;
		private int _Responding;
		private DateTime _StartTime;
		private TimeSpan _TotalProcessorTime;
		private TimeSpan _UserProcessorTime;
		private int _VirtualMemorySize;
		private int _WorkingSet;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the RemoteProcessInfo class.
		/// </summary>
		/// <param name="remoteProcess">Specifies local remote process.</param>
		public RemoteProcessInfo(Process remoteProcess) // object remoteProcess
		{
			if(remoteProcess == null)
				throw new ArgumentNullException("remoteProcess can not be null.");		
			try{this._HandleCount = remoteProcess.HandleCount;}
			catch{_HandleCount = -1 ;}

			try{this._HasExited = Convert.ToInt32(remoteProcess.HasExited);}
			catch{_HasExited = -1 ;}

			try{this._Id = remoteProcess.Id;}
			catch{_Id = -1 ;}

			try{this._NonpagedSystemMemorySize = remoteProcess.NonpagedSystemMemorySize;}
			catch{_NonpagedSystemMemorySize = -1 ;}

			try{this._PagedMemorySize = remoteProcess.PagedMemorySize;}
			catch{_PagedMemorySize = -1 ;}

			try{this._PagedSystemMemorySize = remoteProcess.PagedSystemMemorySize;}
			catch{_PagedSystemMemorySize = -1 ;}

			try{this._PeakPagedMemorySize = remoteProcess.PeakPagedMemorySize;}
			catch{_PeakPagedMemorySize = -1 ;}

			try{this._PeakWorkingSet = remoteProcess.PeakWorkingSet;}
			catch{_PeakWorkingSet = -1 ;}

			try{this._PrivateMemorySize = remoteProcess.PrivateMemorySize;}
			catch{_PrivateMemorySize = -1 ;}

			try{this._ProcessName = remoteProcess.ProcessName;}
			catch{_ProcessName = null ;}

			try{this._Responding = Convert.ToInt32(remoteProcess.Responding);}
			catch{_Responding = -1 ;}

			try{this._StartTime = remoteProcess.StartTime;}
			catch{}

			try{this._TotalProcessorTime = remoteProcess.TotalProcessorTime;}
			catch{}

			try{this._UserProcessorTime = remoteProcess.UserProcessorTime;}
			catch{}

			try{this._VirtualMemorySize = remoteProcess.VirtualMemorySize;}
			catch{_VirtualMemorySize = -1 ;}

			try{this._WorkingSet = remoteProcess.WorkingSet;}
			catch{_WorkingSet = -1 ;}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the number of handles opened by the remote process. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int HandleCount
		{
			get
			{
				return this._HandleCount;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a value indicating whether the associated remote process has been terminated. 1 for true and 0 for false. If any errors occures on server-side, this value will be -1. 
		/// </summary>
		public int HasExited
		{
			get
			{
				return this._HasExited;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the unique identifier for the associated remote process. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int Id
		{
			get
			{
				return this._Id;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the nonpaged system memory size allocated to this remote process. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int NonpagedSystemMemorySize
		{
			get
			{
				return this._NonpagedSystemMemorySize;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the paged memory size for the remote process. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int PagedMemorySize
		{
			get
			{
				return this._PagedMemorySize;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the paged system memory size for the remote process. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int PagedSystemMemorySize
		{
			get
			{
				return this._PagedSystemMemorySize;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the peak paged memory size for the remote process. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int PeakPagedMemorySize
		{
			get
			{
				return this._PeakPagedMemorySize;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the peak working set size for the remote process. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int PeakWorkingSet 
		{
			get
			{
				return this._PeakWorkingSet;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the private memory size for the remote process. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int PrivateMemorySize
		{
			get
			{
				return this._PrivateMemorySize;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the name of the remote process.
		/// </summary>
		public string ProcessName
		{
			get
			{
				return this._ProcessName;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a value indicating whether the user interface of the remote process is responding. 1 for true and 0 for false. If any errors occures on server-side, this value will be -1. 
		/// </summary>
		public int Responding
		{
			get
			{
				return this._Responding;
			}
		}
		//**************************************************************************************************************//
		//public ProcessStartInfo StartInfo {get; set;}
		/// <summary>
		/// Gets the time that the associated remote process was started.
		/// </summary>
		public DateTime StartTime
		{
			get
			{
				return this._StartTime;
			}
		}
		//**************************************************************************************************************//
		//public ProcessThreadCollectionClient Threads {get;}
		/// <summary>
		/// Gets the total processor time for this remote process.
		/// </summary>
		public TimeSpan TotalProcessorTime
		{
			get
			{
				return this._TotalProcessorTime;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the user processor time for this remote process.
		/// </summary>
		public TimeSpan UserProcessorTime
		{
			get
			{
				return this._UserProcessorTime;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the size of the remote process's virtual memory. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int VirtualMemorySize
		{
			get 
			{
				return this._VirtualMemorySize;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the associated remote process's physical memory usage. If any errors occurs on server-side, this value will be -1. 
		/// </summary>
		public int WorkingSet
		{
			get
			{
				return this._WorkingSet;
			}
		}
		//**************************************************************************************************************//
	}
}