/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// 
	/// </summary>
	internal class FileTransferModeHearderInfo
	{
		private long offsetSeek;
		private byte[] buffer;
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="offsetSeek"></param>
		/// <param name="buffer"></param>
		public FileTransferModeHearderInfo(long offsetSeek, byte[] buffer)
		{
			this.offsetSeek = offsetSeek;
			this.buffer = buffer;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public long SeekValue
		{
			get
			{
				return offsetSeek;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		public byte[] Data
		{
			get
			{
				return buffer;
			}
		}
		//**************************************************************************************************************//
	}
}
