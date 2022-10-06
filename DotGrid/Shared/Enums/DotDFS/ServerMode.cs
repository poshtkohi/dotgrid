/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.Shared.Enums.DotDFS
{
	/// <summary>
	/// Enum for ServerMode.
	/// </summary>
	internal enum ServerMode
	{
		/// <summary>
		/// The mode of this session is the normal DotDFS distributed file system.
		/// </summary>
		DFS = 1,
		/// <summary>
		/// The mode of this session is the normal DotDFS File Transfer.
		/// </summary>
		FileTransfer = 2
	}
}
