/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.Shared.Enums
{
	//**************************************************************************************************************//
	/// <summary>
	/// Euom for ClientAuthenticationError.
	/// </summary>
	public enum ClientAuthenticationError
	{
		/// <summary>
		/// Successfully authenticated.
		/// </summary>
		OK = 1,
		/// <summary>
		/// Username or Passwrod is wrong.
		/// </summary>
		NO = 2,
		/// <summary>
		/// Bad format for n bytes of username and m bytes of password (buffer.Length != 2+n+m and buffer.Length les than 2)
		/// </summary>
		BAD = 3
	}
	//**************************************************************************************************************//
}
