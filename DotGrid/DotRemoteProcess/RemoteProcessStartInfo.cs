/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Collections;
using System.Diagnostics;

namespace DotGrid.DotRemoteProcess
{
	/// <summary>
	/// Specifies a set of values used when starting a remote process.
	/// </summary>
	[Serializable]
	public sealed class RemoteProcessStartInfo
	{
		private string _Arguments = null;
		private string[] _Dependencies = null;
		private string _FileName = null;
		private string _Verb = null;
		private string guid;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the RemoteProcessStartInfo class.
		/// </summary>
		/// <param name="info">Specifies ProcessStartInfo for local remote process.</param>
		/// <param name="dependencies">File name dependencies for this process.</param>
		public RemoteProcessStartInfo(ProcessStartInfo info, string[] dependencies)
		{
			if(info == null)
				throw new ArgumentNullException("info can not be null.");
			if(info.FileName == null)
				throw new ArgumentNullException("info.FileName can not be null.");
			this._Arguments = info.Arguments;
			this._Dependencies = dependencies;
			this._FileName = info.FileName;
			this._Verb = info.Verb;
			this.guid = Guid.NewGuid().ToString();
			if(this._Dependencies != null)
				this._Dependencies = SortUniqeDependencies(this._Dependencies);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the RemoteProcessStartInfo class.
		/// </summary>
		/// <param name="info">Specifies ProcessStartInfo for local remote process.</param>
		public RemoteProcessStartInfo(ProcessStartInfo info)
		{
			if(info == null)
				throw new ArgumentNullException("info can not be null.");
			if(info.FileName == null)
				throw new ArgumentNullException("info.FileName can not be null.");
			this._Arguments = info.Arguments;
			this._FileName = info.FileName;
			this._Verb = info.Verb;
			this.guid = Guid.NewGuid().ToString();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sorts and modifies unique dependencies and returns new sorted and modified dependencies.
		/// </summary>
		/// <param name="dependencies">dependencies for sorting and modifying.</param>
		/// <returns>Sorted and modified dependencies.</returns>
		private string[] SortUniqeDependencies(string[] dependencies)
		{
			string[] _dependencies = dependencies;
			for(int j = 0 ; j < _dependencies.Length ; j++)
				if(_dependencies[j] != null)
					for(int i = j ; i < _dependencies.Length - j - 1 ; i++)
						if(_dependencies[i + 1] != null)
							if(_dependencies[j] == _dependencies[i + 1])
								_dependencies[i + 1] = null;
			ArrayList al = new ArrayList();
			for(int i = 0 ; i < _dependencies.Length ; i++)
				if(_dependencies[i] != null)
					al.Add(_dependencies[i]);
			_dependencies = null;
			return (string[])al.ToArray(typeof(string));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the set of command line arguments to use when starting the application.
		/// </summary>
		public string Arguments
		{
			get
			{
				return this._Arguments;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the application or document to start.
		/// </summary>
		public string FileName
		{
			get
			{
				return this._FileName;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the set of verb associated with the type of file specified by the FileName property.
		/// </summary>
		public string Verb
		{
			get
			{
				return this._Verb;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets file name dependencies for this process.
		/// </summary>
		public string [] Dependencies
		{
			get
			{
				return this._Dependencies;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets guid of this instance.
		/// </summary>
		public string GUID 
		{
			get
			{
				return this.guid;
			}
		}
		//**************************************************************************************************************//
	}
}
