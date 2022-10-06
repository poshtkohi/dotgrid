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
	/// A class for LockUnlockHeader.
	/// </summary>
	public class LockUnlockHeader
	{
		private bool Lock;
		private ulong position;
		private ulong length;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the LockUnlockHeader class.
		/// </summary>
		/// <param name="Lock">If Lock is true then this header will be LockHeader, otherwise is UnlockHeader.</param>
		/// <param name="position">The beginning of the range to lock. The value of this parameter must be equal to or greater than zero (0).</param>
		/// <param name="length">The range to be locked.</param>
		public LockUnlockHeader(bool Lock, long position, long length)
		{
			if(position < 0) throw new ArgumentOutOfRangeException("None-negative number is required for position.");
			if(length < 0) throw new ArgumentOutOfRangeException("None-negative number is required for length.");
			this.Lock = Lock;
			this.position = (ulong)position;
			this.length = (ulong)length;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of LockUnlockHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				byte[] PositionBuffer = LongValueHeader.GetBytesOfLongNumber(this.position);
				byte[] LengthBuffer = LongValueHeader.GetBytesOfLongNumber(this.length);
				// (Mode1,Method)[1] + Mode2[1] + l + m 
				byte[] buffer = new byte[2 + PositionBuffer.Length + LengthBuffer.Length];
				if(Lock)    buffer[0] = (byte)Method.Lock; // Method
				else        buffer[0] = (byte)Method.UnLock; // Method
				buffer[0] |= (byte)(PositionBuffer.Length << 4);  // Mode1
				buffer[1] = (byte)LengthBuffer.Length; // Mode2
				for(int i = 0 ; i < PositionBuffer.Length ; i++)
					buffer[2 + i] = PositionBuffer[i];
				for(int i = 0 ; i < LengthBuffer.Length ; i++)
					buffer[3 + PositionBuffer.Length + i] = LengthBuffer[i];
				PositionBuffer = LengthBuffer = null;
				return buffer;
			}
		}
		//**************************************************************************************************************//
	}
}