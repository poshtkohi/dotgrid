/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;

using DotGrid.Shared.Enums.DotDFS;

namespace DotGrid.Shared.Headers.DotDFS
{
	/// <summary>
	/// A class for SeekHeader.
	/// </summary>
	public class SeekHeader
	{
		private ulong offset;
		private SeekOrigin origin;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the LockUnlockHeader class.
		/// </summary>
		/// <param name="offset">The point relative to origin from which to begin seeking.</param>
		/// <param name="origin">Specifies the beginning, the end, or the current position as a reference point for origin, using a value of type SeekOrigin.</param>
		public SeekHeader(long offset, SeekOrigin origin)
		{
			if(offset < 0) throw new ArgumentOutOfRangeException("None-negative number is required for offset.");
			if(!Enum.IsDefined(typeof(SeekOrigin), origin)) 
				throw new ArgumentException("Not supported value for origin.");
			this.offset = (ulong)offset;
			this.origin = origin;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of LockUnlockHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				byte[] OffsetBuffer = LongValueHeader.GetBytesOfLongNumber(this.offset);
				// (Mode,Method)[1] + (offset)[l] + origin[1] 
				byte[] buffer = new byte[2 + OffsetBuffer.Length];
				buffer[0] = (byte)Method.Seek; // Method
				buffer[0] |= (byte)(OffsetBuffer.Length << 4);  // Mode
				for(int i = 0 ; i < OffsetBuffer.Length ; i++)
					buffer[1 + i] = OffsetBuffer[i];
				buffer[1 + OffsetBuffer.Length] = (byte) this.origin;
				OffsetBuffer = null;
				return buffer;
			}
		}
		//**************************************************************************************************************//
	}
}
