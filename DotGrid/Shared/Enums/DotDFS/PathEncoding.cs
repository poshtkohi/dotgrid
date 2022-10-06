/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.Shared.Enums.DotDFS
{
	/// <summary>
	/// Enum for PathEncoding.
	/// </summary>
	public enum PathEncoding
	{
		/// <summary>
		/// Represents an ASCII character encoding of Unicode characters.
		/// </summary>
		ASCII = 0,
		/// <summary>
		/// Represents a UTF-7 character encoding of Unicode characters.
		/// </summary>
		UTF7 = 1,
		/// <summary>
		/// Represents a UTF-8 character encoding of Unicode characters.
		/// </summary>
		UTF8 = 2,
		/// <summary>
		/// Represents a UTF-16 character encoding of Unicode characters.
		/// </summary>
		UTF16 = 3
	}
}