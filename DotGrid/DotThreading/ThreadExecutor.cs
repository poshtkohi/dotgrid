/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

using DotGrid.DotSec.Permission;
using DotGrid.Serialization;

namespace DotGrid.DotThreading
{
	/// <summary>
	/// Executes a thread or thread collection requested by client.
	/// </summary>
	[Serializable]
	public class ThreadExecutor : MarshalByRefObject
	{
		private bool IsAborted;
		//**************************************************************************************************************//
		/// <summary>
		/// Constructor for ThreadExecutor class.
		/// </summary>
		public ThreadExecutor()
		{
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Executes thread.
		/// </summary>
		/// <param name="methodName">String name of method to execute.</param>
		/// <param name="Obj">Binary representation of the remote object.</param>
		/// <param name="IsAborted">Determines aborted thread collection by the client.</param>
		/// <param name="permission">Needed permissions to run the thread or thread collection.</param>
		/// <returns>Executed result.</returns>
		public byte[] ExecuteThread(string methodName, byte[] Obj, ref bool IsAborted, object permission)
		{
			if(permission != null)
			{
				System.Security.PermissionSet setPerm = new System.Security.PermissionSet(System.Security.Permissions.PermissionState.None);
				System.Security.Permissions.SecurityPermissionFlag securityFlag = (System.Security.Permissions.SecurityPermissionFlag)permission.GetType().GetMethod("get_SecurityPermission").Invoke(permission, null);
				if(securityFlag != System.Security.Permissions.SecurityPermissionFlag.NoFlags)
					setPerm.AddPermission(new System.Security.Permissions.SecurityPermission(securityFlag));
				if(!(bool)permission.GetType().GetMethod("get_OdbcPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Data.Odbc.OdbcPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_OleDbPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Data.OleDb.OleDbPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_OraclePermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Data.OleDb.OleDbPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_SqlClientPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Data.SqlClient.SqlClientPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_EventLogPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Diagnostics.EventLogPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_PerformanceCounterPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Diagnostics.PerformanceCounterPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_DirectoryServicesPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.DirectoryServices.DirectoryServicesPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_PrintingPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Drawing.Printing.PrintingPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_MessageQueuePermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Messaging.MessageQueuePermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_DnsPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Net.DnsPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_SocketPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Net.SocketPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_EnvironmentPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.EnvironmentPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_FileDialogPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.FileDialogPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_FileIOPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.FileIOPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_IsolatedStorageFilePermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.IsolatedStorageFilePermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_PublisherIdentityPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.PublisherIdentityPermission(System.Security.Permissions.PermissionState.None));
				if(!(bool)permission.GetType().GetMethod("get_ReflectionPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.ReflectionPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_RegistryPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.RegistryPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_SiteIdentityPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.SiteIdentityPermission(System.Security.Permissions.PermissionState.None));
				if(!(bool)permission.GetType().GetMethod("get_StrongNameIdentityPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.PermissionState.None));
				if(!(bool)permission.GetType().GetMethod("get_UIPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.UIPermission(System.Security.Permissions.PermissionState.Unrestricted));
				//if(!(bool)permission.GetType().GetMethod("get_UrlIdentityPermission").Invoke(permission, null))
				//	setPerm.AddPermission(new System.Security.Permissions.UrlIdentityPermission(System.Security.Permissions.PermissionState.None));
				if(!(bool)permission.GetType().GetMethod("get_ZoneIdentityPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Security.Permissions.ZoneIdentityPermission(System.Security.Permissions.PermissionState.None));
				if(!(bool)permission.GetType().GetMethod("get_ServiceControllerPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.ServiceProcess.ServiceControllerPermission(System.Security.Permissions.PermissionState.Unrestricted));
				if(!(bool)permission.GetType().GetMethod("get_AspNetHostingPermission").Invoke(permission, null))
					setPerm.AddPermission(new System.Web.AspNetHostingPermission(System.Security.Permissions.PermissionState.Unrestricted));
				setPerm.Demand();
				setPerm.Deny();
			}
			this.IsAborted = IsAborted;
			object obj = SerializeDeserialize.DeSerialize(Obj);
			if(obj.GetType().ToString() == typeof(ThreadCollectionExecutor).ToString())
			{
				object[] oo = new object[1];
				oo[0] = IsAborted;
				obj.GetType().GetMethod("SetIsAborted").Invoke(obj, oo);
			}
			obj.GetType().GetMethod(methodName).Invoke(obj, null);
			return SerializeDeserialize.Serialize(obj);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Obtains a lifetime service object to control the lifetime policy for this instance.
		/// </summary>
		/// <returns></returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}
		//**************************************************************************************************************//
	}
}