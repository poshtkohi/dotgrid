/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.Shared.Enums
{
	/// <summary>
	/// Enum for Method.
	/// </summary>
	public enum DotRemoteProcessMethod
	{
		/// <summary>
		/// Specifies Start method.
		/// </summary>
		Start = 0,
		/// <summary>
		/// Specifies Kill method.
		/// </summary>
		Kill = 1,
		/// <summary>
		/// Specifies Refresh method.
		/// </summary>
		Refresh = 2,
		/// <summary>
		/// Specifies RemoteProcessInfo property.
		/// </summary>
		RemoteProcessInfo = 3,
		/// <summary>
		/// Specifies RemoteProcess.ExitTime property.
		/// </summary>
		ExitTime = 4,
		/*/// <summary>
		/// Specifies GetRemoteProcessById method.
		/// </summary>
		GetRemoteProcessById = 5,*/
		/// <summary>
		/// Specifies GetRemoteProcess method.
		/// </summary>
		GetRemoteProcesses = 5,
		/// <summary>
		/// Specifies KillById method.
		/// </summary>
		KillById = 6,
		/// <summary>
		/// Specifies GetRemoteProcessInfoById method.
		/// </summary>
		GetRemoteProcessInfoById = 7,
	}
}
