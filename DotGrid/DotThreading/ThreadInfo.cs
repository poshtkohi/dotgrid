/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Collections;
using System.Threading;
using System.Reflection;

using DotGrid.Serialization;

namespace DotGrid.DotThreading
{// after completion all APIs, change the following class accessors to "internal"
	/// <summary>
	/// A class for module information.
	/// </summary>
	[Serializable()]
	public class ModuleInfo
	{
		private string _FullyQualifiedName;
		private string _ScopeName;
		//private DateTime _DateCreate;
		//**************************************************************************************************************//
		/// <summary>
		/// Constructor for ModuleInfo class.
		/// </summary>
		/// <param name="FullyQualifiedName">A string representing the fully qualified name and path to this module.</param>
		/// <param name="ScopeName">A string representing the name of the module.</param>
		/*/// <param name="DateCreate">The creation time of the current Assembly.</param>*/
		public ModuleInfo(string FullyQualifiedName, string ScopeName/*, DateTime DateCreate*/)
		{
			if(FullyQualifiedName == null)
				throw new ArgumentNullException("FullyQualifiedName can not be null.");
			if(ScopeName == null)
				throw new ArgumentNullException("ScopeName can not be null.");
			this._FullyQualifiedName = FullyQualifiedName;
			this._ScopeName = ScopeName;
			//this._DateCreate = DateCreate;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a string representing the fully qualified name and path to this module.
		/// </summary>
		public string FullyQualifiedName
		{
			get 
			{
				return this._FullyQualifiedName;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a string representing the name of the module.
		/// </summary>
		public string ScopeName
		{
			get 
			{
				return this._ScopeName;
			}
		}
		//**************************************************************************************************************//
		/*/// <summary>
		/// Gets the creation time of the current Assembly.
		/// </summary>
		public DateTime DateCreate
		{
			get 
			{
				return this._DateCreate;
			}
		}*/
		//**************************************************************************************************************//
	}



    /*/// <summary>
    /// 
    /// </summary>
	[Serializable]
	public abstract class AbstractThread : MarshalByRefObject
	{
		/// <summary>
		/// 
		/// </summary>
		public abstract void Run();
	}*/


	/// <summary>
	/// States remote thread information.
	/// </summary>
	[Serializable()]
	public class ThreadInfo
	{
		private string start;
		private string fullName;
		private ModuleInfo[] modules;
		private byte[] obj;
		private string guid;
		//**************************************************************************************************************//
		/// <summary>
		/// Initialize ThreadInfo instance.
		/// </summary>
		/// <param name="start">A ThreadStart delegate that references the methods to be invoked when the related thread instance begins executing.</param>
		/// <param name="modules">States modules that are depended to start parameter for convenient remote assembly loading.</param>
		public ThreadInfo(ThreadStart start, Module[] modules)
		{
			if(start == null)
				throw new ArgumentNullException("start can not be null.");
			if(modules == null)
				throw new ArgumentNullException("modules can not be null.");
			this.start = start.Method.Name;
			this.fullName = start.Method.DeclaringType.FullName;
			Module[] modifiedModules = SortUniqeModules(modules);
			this.modules = new ModuleInfo[modifiedModules.Length];
			for(int i = 0 ; i < this.modules.Length ; i++)
				this.modules[i] = new ModuleInfo(modifiedModules[i].FullyQualifiedName, modifiedModules[i].ScopeName/*, new FileInfo(modifiedModules[i].FullyQualifiedName).CreationTime*/);
			this.obj = SerializeDeserialize.Serialize(start.Target);
			this.guid = Guid.NewGuid().ToString();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sorts and modifies unique modules and returns new sorted and modified modules.
		/// </summary>
		/// <param name="modules">Modules for sorting and modifying.</param>
		/// <returns>Sorted and modified modules.</returns>
		private Module[] SortUniqeModules(Module[] modules)
		{
			Module[] _modules = modules;
			for(int j = 0 ; j < _modules.Length ; j++)
				if(_modules[j] != null)
					for(int i = j ; i < _modules.Length - j - 1 ; i++)
						if(_modules[i + 1] != null)
							if(_modules[j].ScopeName == _modules[i + 1].ScopeName)
								_modules[i + 1] = null;
			ArrayList al = new ArrayList();
			for(int i = 0 ; i < _modules.Length ; i++)
				if(_modules[i] != null)
						al.Add(_modules[i]);
			_modules = null;
			return (Module[])al.ToArray(typeof(Module));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets data object of this instance.
		/// </summary>
		public byte[] Obj
		{
			get
			{
				return this.obj;
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
		/// <summary>
		/// Gets start name of this instance.
		/// </summary>
		public string Start
		{
			get
			{
				return this.start;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets modules of this instance.
		/// </summary>
		public ModuleInfo[] Modules
		{
			get
			{
				return this.modules;
			}
		}
		//**************************************************************************************************************//
	}
}