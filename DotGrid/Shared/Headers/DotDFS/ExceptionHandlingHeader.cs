/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using DotGrid.Shared.Enums;

namespace DotGrid.Shared.Headers.DotDFS
{
	/// <summary>
	/// A class for ExceptionHandlingHeader.
	/// </summary>
	public class ExceptionHandlingHeader
	{
		private Exception _e;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the ExceptionHandlingHeader class.
		/// </summary>
		/// <param name="e">An dropped exception by the system.</param>
		public ExceptionHandlingHeader(Exception e)
		{
			this._e = e;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of ExceptionHandlingHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				MemoryStream ms = new MemoryStream();   //serialize the exception instance
				IFormatter formatter = new BinaryFormatter();
				formatter.Serialize(ms, _e);
				if(ms.Length > 4294967295)
				{
					ms.Close();
					throw new ArgumentOutOfRangeException("Length of bytes of serialaized exception is greater than " + Int32.MaxValue.ToString() + ".");
				}
				byte[] holder = ms.GetBuffer();
				//Console.WriteLine("holder: {0}", holder.Length);
				ms.Close();
				formatter = null;
				ms = null;
				EMode _EMode = EMode.INT8;
				if(holder.Length <= 255)
					_EMode = EMode.INT8;
				if(holder.Length > 255 && holder.Length <= 65535)
					_EMode = EMode.INT16;
				if(holder.Length > 65535 && holder.Length <= 16777215)
					_EMode = EMode.INT24;
				if(holder.Length > 16777215 && holder.Length <= Int32.MaxValue)
					_EMode = EMode.INT32;
				//(EMode,E)+ELength+EData
				byte[] buffer = new byte[1 + (int)_EMode  + holder.Length];
				buffer[0] = (byte)eXception.OK; //E
				buffer[0] |= (byte)((int)_EMode << 4);  //EMode
				switch(_EMode) //PathLength
				{
					case EMode.INT8:
						buffer[1] = (byte)holder.Length;
						break;
					case EMode.INT16:
						buffer[1] = (byte)((holder.Length & 0xFF00) >> 8);
						buffer[2] = (byte) (holder.Length & 0x00FF);
						break;
					case EMode.INT24:
						buffer[1] = (byte)((holder.Length & 0xFF0000) >> 16);
						buffer[2] = (byte)((holder.Length & 0x00FF00) >> 8);
						buffer[3] = (byte) (holder.Length & 0x0000FF);
						break;
					case EMode.INT32:
						buffer[1] = (byte)((holder.Length & 0xFF000000) >> 24);
						buffer[2] = (byte)((holder.Length & 0x00FF0000) >> 16);
						buffer[3] = (byte)((holder.Length & 0x0000FF00) >> 8);
						buffer[4] = (byte) (holder.Length & 0x000000FF);
						break;
					default:
						holder = buffer = null;
						throw new ArgumentOutOfRangeException("Length of bytes of serialaized exception is greater than " + Int32.MaxValue.ToString() + ".");
				}
				for(int i = 0 ; i < holder.Length; i++)
					buffer[(int)_EMode + 1 + i] = holder[i];
				holder = null;
				//Console.WriteLine("buffer server: {0}", buffer.Length);
				return buffer;
			}
		}
		//**************************************************************************************************************//
	}
}
