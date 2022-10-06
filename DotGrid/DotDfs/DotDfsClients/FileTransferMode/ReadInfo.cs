/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Specifies a class for reading file blocks to queue.
	/// </summary>
	internal class ReadInfo
	{
		private long offsetSeek;
		private byte[] buffer;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of ReadInfo class
		/// </summary>
		/// <param name="buffer">The read buffer block.</param>
		/// <param name="offsetSeek">Offset of this file block.</param>
		public ReadInfo(ref byte[] buffer, long offsetSeek)
		{
			this.offsetSeek = offsetSeek;
			this.buffer = buffer;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets buffer data of this instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				return buffer;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets buffer length of this instance.
		/// </summary>
		public int Length
		{
			get
			{
				return buffer.Length;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets offset seek value of this instance.
		/// </summary>
		public long OffsetSeek
		{
			get
			{
				return offsetSeek;
			}
		}
		//**************************************************************************************************************//
	}
}
