using System;

namespace DotGrid.Shared.Enums.DotDFS
{
	/// <summary>
	/// Enum for Method.
	/// </summary>
	public enum Method
	{
		/// <summary>
		/// Read to file mode.
		/// </summary>
		Read = 0,
		/// <summary>
		/// Write to file mode.
		/// </summary>
		Write = 1,
		/// <summary>
		/// Flush mode.
		/// </summary>
		Flush = 2,
		/// <summary>
		/// Lock mode.
		/// </summary>
		Lock = 3,
		/// <summary>
		/// Seek mode.
		/// </summary>
		Seek = 4,
		/// <summary>
		/// Close mode.
		/// </summary>
		Close = 5,
		/// <summary>
		/// SetLength mode.
		/// </summary>
		SetLength = 6,
		/// <summary>
		/// UnLock mode.
		/// </summary>
		UnLock = 7,
		/// <summary>
		/// Position mode.
		/// </summary>
		Position = 8,
		/// <summary>
		/// Length mode.
		/// </summary>
		Length = 9,
		/// <summary>
		/// CanSeek mode.
		/// </summary>
		CanSeek = 10
	}
}
