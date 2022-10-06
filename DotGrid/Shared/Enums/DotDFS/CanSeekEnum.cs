/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.Shared.Enums.DotDFS
{
	/// <summary>
	/// Enum for CanSeek.
	/// </summary>
	public enum CanSeekEnum
	{
		/// <summary>
		///  The stream supports seeking.
		/// </summary>
		TRUE = 0,
		/// <summary>
		/// false if the stream is closed or if the FileStream was constructed from an operating-system handle such as a pipe or output to the console.
		/// </summary>
		FALSE = 1
	}
}
