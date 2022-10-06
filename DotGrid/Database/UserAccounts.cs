/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Net;
using MySql.Data.MySqlClient;
using DotGrid.DotSec.Permission;

namespace DotGrid.Database
{
	/// <summary>
	/// Manages user accounts.
	/// </summary>
	public class UserAccounts
	{
		private string MySqlAddress;
		private NetworkCredential nc;
		//**************************************************************************************************************//
		/// <summary>
		/// A constructor for UserAccounts class.
		/// </summary>
		/// <param name="MySqlAddress">MySql server address.</param>
		/// <param name="nc">Needed credential for connecting to MySql server.</param>
		public UserAccounts(string MySqlAddress, NetworkCredential nc)
		{
			if(MySqlAddress == null)
				throw new ArgumentNullException("MySqlAddress is null");
			if(nc == null)
				throw new ArgumentNullException("nc is null");
			this.MySqlAddress = MySqlAddress;
			this.nc = nc;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Adds new user to MySql accounts database.
		/// </summary>
		/// <param name="isAdministratorAccount">True, if this user is for an Administrator account that has full system permission. Otherwise, False for Others kind accounts.</param>
		/// <param name="newUser">New user credential.</param>
		/// <returns>If there are not a user with this provided newUser account, True will be returned.</returns>
		public bool AddUser(bool isAdministratorAccount, NetworkCredential newUser)
		{
			if(newUser == null)
				throw new ArgumentNullException("newUser is null");
			string ConnectionString = String.Format("Database=permissionservice;Data Source={0};User Id={1};Password={2}", this.MySqlAddress, this.nc.UserName, this.nc.Password);
			MySqlConnection connection = new MySqlConnection(ConnectionString);
			connection.Open();
			MySqlCommand command = connection.CreateCommand();
			MySqlTransaction trans = connection.BeginTransaction();
			command.Connection = connection;
			command.Transaction = trans;
			string query = String.Format("SELECT COUNT(*) FROM accounts WHERE username='{0}'", newUser.UserName);
			command.CommandText = query;
			MySqlDataReader read = command.ExecuteReader();
			if(read.Read())
			{
				int count = read.GetInt32(0);
				if(count != 0)
				{
					read.Close();
					trans.Commit();
					connection.Close();
					return false;
				}
			}
			read.Close();
			Permissions permission = null;
			if(!isAdministratorAccount) // Only for .NET 1.1
			{
				permission = new Permissions();
				query = "SELECT PermissionName,Others FROM permissions1_1";
				command.CommandText = query;
				read = command.ExecuteReader();
				while(read.Read())
				{
					string name = read.GetString(0).Trim();
					bool others = read.GetBoolean(1);
					switch(name)
					{
						case "AspNetHostingPermission":
							permission.AspNetHostingPermission = others;
							break;
						case "Assertion":
							permission.Assertion = others;
							break;
						case "BindingRedirects":
							permission.BindingRedirects = others;
							break;
						case "ControlAppDomain":
							permission.ControlAppDomain = others;
							break;
						case "ControlDomainPolicy":
							permission.ControlDomainPolicy = others;
							break;
						case "ControlEvidence":
							permission.ControlEvidence = others;
							break;
						case "ControlPolicy":
							permission.ControlPolicy = others;
							break;
						case "ControlPrincipal":
							permission.ControlPrincipal = others;
							break;
						case "ControlThread":
							permission.ControlThread = others;
							break;
						case "DirectoryServicesPermission":
							permission.DirectoryServicesPermission = others;
							break;
						case "DnsPermission":
							permission.DnsPermission = others;
							break;
						case "EnvironmentPermission":
							permission.EnvironmentPermission = others;
							break;
						case "EventLogPermission":
							permission.EventLogPermission = others;
							break;
						case "Execution":
							permission.Execution = others;
							break;
						case "FileDialogPermission":
							permission.FileDialogPermission = others;
							break;
						case "FileIOPermission":
							permission.FileIOPermission = others;
							break;
						case "Infrastructure":
							permission.Infrastructure = others;
							break;
						case "IsolatedStorageFilePermission":
							permission.IsolatedStorageFilePermission = others;
							break;
						case "MessageQueuePermission":
							permission.MessageQueuePermission = others;
							break;
						case "OdbcPermission":
							permission.OdbcPermission = others;
							break;
						case "OleDbPermission":
							permission.OleDbPermission = others;
							break;
						case "OraclePermission":
							permission.OraclePermission = others;
							break;
						case "PerformanceCounterPermission":
							permission.PerformanceCounterPermission = others;
							break;
						case "PrincipalPermission":
							permission.PrincipalPermission = others;
							break;
						case "PrintingPermission":
							permission.PrintingPermission = others;
							break;
						case "PublisherIdentityPermission":
							permission.PublisherIdentityPermission = others;
							break;
						case "ReflectionPermission":
							permission.ReflectionPermission = others;
							break;
						case "RegistryPermission":
							permission.RegistryPermission = others;
							break;
						case "RemotingConfiguration":
							permission.RemotingConfiguration = others;
							break;
						case "SerializationFormatter":
							permission.SerializationFormatter = others;
							break;
						case "ServiceControllerPermission":
							permission.ServiceControllerPermission = others;
							break;
						case "SiteIdentityPermission":
							permission.SiteIdentityPermission = others;
							break;
						case "SkipVerification":
							permission.SkipVerification = others;
							break;
						case "SocketPermission":
							permission.SocketPermission = others;
							break;
						case "SqlClientPermission":
							permission.SqlClientPermission = others;
							break;
						case "StrongNameIdentityPermission":
							permission.StrongNameIdentityPermission = others;
							break;
						case "UIPermission":
							permission.UIPermission = others;
							break;
						case "UnmanagedCode":
							permission.UnmanagedCode = others;
							break;
						case "UrlIdentityPermission":
							permission.UrlIdentityPermission = others;
							break;
						case "ZoneIdentityPermission":
							permission.ZoneIdentityPermission = others;
							break;
						default:
							break;
					}
				}
				read.Close();
			}
			if(permission != null)
				command.CommandText = String.Format("INSERT INTO accounts (username,password,AccountType,permissions1_1) VALUES('{0}','{1}',{2},'{3}')", newUser.UserName, newUser.Password, 0, Permissions.ToXml(permission));
			else
				command.CommandText =  String.Format("INSERT INTO accounts (username,password,AccountType) VALUES('{0}','{1}','{2}')", newUser.UserName, newUser.Password, 1);
			command.ExecuteNonQuery();
			trans.Commit();
			connection.Close();
			return true;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Logins a user account.
		/// </summary>
		/// <param name="userAccount">User credential to login.</param>
		/// <returns>Returns true, if the user already exists in accounts database.</returns>
		public bool LoginUser(NetworkCredential userAccount)
		{
			if(userAccount == null)
				throw new ArgumentNullException("userAccount is null");
			string ConnectionString = String.Format("Database=permissionservice;Data Source={0};User Id={1};Password={2}", this.MySqlAddress, this.nc.UserName, this.nc.Password);
			MySqlConnection connection = new MySqlConnection(ConnectionString);
			connection.Open();
			MySqlCommand command = connection.CreateCommand();
			command.Connection = connection;
			string query = String.Format("SELECT COUNT(*) FROM accounts WHERE username='{0}' AND password='{1}'", userAccount.UserName, userAccount.Password);
			command.CommandText = query;
			MySqlDataReader read = command.ExecuteReader();
			int count = 0;
			if(read.Read())
				count = read.GetInt32(0);
			read.Close();
			connection.Close();
			if(count == 0)
				return false;
			else return true;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Logins and gets user permissions the user account.
		/// </summary>
		/// <param name="userAccount">User credential to login.</param>
		/// <param name="permission">If permission is null, this user account is an administrator permission with unlimited system permissions.</param>
		/// <returns>Returns true, if the user already exists in accounts database.</returns>
		public bool LoginAndGetUserPermissions(NetworkCredential userAccount, ref Permissions permission)
		{
			if(userAccount == null)
				throw new ArgumentNullException("userAccount is null");
			string ConnectionString = String.Format("Database=permissionservice;Data Source={0};User Id={1};Password={2}", this.MySqlAddress, this.nc.UserName, this.nc.Password);
			MySqlConnection connection = new MySqlConnection(ConnectionString);
			connection.Open();
			MySqlCommand command = connection.CreateCommand();
			command.Connection = connection;
			string query = String.Format("SELECT AccountType,permissions1_1 FROM accounts WHERE username='{0}' AND password='{1}'", userAccount.UserName, userAccount.Password);
			command.CommandText = query;
			MySqlDataReader read = command.ExecuteReader();
			if(read.Read())
			{
				bool accountType = read.GetBoolean(0);
				if(accountType)
					permission = null;
				else
				{
					if(!read.IsDBNull(1))
						permission = Permissions.FromXml(read.GetString(1));
				}
				read.Close();
				connection.Close();
				return true;
			}
			else
			{
				read.Close();
				connection.Close();
				return false;
			}

		}
		//**************************************************************************************************************//
		/// <summary>
		/// Deletes an available user account from accounts database.
		/// </summary>
		/// <param name="userAccount">The user account to delete.</param>
		public void DeleteUser(NetworkCredential userAccount)
		{
			if(userAccount == null)
				throw new ArgumentNullException("userAccount is null");
			string ConnectionString = String.Format("Database=permissionservice;Data Source={0};User Id={1};Password={2}", this.MySqlAddress, this.nc.UserName, this.nc.Password);
			MySqlConnection connection = new MySqlConnection(ConnectionString);
			connection.Open();
			MySqlCommand command = connection.CreateCommand();
			command.Connection = connection;
			string query = String.Format("DELETE FROM accounts WHERE username='{0}' AND password='{1}'", userAccount.UserName, userAccount.Password);
			command.CommandText = query;
			command.ExecuteNonQuery();
			connection.Close();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Updates permissions for the user account.
		/// </summary>
		/// <param name="newPermission">Updated new user permission.</param>
		/// <param name="userAccount">The user account to update permissions.</param>
		public void UpdateUserPermisions(Permissions newPermission, NetworkCredential userAccount)
		{
			if(newPermission == null)
				throw new ArgumentNullException("newPermission is null");
			if(userAccount == null)
				throw new ArgumentNullException("userAccount is null");
			string ConnectionString = String.Format("Database=permissionservice;Data Source={0};User Id={1};Password={2}", this.MySqlAddress, this.nc.UserName, this.nc.Password);
			MySqlConnection connection = new MySqlConnection(ConnectionString);
			connection.Open();
			MySqlCommand command = connection.CreateCommand();
			command.Connection = connection;
			string query = String.Format("UPDATE accounts SET permissions1_1='{0}' WHERE username='{1}' AND password='{2}'", userAccount.UserName, userAccount.Password, Permissions.ToXml(newPermission));
			command.CommandText = query;
			command.ExecuteNonQuery();
			connection.Close();
		}
		//**************************************************************************************************************//
	}
}