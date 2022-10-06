/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.Shared.Enums.gDsm
{
	/// <summary>
	/// Enum for gDsmMethods.
	/// </summary>
	public enum gDsmMethods
	{
		/// <summary>
		/// Meaning new creation of new shared object.
		/// </summary>
		CreateNew = 1,
		/// <summary>
		/// Searches existing shared object with the related GUID.
		/// </summary>
		ObjectExists = 2,
	}
}