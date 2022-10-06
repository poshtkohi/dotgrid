/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

using DotGrid.Shared.Enums.DotDFS;

namespace DotGrid.Shared.Headers.DotDFS
{
	/// <summary>
	/// A class for SetLengthHeader.
	/// </summary>
	public class SetLengthHeader
	{
		private ulong value;
		/// <summary>
		/// Initializes a new instance of the SetLengthHeader class.
		/// </summary>
		/// <param name="value">The new length of the stream.</param>
		//**************************************************************************************************************//
		public SetLengthHeader(long value)
		{
			if(value < 0) throw new ArgumentOutOfRangeException("None-negative number is required.");
			this.value = (ulong)value;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of SetLengthHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				byte[] ValueBuffer = LongValueHeader.GetBytesOfLongNumber(this.value);
				// (Mode,Method)[1] + (value)[l] 
				byte[] buffer = new byte[1 + ValueBuffer.Length];
				buffer[0] = (byte)Method.SetLength; // Method
				buffer[0] |= (byte)(ValueBuffer.Length << 4);  // Mode
				for(int i = 0 ; i < ValueBuffer.Length ; i++)
					buffer[1 + i] = ValueBuffer[i];
				ValueBuffer = null;
				return buffer;
			}
		}
		//**************************************************************************************************************//
	}
}