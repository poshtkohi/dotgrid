using System;

using DotGrid.Shared.Enums.DotDFS;

namespace DotGrid.Shared.Headers.DotDFS
{
	/// <summary>
	/// A class for LongValueHeader.
	/// </summary>
	public class LongValueHeader
	{
		private ulong value;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the LongValueHeader class.
		/// </summary>
		/// <param name="value">Long number value.</param>
		public LongValueHeader(long value)
		{
			if(value < 0) throw new ArgumentOutOfRangeException("None-negative number is required for value.");
			this.value = (ulong)value;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Get bytes of an unsigned long number.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <returns>Bytes of value.</returns>
		public static byte[] GetBytesOfLongNumber(ulong value)
		{
			if(value > 0xFFFFFFFFFFFFFFFF) throw new ArgumentOutOfRangeException("Length of value field is greater than 18446744073709551615.");
			LongMode _Mode = LongMode.INT8;
			if(value <= 0xFF)
				_Mode = LongMode.INT8;
			if(value > 0xFF && value <= 0xFFFF)
				_Mode = LongMode.INT16;
			if(value > 0xFFFF && value <= 0xFFFFFF)
				_Mode = LongMode.INT24;
			if(value > 0xFFFFFF && value <= 0xFFFFFFFF)
				_Mode = LongMode.INT32;
			if(value > 0xFFFFFFFF && value <= 0xFFFFFFFFFF)
				_Mode = LongMode.INT40;
			if(value > 0xFFFFFFFFFF && value <= 0xFFFFFFFFFFFF)
				_Mode = LongMode.INT48;
			if(value > 0xFFFFFFFFFFFF && value <= 0xFFFFFFFFFFFFFF)
				_Mode = LongMode.INT56;
			if(value > 0xFFFFFFFFFFFFFF && value <= 0xFFFFFFFFFFFFFFFF)
				_Mode = LongMode.INT64;
			byte[] buffer = new byte[(int)_Mode];
			switch(_Mode) //Value Length
			{
				case LongMode.INT8:
				{
					buffer[0] = (byte)value;
					break;
				}
				case LongMode.INT16:
				{
					buffer[0] = (byte)((value & 0xFF00) >> 8);
					buffer[1] = (byte) (value & 0x00FF);
					break;
				}
				case LongMode.INT24:
				{
					buffer[0] = (byte)((value & 0xFF0000) >> 16);
					buffer[1] = (byte)((value & 0x00FF00) >> 8);
					buffer[2] = (byte) (value & 0x0000FF);
					break;
				}
				case LongMode.INT32:
				{
					buffer[0] = (byte)((value & 0xFF000000) >> 24);
					buffer[1] = (byte)((value & 0x00FF0000) >> 16);
					buffer[2] = (byte)((value & 0x0000FF00) >> 8);
					buffer[3] = (byte)(value & 0x000000FF);
					break;
				}
				case LongMode.INT40:
				{
					buffer[0] = (byte)((value & 0xFF00000000) >> 32);
					buffer[1] = (byte)((value & 0x00FF000000) >> 24);
					buffer[2] = (byte)((value & 0x0000FF0000) >> 16);
					buffer[3] = (byte)((value & 0x000000FF00) >> 8);
					buffer[4] = (byte) (value & 0x00000000FF);
					break;
				}
				case LongMode.INT48:
				{
					buffer[0] = (byte)((value & 0xFF0000000000) >> 40);
					buffer[1] = (byte)((value & 0x00FF00000000) >> 32);
					buffer[2] = (byte)((value & 0x0000FF000000) >> 24);
					buffer[3] = (byte)((value & 0x000000FF0000) >> 16);
					buffer[4] = (byte)((value & 0x00000000FF00) >> 8);
					buffer[5] = (byte) (value & 0x0000000000FF);
					break;
				}
				case LongMode.INT56:
				{
					buffer[0] = (byte)((value & 0xFF000000000000) >> 48);
					buffer[1] = (byte)((value & 0x00FF0000000000) >> 40);
					buffer[2] = (byte)((value & 0x0000FF00000000) >> 32);
					buffer[3] = (byte)((value & 0x000000FF000000) >> 24);
					buffer[4] = (byte)((value & 0x00000000FF0000) >> 16);
					buffer[5] = (byte)((value & 0x0000000000FF00) >> 8);
					buffer[6] = (byte) (value & 0x000000000000FF);
					break;
				}
				case LongMode.INT64:
				{
					buffer[0] = (byte)((value & 0xFF00000000000000) >> 56);
					buffer[1] = (byte)((value & 0x00FF000000000000) >> 48);
					buffer[2] = (byte)((value & 0x0000FF0000000000) >> 40);
					buffer[3] = (byte)((value & 0x000000FF00000000) >> 32);
					buffer[4] = (byte)((value & 0x00000000FF000000) >> 24);
					buffer[5] = (byte)((value & 0x0000000000FF0000) >> 16);
					buffer[6] = (byte)((value & 0x000000000000FF00) >> 8);
					buffer[7] = (byte) (value & 0x00000000000000FF);
					break;
				}
				default:
				{
					buffer = null;
					throw new ArgumentOutOfRangeException("Length of value field is greater than 18446744073709551615.");
				}
			}
			return buffer;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Get bytes of an unsigned long number for DotDFS GridFTPMode transfer.
		/// </summary>
		/// <param name="buffer">A reference buffer with 8 bytes length.</param>
		/// <param name="value">Value.</param>
		public static void GetBytesOfLongNumberForGridFTPMode(ref byte[] buffer, ulong value)
		{
			if(value > 0xFFFFFFFFFFFFFFFF) throw new ArgumentOutOfRangeException("Length of value field is greater than 18446744073709551615.");
			if(buffer == null)
				buffer = new byte[8];
			if(buffer.Length != 8)
				throw new ArgumentOutOfRangeException("buffer length must be 8 bytes long.");
			buffer[0] = (byte)((value & 0xFF00000000000000) >> 56);
			buffer[1] = (byte)((value & 0x00FF000000000000) >> 48);
			buffer[2] = (byte)((value & 0x0000FF0000000000) >> 40);
			buffer[3] = (byte)((value & 0x000000FF00000000) >> 32);
			buffer[4] = (byte)((value & 0x00000000FF000000) >> 24);
			buffer[5] = (byte)((value & 0x0000000000FF0000) >> 16);
			buffer[6] = (byte)((value & 0x000000000000FF00) >> 8);
			buffer[7] = (byte) (value & 0x00000000000000FF);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of LongValueHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				byte[] ValueBuffer = GetBytesOfLongNumber(this.value);
				// Mode[1] + value[l] 
				byte[] buffer = new byte[1 + ValueBuffer.Length];
				buffer[0] = (byte)ValueBuffer.Length;  // Mode
				for(int i = 0 ; i < ValueBuffer.Length ; i++) // Value
					buffer[1 + i] = ValueBuffer[i];
				ValueBuffer = null;
				return buffer;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets an unsigned long number from input buffer.
		/// </summary>
		/// <param name="buffer">Input buffer.</param>
		/// <returns>Unsigned long number.</returns>
		public static ulong GetLongNumberFromBytes(byte[] buffer)
		{
			if(buffer == null) throw new ArgumentNullException("Buffer can not be null.");
			if(buffer.Length > 8) throw new ArgumentOutOfRangeException("Buffer length cannot be greater than 8 bytes.");
			switch((LongMode)buffer.Length)
			{
				case LongMode.INT8:
					return (ulong)buffer[0];
				case LongMode.INT16:
					return ((ulong)buffer[0] << 8) | (ulong)buffer[1];
				case LongMode.INT24:
					return((ulong)buffer[0] << 16) | ((ulong)buffer[1]  << 8) | (ulong)buffer[2];
				case LongMode.INT32:
					return ((ulong)buffer[0] << 24) | ((ulong)buffer[1]  << 16) | ((ulong)buffer[2]  << 8) | (ulong)buffer[3];
				case LongMode.INT40:
					return ((ulong)buffer[0] << 32) | ((ulong)buffer[1] << 24) | ((ulong)buffer[2]  << 16) | ((ulong)buffer[3]  << 8) | (ulong)buffer[4];
				case LongMode.INT48:
					return ((ulong)buffer[0] << 40) | ((ulong)buffer[1] << 32) | ((ulong)buffer[2] << 24) | ((ulong)buffer[3]  << 16) | ((ulong)buffer[4]  << 8) | (ulong)buffer[5];
				case LongMode.INT56:
					return ((ulong)buffer[0] << 48) | ((ulong)buffer[1] << 40) | ((ulong)buffer[2] << 32) | ((ulong)buffer[3] << 24) | ((ulong)buffer[4]  << 16) | ((ulong)buffer[5]  << 8) | (ulong)buffer[6];
				case LongMode.INT64:
					return ((ulong)buffer[0]  << 56) | ((ulong)buffer[1] << 48) | ((ulong)buffer[2] << 40) | ((ulong)buffer[3] << 32) | ((ulong)buffer[4] << 24) | ((ulong)buffer[5]  << 16) | ((ulong)buffer[6]  << 8) | (ulong)buffer[7];
				default: 
					throw new ArgumentOutOfRangeException("Buffer length can be less or equal than eight bytes and greater than zero.");
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets an unsigned long number from input buffer for DotDFS GridFTPMode transfer.
		/// </summary>
		/// <param name="buffer">Input buffer.</param>
		/// <returns>Unsigned long number.</returns>
		public static ulong GetLongNumberFromBytesForGridFTPMode(byte[] buffer)
		{
			if(buffer == null) throw new ArgumentNullException("Buffer can not be null.");
			if(buffer.Length != 8) throw new ArgumentOutOfRangeException("Buffer length cannot be opposite of 8 bytes.");
			return ((ulong)buffer[0]  << 56) | ((ulong)buffer[1] << 48) | ((ulong)buffer[2] << 40) | ((ulong)buffer[3] << 32) | ((ulong)buffer[4] << 24) | ((ulong)buffer[5]  << 16) | ((ulong)buffer[6]  << 8) | (ulong)buffer[7];
		}
		//**************************************************************************************************************//
	}
}