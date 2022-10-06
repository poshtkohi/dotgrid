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
	/// A class for for ReadWriteHeader.
	/// </summary>
	public class ReadWriteHeader
	{
		private int BufferLength;
		private Method _Method;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the ReadWriteHeader class.
		/// </summary>
		/// <param name="BufferLength">Length of buffer field.</param>
		/// <param name="method">Specify Method filed of ReadWriteHeader.</param>
		public ReadWriteHeader(int BufferLength, Method method)
		{
			this.BufferLength = BufferLength;
			this._Method = method;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of ReadWriteHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				if(BufferLength > Int32.MaxValue)
					throw new Exception("Length of bytes of buffer is greater than 4294967295.");
				RWMode _RWMode = RWMode.INT8;
				if(BufferLength <= 255)
					_RWMode = RWMode.INT8;
				if(BufferLength > 255 && BufferLength <= 65535)
					_RWMode = RWMode.INT16;
				if(BufferLength > 65535 && BufferLength <= 16777215)
					_RWMode = RWMode.INT24;
				if(BufferLength > 16777215 && BufferLength <= Int32.MaxValue)
					_RWMode = RWMode.INT32;
				//(RWMode,E)+ELength+EData
				byte[] buffer = new byte[1 + (int)_RWMode];
				buffer[0] = (byte)_Method; //Method
				buffer[0] |= (byte)((int)_RWMode << 4);  //RWMode
				switch(_RWMode) //PathLength
				{
					case RWMode.INT8:
						buffer[1] = (byte)BufferLength;
						break;
					case RWMode.INT16:
						buffer[1] = (byte)((BufferLength & 0xFF00) >> 8);
						buffer[2] = (byte)(BufferLength & 0x00FF);
						break;
					case RWMode.INT24:
						buffer[1] = (byte)((BufferLength & 0xFF0000) >> 16);
						buffer[2] = (byte)((BufferLength & 0x00FF00) >> 8);
						buffer[3] = (byte) (BufferLength & 0x0000FF);
						break;
					case RWMode.INT32:
						buffer[1] = (byte)((BufferLength & 0xFF000000) >> 24);
						buffer[2] = (byte)((BufferLength & 0x00FF0000) >> 16);
						buffer[3] = (byte)((BufferLength & 0x0000FF00) >> 8);
						buffer[4] = (byte) (BufferLength & 0x000000FF);
						break;
					default:
						buffer = null;
						throw new Exception("buffer length is greater than 4294967295.");
				}
				return buffer;
			}
		}
		//**************************************************************************************************************//
	}
}
