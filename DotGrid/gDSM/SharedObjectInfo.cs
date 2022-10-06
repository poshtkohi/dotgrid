/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Reflection;
using System.Collections;

using DotGrid.Serialization;

namespace DotGrid.gDSM
{
	/// <summary>
	/// A class for shared object information on gDsm.
	/// </summary>
	[Serializable]
	public class SharedObjectInfo
	{
		private byte[] obj = null;
		private string guid = null;
		private string owner = null;
		private string[] nodes;
		private FileAccess access;
		private string[] dependencies = null;
		private ArrayList users = null;
		private bool locked = false;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the SharedObjectInfo class.
		/// </summary>
		/// <param name="obj">The main object to share on gDsm</param>
		/// <param name="guid">Unique ID of this shared object on gDsm.</param>
		/// <param name="owner">Address of the owner of this shared object.</param>
		/// <param name="nodes">Specifies the participating nodes on current gDsm.</param>
		/// <param name="access">Defines constants for read, write, or read/write access to this shared object on gDsm.</param>
		public SharedObjectInfo(object obj, string guid, string owner, string[] nodes, FileAccess access)
		{
			if(obj == null)
				throw new ArgumentNullException("obj can not be null");
			if(guid == null)
				throw new ArgumentNullException("guid can not be null");
			if(owner == null)
				throw new ArgumentNullException("owner can not be null");
			if(nodes == null)
				throw new ArgumentNullException("nodes can not be null");
			this.obj = SerializeDeserialize.Serialize(obj);
			this.guid = guid;
			this.owner = owner;
			this.nodes = nodes;
			this.access = access;
			Type type = obj.GetType();
			Module[] modules = type.Module.Assembly.GetLoadedModules();
			if(modules != null)
			{
				ArrayList al = null;
				for(int i = 0 ; i < modules.Length; i++)
				{
					if(modules[i].Name.ToLower().IndexOf("system", 0, 6) < 0)
					{
						if(al == null)
							al = new ArrayList();
						al.Add(modules[i].FullyQualifiedName);
					}
				}
				if(al != null)
				{
					this.dependencies = new string[al.Count];
					for(int i = 0 ; i < al.Count ; i++)
						this.dependencies[i] = (string)al[i];
				}
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Adds a new node to list of users of this shared object on gDsm.
		/// </summary>
		/// <param name="node">Address of new user node.</param>
		/// <returns>If the node has not been existed then true will be returned.</returns>
		public bool AddNode(string node)
		{
			if(this.users == null)
				this.users = new ArrayList();
			if(!this.users.Contains(node))
			{
				this.users.Add(node);
				return true;
			}
			else return false;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Removes a available node from list of users of this shared object on gDsm.
		/// </summary>
		/// <param name="node">Address of a user node.</param>
		/// <returns>If the node has not been existed then true will be returned.</returns>
		public bool RemoveNode(string node)
		{
			if(this.users != null)
			{
				if(this.users.Contains(node))
				{
					this.users.Remove(node);
					return true;
				}
				else return false;
			}
			else return false;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets all nodes using this shared memory instance on gDsm.
		/// </summary>
		public string[] NodeUsers
		{
			get
			{
				if(this.users != null)
				{
					return (string[])this.users.ToArray(typeof(string));
				}
				else return null;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets Locked or UnLocked state of this shared object on gDsm.
		/// </summary>
		public  bool IsLocked
		{
			get
			{
				return this.locked;
			}
			set
			{
				this.locked = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets read, write, or read/write access to this shared object on gDsm.
		/// </summary>
		public FileAccess Access
		{
			get
			{
				return this.access;
			}
			set
			{
				this.access = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets binary form of this shared object.
		/// </summary>
		public byte[] SharedObject
		{
			get
			{
				return this.obj;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets unique ID of this shared object on gDsm.
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
		/// Gets the owner address of this shared object.
		/// </summary>
		public string Owner
		{
			get
			{
				return this.owner;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the nodes that are participating for this shared object on related gDsm topology deployment.
		/// </summary>
		public string[] Nodes
		{
			get
			{
				return this.nodes;
			}
		}
		//**************************************************************************************************************//
	}
}