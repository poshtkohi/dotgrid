/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Xml;
using System.Collections;

namespace DotGrid.DotSec.Permission
{
	/// <summary>
	/// Represents complete .NET Code Access Security (CAS) Runtime permissions.
	/// </summary>
	[Serializable]
	public class Permissions
	{
		private bool odbc = false;
		private bool oledb = false;
		private bool oracle = false;
		private bool sql = false;
		private bool eventlog = false;
		private bool performanceCounter = false;
		private bool directoryServices = false;
		private bool priniting = false;
		private bool messageQueue = false;
		private bool dns = false;
		private bool socket = false;
		private bool environment = false;
		private bool filedialog = false;
		private bool fileIO = false;
		private bool islolatedStoragefile = false;
		private bool principal = false;
		private bool publisherIdentify = false;
		private bool reflection = false;
		private bool registry = false;
		private bool assertion = false;
		private bool bindingRedirects = false;
		private bool controlAppDomain = false;
		private bool controlDomainPolicy = false;
		private bool controlEvidence = false;
		private bool controlPolicy = false;
		private bool controlPrincipal = false;
		private bool controlThread = false;
		private bool execution = false;
		private bool infrastructure = false;
		private bool remotingConfiguration = false;
		private bool serializationFormatter = false;
		private bool skipVerification = false;
		private bool unmanagedCode = false;
		private bool siteIdentify = false;
		private bool strongNameIdentify = false;
		private bool ui = false;
		private bool urlIdentify = false;
		private bool zoneIdentify = false;
		private bool serviceController = false;
		private bool aspNetHosting = false;
		/// <summary>
		/// Constructor for Permissions class.
		/// </summary>
		public Permissions()
		{
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the .NET Framework Data Provider for ODBC to ensure that a user has a security level adequate to access an ODBC data source.
		/// </summary>
		public bool OdbcPermission
		{
			get
			{
				return this.odbc;
			}
			set
			{
				this.odbc = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the .NET Framework Data Provider for OLE DB to ensure that a user has a security level adequate to access an OLE DB data source.
		/// </summary>
		public bool OleDbPermission
		{
			get
			{
				return this.oledb;
			
			}
			set
			{
				this.oledb = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the .NET Framework Data Provider for Oracle to ensure that a user has a security level adequate to access an Oracle database.
		/// </summary>
		public bool OraclePermission
		{
			get
			{
				return this.oracle;
			}
			set
			{
				this.oracle = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the .NET Framework Data Provider for SQL Server to ensure that a user has a security level adequate to access a data source.
		/// </summary>
		public bool SqlClientPermission
		{
			get
			{
				return this.sql;
			}
			set
			{
				this.sql = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets control of code access permissions for event logging.
		/// </summary>
		public bool EventLogPermission
		{
			get
			{
				return this.eventlog;
			}
			set
			{
				this.eventlog = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets control of code access permissions for PerformanceCounter.
		/// </summary>
		public bool PerformanceCounterPermission 
		{
			get
			{
				return this.performanceCounter;
			}
			set
			{
				this.performanceCounter = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets control of code access security permissions for System.DirectoryServices.
		/// </summary>
		public bool DirectoryServicesPermission 
		{
			get
			{
				return this.directoryServices;
			}
			set
			{ 
				this.directoryServices = value;

			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets access to printers.
		/// </summary>
		public bool PrintingPermission
		{
			get
			{
				return this.priniting;
			}
			set
			{
				this.priniting = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets control of code access permissions for messaging.
		/// </summary>
		public bool MessageQueuePermission
		{
			get
			{
				return this.messageQueue;
			}
			set
			{
				this.messageQueue = value;

			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets rights to access Domain Name System (DNS) servers on the network.
		/// </summary>
		public bool DnsPermission
		{
			get
			{
				return this.dns;
			}
			set
			{
				this.dns = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets rights to make or accept connections on a transport address.
		/// </summary>
		public bool SocketPermission
		{
			get
			{
				return this.socket;
			}
			set
			{
				this.socket = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets access to system and user environment variables.
		/// </summary>
		public bool EnvironmentPermission 
		{
			get
			{
				return this.environment;
			}
			set
			{
				this.environment = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the ability to access files or folders through a file dialog.
		/// </summary>
		public bool FileDialogPermission
		{
			get
			{
				return this.filedialog;
			}
			set
			{ 
				this.filedialog = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the ability to access files and folders.
		/// </summary>
		public bool FileIOPermission
		{
			get
			{
				return this.fileIO;
			}
			set
			{
				this.fileIO = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the allowed usage of a private virtual file system.
		/// </summary>
		public bool IsolatedStorageFilePermission
		{
			get
			{
				return this.islolatedStoragefile;
			}
			set
			{
				this.islolatedStoragefile = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets checks against the active principal (see IPrincipal) using the language constructs defined for both declarative and imperative security actions.
		/// </summary>
		public bool PrincipalPermission 
		{
			get
			{
				return this.principal;
			}
			set
			{
				this.principal = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the identity of a software publisher.
		/// </summary>
		public bool PublisherIdentityPermission
		{
			get
			{
				return this.publisherIdentify;
			}
			set
			{
				this.publisherIdentify = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets access to metadata through the System.Reflection APIs.
		/// </summary>
		public bool ReflectionPermission
		{
			get
			{
				return this.reflection;
			}
			set
			{
				this.reflection = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the ability to access registry variables.
		/// </summary>
		public bool RegistryPermission
		{
			get
			{
				return this.registry;
			}
			set
			{
				this.registry = value;
			}
		}
		//private System.Security.Permissions.SecurityPermission security;
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets
		/// </summary>
		public bool Assertion
		{
			get
			{
				return this.assertion;
			}
			set
			{
				this.assertion = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets permission to perform explicit binding redirection in the application configuration file. This includes redirection of .NET Framework assemblies that have been unified as well as other assemblies found outside the .NET Framework.
		/// </summary>
		public bool BindingRedirects
		{
			get
			{
				return this.bindingRedirects;
			}
			set
			{
				this.bindingRedirects = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets Ability to create and manipulate an AppDomain.
		/// </summary>
		public bool ControlAppDomain
		{
			get
			{
				return this.controlAppDomain;
			}
			set
			{
				this.controlAppDomain = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets ability to specify domain policy.
		/// </summary>
		public bool ControlDomainPolicy
		{
			get
			{
				return this.controlDomainPolicy;
			}
			set
			{
				this.controlDomainPolicy = value;
			}

		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets ability to provide evidence, including the ability to alter the evidence provided by the common language runtime. This is a powerful permission that should only be granted to highly trusted code.
		/// </summary>
		public bool ControlEvidence
		{
			get
			{
				return this.controlEvidence;
			}
			set
			{
				this.controlEvidence = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets ability to view and modify policy. This is a powerful permission that should only be granted to highly trusted code.
		/// </summary>
		public bool ControlPolicy
		{
			get
			{
				return this.controlPolicy;
			}
			set
			{
				this.controlPolicy = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets ability to manipulate the principal object.
		/// </summary>
		public bool ControlPrincipal
		{
			get
			{
				return this.controlPrincipal;
			}
			set
			{
				this.controlPrincipal = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets ability to use certain advanced operations on threads.
		/// </summary>
		public bool ControlThread
		{
			get
			{
				return this.controlThread;
			}
			set
			{
				this.controlThread = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets permission for the code to run. Without this permission, managed code will not be executed. This flag has no effect when used dynamically with stack modifiers such as Deny, Assert, and PermitOnly.
		/// </summary>
		public bool Execution
		{
			get
			{
				return this.execution;
			}
			set
			{
				this.execution = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets permission to plug code into the common language runtime infrastructure, such as adding Remoting Context Sinks, Envoy Sinks and Dynamic Sinks.
		/// </summary>
		public bool Infrastructure
		{
			get
			{
				return this.infrastructure;
			}
			set
			{
				this.infrastructure = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets permission to configure Remoting types and channels.
		/// </summary>
		public bool RemotingConfiguration
		{
			get
			{
				return this.remotingConfiguration;
			}
			set
			{
				this.remotingConfiguration = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets ability to provide serialization services. Used by serialization formatters.
		/// </summary>
		public bool SerializationFormatter
		{
			get
			{
				return this.serializationFormatter;
			}
			set
			{
				this.serializationFormatter = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets ability to skip verification of code in this assembly. Code that is unverifiable can be run if this permission is granted. This is a powerful permission that should be granted only to highly trusted code. This flag has no effect when used dynamically with stack modifiers such as Deny, Assert, and PermitOnly.
		/// </summary>
		public bool SkipVerification
		{
			get
			{
				return this.skipVerification;
			}
			set
			{
				this.skipVerification = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets ability to call unmanaged code. Since unmanaged code potentially allows other permissions to be bypassed, this is a dangerous permission that should only be granted to highly trusted code. It is used for such applications as calling native code using PInvoke or using COM interop.
		/// </summary>
		public bool UnmanagedCode
		{
			get
			{
				return this.unmanagedCode;
			}
			set
			{
				this.unmanagedCode = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets access flags for the security permission object.
		/// </summary>
		public System.Security.Permissions.SecurityPermissionFlag SecurityPermission
		{
			get
			{
				System.Security.Permissions.SecurityPermissionFlag result = System.Security.Permissions.SecurityPermissionFlag.NoFlags;
				if(!assertion)
					result |= System.Security.Permissions.SecurityPermissionFlag.Assertion;
				if(!bindingRedirects )
					result |= System.Security.Permissions.SecurityPermissionFlag.BindingRedirects;
				if(!controlDomainPolicy)
					result |= System.Security.Permissions.SecurityPermissionFlag.ControlDomainPolicy;
				if(!controlEvidence)
					result |= System.Security.Permissions.SecurityPermissionFlag.ControlEvidence;
				if(!controlPolicy)
					result |= System.Security.Permissions.SecurityPermissionFlag.ControlPolicy;
				if(!controlPrincipal)
					result |= System.Security.Permissions.SecurityPermissionFlag.ControlPrincipal;
				if(!controlThread)
					result |= System.Security.Permissions.SecurityPermissionFlag.ControlThread;
				if(!execution)
					result |= System.Security.Permissions.SecurityPermissionFlag.Execution;
				if(!infrastructure)
					result |= System.Security.Permissions.SecurityPermissionFlag.Infrastructure;
				if(!remotingConfiguration)
					result |= System.Security.Permissions.SecurityPermissionFlag.RemotingConfiguration;
				if(!serializationFormatter)
					result |= System.Security.Permissions.SecurityPermissionFlag.SerializationFormatter;
				if(!skipVerification)
					result |= System.Security.Permissions.SecurityPermissionFlag.SkipVerification;
				if(!unmanagedCode)
					result |= System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode;
				return result;
			}
		}
		////////////////////////////////////////////////////////////////
		/// //**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the identity permission for the Web site from which the code originates.
		/// </summary>
		public bool SiteIdentityPermission
		{
			get
			{
				return this.siteIdentify;
			}
			set
			{ 
				this.siteIdentify = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the identity permission for strong names. 
		/// </summary>
		public bool StrongNameIdentityPermission
		{
			get
			{
				return this.strongNameIdentify;
			}
			set
			{
				this.strongNameIdentify = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the permissions related to user interfaces and the clipboard.
		/// </summary>
		public bool UIPermission
		{
			get
			{
				return this.ui;
			}
			set
			{
				this.ui = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the identity permission for the URL from which the code originates.
		/// </summary>
		public bool UrlIdentityPermission
		{
			get
			{
				return this.urlIdentify;
			}
			set
			{
				this.urlIdentify = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the identity permission for the zone from which the code originates. 
		/// </summary>
		public bool ZoneIdentityPermission
		{
			get
			{
				return this.zoneIdentify;
			}
			set
			{
				this.zoneIdentify = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets control of code access security permissions for service controllers.
		/// </summary>
		public bool ServiceControllerPermission
		{
			get
			{
				return this.serviceController;
			}
			set
			{
				this.serviceController = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets access permissions in ASP.NET hosted environments.
		/// </summary>
		public bool AspNetHostingPermission
		{
			get
			{
				return this.aspNetHosting;
			}
			set
			{
				this.aspNetHosting = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Builds XML instance from permission input.
		/// </summary>
		/// <param name="permission">Thread permissions.</param>
		/// <returns>Permission XML Representation.</returns>
		public static string ToXml(Permissions permission)// Only for .NET 1.1
		{
			StringWriter sb = new StringWriter();
			sb.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?><permission>");
			sb.Write(ToXmlTag("AspNetHostingPermission", permission.AspNetHostingPermission));
			sb.Write(ToXmlTag("Assertion", permission.Assertion));
			sb.Write(ToXmlTag("BindingRedirects", permission.BindingRedirects));
			sb.Write(ToXmlTag("ControlAppDomain", permission.ControlAppDomain));
			sb.Write(ToXmlTag("ControlDomainPolicy", permission.ControlDomainPolicy));
			sb.Write(ToXmlTag("ControlEvidence", permission.ControlEvidence));
			sb.Write(ToXmlTag("ControlPolicy", permission.ControlPolicy));
			sb.Write(ToXmlTag("ControlPrincipal", permission.ControlPrincipal));
			sb.Write(ToXmlTag("ControlThread", permission.ControlThread));
			sb.Write(ToXmlTag("DirectoryServicesPermission", permission.DirectoryServicesPermission));
			sb.Write(ToXmlTag("DnsPermission", permission.DnsPermission));
			sb.Write(ToXmlTag("EnvironmentPermission", permission.EnvironmentPermission));
			sb.Write(ToXmlTag("EventLogPermission", permission.EventLogPermission));
			sb.Write(ToXmlTag("Execution", permission.Execution));
			sb.Write(ToXmlTag("FileDialogPermission", permission.FileDialogPermission));
			sb.Write(ToXmlTag("FileIOPermission", permission.FileIOPermission));
			sb.Write(ToXmlTag("Infrastructure", permission.Infrastructure));
			sb.Write(ToXmlTag("IsolatedStorageFilePermission", permission.IsolatedStorageFilePermission));
			sb.Write(ToXmlTag("MessageQueuePermission", permission.MessageQueuePermission));
			sb.Write(ToXmlTag("OdbcPermission", permission.OdbcPermission));
			sb.Write(ToXmlTag("OleDbPermission", permission.OleDbPermission));
			sb.Write(ToXmlTag("OraclePermission", permission.OraclePermission));
			sb.Write(ToXmlTag("PerformanceCounterPermission", permission.PerformanceCounterPermission));
			sb.Write(ToXmlTag("PrincipalPermission", permission.PrincipalPermission));
			sb.Write(ToXmlTag("PrintingPermission", permission.PrintingPermission));
			sb.Write(ToXmlTag("PublisherIdentityPermission", permission.PublisherIdentityPermission));
			sb.Write(ToXmlTag("ReflectionPermission", permission.ReflectionPermission));
			sb.Write(ToXmlTag("RegistryPermission", permission.RegistryPermission));
			sb.Write(ToXmlTag("RemotingConfiguration", permission.RemotingConfiguration));
			sb.Write(ToXmlTag("SerializationFormatter", permission.SerializationFormatter));
			sb.Write(ToXmlTag("ServiceControllerPermission", permission.ServiceControllerPermission));
			sb.Write(ToXmlTag("SiteIdentityPermission", permission.SiteIdentityPermission));
			sb.Write(ToXmlTag("SkipVerification", permission.SkipVerification));
			sb.Write(ToXmlTag("SocketPermission", permission.SocketPermission));
			sb.Write(ToXmlTag("SqlClientPermission", permission.SqlClientPermission));
			sb.Write(ToXmlTag("StrongNameIdentityPermission", permission.StrongNameIdentityPermission));
			sb.Write(ToXmlTag("UIPermission", permission.UIPermission));
			sb.Write(ToXmlTag("UnmanagedCode", permission.UnmanagedCode));
			sb.Write(ToXmlTag("UrlIdentityPermission", permission.UrlIdentityPermission));
			sb.Write(ToXmlTag("ZoneIdentityPermission", permission.ZoneIdentityPermission));
			sb.Write("</permission>");
			return sb.ToString();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Builds XML permissions tags.
		/// </summary>
		/// <param name="name">Name of the tag.</param>
		/// <param name="value">Value of the tag.</param>
		/// <returns></returns>
		private static string ToXmlTag(string name, bool value)
		{
			return String.Format("<{0} value=\"{1}\"/>", name, value);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Builds a Permissions instance from string xml representation.
		/// </summary>
		/// <param name="xml">a string XML Permissions instance.</param>
		/// <returns>A Permissions instance.</returns>
		public static Permissions FromXml(string xml) // Only for .NET 1.1
		{
			Permissions permission = new Permissions();
			XmlTextReader read = new XmlTextReader(new StringReader(xml));
			while(read.Read())
			{
				if(read.NodeType == XmlNodeType.Element)
					switch(read.Name)
					{
						case "AspNetHostingPermission":
							read.MoveToAttribute("value");
							permission.AspNetHostingPermission = Convert.ToBoolean(read.Value);
							break;
						case "Assertion":
							read.MoveToAttribute("value");
							permission.Assertion = Convert.ToBoolean(read.Value);
							break;
						case "BindingRedirects":
							read.MoveToAttribute("value");
							permission.BindingRedirects = Convert.ToBoolean(read.Value);
							break;
						case "ControlAppDomain":
							read.MoveToAttribute("value");
							permission.ControlAppDomain = Convert.ToBoolean(read.Value);
							break;
						case "ControlDomainPolicy":
							read.MoveToAttribute("value");
							permission.ControlDomainPolicy = Convert.ToBoolean(read.Value);
							break;
						case "ControlEvidence":
							read.MoveToAttribute("value");
							permission.ControlEvidence = Convert.ToBoolean(read.Value);
							break;
						case "ControlPolicy":
							read.MoveToAttribute("value");
							permission.ControlPolicy = Convert.ToBoolean(read.Value);
							break;
						case "ControlPrincipal":
							read.MoveToAttribute("value");
							permission.ControlPrincipal = Convert.ToBoolean(read.Value);
							break;
						case "ControlThread":
							read.MoveToAttribute("value");
							permission.ControlThread = Convert.ToBoolean(read.Value);
							break;
						case "DirectoryServicesPermission":
							read.MoveToAttribute("value");
							permission.DirectoryServicesPermission = Convert.ToBoolean(read.Value);
							break;
						case "DnsPermission":
							read.MoveToAttribute("value");
							permission.DnsPermission = Convert.ToBoolean(read.Value);
							break;
						case "EnvironmentPermission":
							read.MoveToAttribute("value");
							permission.EnvironmentPermission = Convert.ToBoolean(read.Value);
							break;
						case "EventLogPermission":
							read.MoveToAttribute("value");
							permission.EventLogPermission = Convert.ToBoolean(read.Value);
							break;
						case "Execution":
							read.MoveToAttribute("value");
							permission.Execution = Convert.ToBoolean(read.Value);
							break;
						case "FileDialogPermission":
							read.MoveToAttribute("value");
							permission.FileDialogPermission = Convert.ToBoolean(read.Value);
							break;
						case "FileIOPermission":
							read.MoveToAttribute("value");
							permission.FileIOPermission = Convert.ToBoolean(read.Value);
							break;
						case "Infrastructure":
							read.MoveToAttribute("value");
							permission.Infrastructure = Convert.ToBoolean(read.Value);
							break;
						case "IsolatedStorageFilePermission":
							read.MoveToAttribute("value");
							permission.IsolatedStorageFilePermission = Convert.ToBoolean(read.Value);
							break;
						case "MessageQueuePermission":
							read.MoveToAttribute("value");
							permission.MessageQueuePermission = Convert.ToBoolean(read.Value);
							break;
						case "OdbcPermission":
							read.MoveToAttribute("value");
							permission.OdbcPermission = Convert.ToBoolean(read.Value);
							break;
						case "OleDbPermission":
							read.MoveToAttribute("value");
							permission.OleDbPermission = Convert.ToBoolean(read.Value);
							break;
						case "OraclePermission":
							read.MoveToAttribute("value");
							permission.OraclePermission = Convert.ToBoolean(read.Value);
							break;
						case "PerformanceCounterPermission":
							read.MoveToAttribute("value");
							permission.PerformanceCounterPermission = Convert.ToBoolean(read.Value);
							break;
						case "PrincipalPermission":
							read.MoveToAttribute("value");
							permission.PrincipalPermission = Convert.ToBoolean(read.Value);
							break;
						case "PrintingPermission":
							read.MoveToAttribute("value");
							permission.PrintingPermission = Convert.ToBoolean(read.Value);
							break;
						case "PublisherIdentityPermission":
							read.MoveToAttribute("value");
							permission.PublisherIdentityPermission = Convert.ToBoolean(read.Value);
							break;
						case "ReflectionPermission":
							read.MoveToAttribute("value");
							permission.ReflectionPermission = Convert.ToBoolean(read.Value);
							break;
						case "RegistryPermission":
							read.MoveToAttribute("value");
							permission.RegistryPermission = Convert.ToBoolean(read.Value);
							break;
						case "RemotingConfiguration":
							read.MoveToAttribute("value");
							permission.RemotingConfiguration = Convert.ToBoolean(read.Value);
							break;
						case "SerializationFormatter":
							read.MoveToAttribute("value");
							permission.SerializationFormatter = Convert.ToBoolean(read.Value);
							break;
						case "ServiceControllerPermission":
							read.MoveToAttribute("value");
							permission.ServiceControllerPermission = Convert.ToBoolean(read.Value);
							break;
						case "SiteIdentityPermission":
							read.MoveToAttribute("value");
							permission.SiteIdentityPermission = Convert.ToBoolean(read.Value);
							break;
						case "SkipVerification":
							read.MoveToAttribute("value");
							permission.SkipVerification = Convert.ToBoolean(read.Value);
							break;
						case "SocketPermission":
							read.MoveToAttribute("value");
							permission.SocketPermission = Convert.ToBoolean(read.Value);
							break;
						case "SqlClientPermission":
							read.MoveToAttribute("value");
							permission.SqlClientPermission = Convert.ToBoolean(read.Value);
							break;
						case "StrongNameIdentityPermission":
							read.MoveToAttribute("value");
							permission.StrongNameIdentityPermission = Convert.ToBoolean(read.Value);
							break;
						case "UIPermission":
							read.MoveToAttribute("value");
							permission.UIPermission = Convert.ToBoolean(read.Value);
							break;
						case "UnmanagedCode":
							read.MoveToAttribute("value");
							permission.UnmanagedCode = Convert.ToBoolean(read.Value);
							break;
						case "UrlIdentityPermission":
							read.MoveToAttribute("value");
							permission.UrlIdentityPermission = Convert.ToBoolean(read.Value);
							break;
						case "ZoneIdentityPermission":
							read.MoveToAttribute("value");
							permission.ZoneIdentityPermission = Convert.ToBoolean(read.Value);
							break;
						default:
							break;
					}
			}
			read.Close();
			return permission;
		}
		//**************************************************************************************************************//
	}


}