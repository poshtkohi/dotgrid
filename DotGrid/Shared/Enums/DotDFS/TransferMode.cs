/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.Shared.Enums.DotDFS
{
	/// <summary>
	/// Enum for TransferMode.
	/// </summary>
	public enum TransferMode
	{
		/// <summary>
		/// The mode of this session is the proposed DotDFS Protocol headers.
		/// </summary>
		DotDFS = 1,
		/// <summary>
		/// The mode of this session is the GridFTP V1 Extended Block Mode.
		/// </summary>
		GridFTP = 2
	}
}
