/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.Shared.Enums.DotDFS
{
	/// <summary>
	/// Enum for PathMode.
	/// </summary>
	public enum PathMode
	{
		/// <summary>
		/// Meaning l = 1   (l is numbers of bytes of PathLength field.)
		/// </summary>
		INT8 = 1,
		/// <summary>
		/// Meaning l = 2   (l is numbers of bytes of PathLength field.)
		/// </summary>
		INT16 = 2,
		/// <summary>
		/// Meaning l = 3   (l is numbers of bytes of PathLength field.)
		/// </summary>
		INT24 = 3,
		/// <summary>
		/// Meaning l = 4   (l is numbers of bytes of PathLength field.)
		/// </summary>
		INT32 = 4
	}
}