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
	/// A class for FileStreamHeader.
	/// </summary>
	public class FileStreamHeader
	{
		private string path;
		private FileMode mode;
		private FileAccess access;
		private FileShare share;
		private PathEncoding encoding;
		/// <summary>
		/// Initializes a new instance of the FileStreamHeader class.
		/// </summary>
		/// <param name="path">A relative or absolute path for the file that the current FileStream object will encapsulate.</param>
		/// <param name="mode">A FileMode constant that determines how to open or create the file.</param>
		/// <param name="access">A FileAccess constant that determines how the file can be accessed by the FileStream object.</param>
		/// <param name="share">A FileShare constant that determines how the file will be shared by processes.</param>
		/// <param name="encoding">A PathEncoding constant that determines how the path file will be encoded by processes.</param>
		//**************************************************************************************************************//
		public FileStreamHeader(string path, FileMode mode, FileAccess access, FileShare share, PathEncoding encoding)
		{
			this.path = path;
			this.mode = mode;
			this.access = access;
			this.share = share;
			this.encoding = encoding;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// A relative or absolute path for the file that the current FileStream object will encapsulate.
		/// </summary>
		public string Path
		{
			get
			{
				return this.path;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// A FileMode constant that determines how to open or create the file.
		/// </summary>
		public FileMode FileMode
		{
			get
			{
				return this.mode;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// A FileAccess constant that determines how the file can be accessed by the FileStream object.
		/// </summary>
		public FileAccess FileAccess
		{
			get
			{
				return this.access;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// A FileShare constant that determines how the file will be shared by processes.
		/// </summary>
		public FileShare FileShare
		{
			get
			{
				return this.share;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// A PathEncoding constant that determines how the path file will be encoded by processes
		/// </summary>
		public PathEncoding PathEncoding
		{
			get
			{
				return this.encoding;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of FileStreamHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				if(path.Length > Int32.MaxValue)
					throw new ArgumentOutOfRangeException("Path length is greater than 4294967295");
				PathMode _PathMode = PathMode.INT8;
				if(path.Length <= 255)
					_PathMode = PathMode.INT8;
				if(path.Length > 255 && path.Length <= 65535)
					_PathMode = PathMode.INT16;
				if(path.Length > 65535 && path.Length <= 16777215)
					_PathMode = PathMode.INT24;
				if(path.Length > 16777215 && path.Length <= Int32.MaxValue)
					_PathMode = PathMode.INT32;
				int encoding_num = 1;
				if(encoding == PathEncoding.ASCII)
					encoding_num = 1;
				else
					encoding_num = 2;
				//(PathMode,PathEncoding)+FileMode+FileAccess+FileShare+_PathMode+n*encoding_num
				byte[] buffer = new byte[4 + (int)_PathMode  + path.Length * encoding_num];
				buffer[0] = (byte)encoding; //PathEncoding
				buffer[0] |= (byte)((int)_PathMode << 4);  //PathMode
				switch(_PathMode) //PathLength
				{
					case PathMode.INT8:
						buffer[1] = (byte)path.Length;
						break;
					case PathMode.INT16:
						buffer[1] = (byte)((path.Length & 0xFF00) >> 8);
						buffer[2] = (byte) (path.Length & 0x00FF);
						break;
					case PathMode.INT24:
						buffer[1] = (byte)((path.Length & 0xFF0000) >> 16);
						buffer[2] = (byte)((path.Length & 0x00FF00) >> 8);
						buffer[3] = (byte) (path.Length & 0x0000FF);
						break;
					case PathMode.INT32:
						buffer[1] = (byte)((path.Length & 0xFF000000) >> 24);
						buffer[2] = (byte)((path.Length & 0x00FF0000) >> 16);
						buffer[3] = (byte)((path.Length & 0x0000FF00) >> 8);
						buffer[4] = (byte) (path.Length & 0x000000FF);
						break;
					default:
						throw new Exception("Path length is greater than 4294967295");
				}
				buffer[(int)_PathMode + 1] = (byte)mode;   //FileMode
				buffer[(int)_PathMode + 2] = (byte)access; //FileAccess
				buffer[(int)_PathMode + 3] = (byte)share;  //FileShare
				switch(encoding)
				{
					case PathEncoding.ASCII:
						System.Text.ASCIIEncoding.ASCII.GetBytes(path,0, path.Length, buffer, (int)_PathMode + 4);
						break;
					case PathEncoding.UTF7:
						System.Text.UTF7Encoding.UTF7.GetBytes(path,0, path.Length, buffer, (int)_PathMode + 4);
						break;
					case PathEncoding.UTF8:
						System.Text.UTF8Encoding.UTF8.GetBytes(path,0, path.Length, buffer, (int)_PathMode + 4);
						break;
					case PathEncoding.UTF16:
						System.Text.UnicodeEncoding.Unicode.GetBytes(path,0, path.Length, buffer, (int)_PathMode + 4);
						break;
					default:
						buffer = null;
						throw new ArgumentException("PathEncoding is not supported.");
				}
				return buffer;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Finds a FileStreamHeader instance from the inpit buffer, if there are any error then the method trow an exception with detailed error message. 
		/// </summary>
		/// <param name="buffer">Input buffer for contructing a FileStreamHeader instance.</param>
		/// <returns>A FileStreamHeader instance built based on input buffer.</returns>
		public static FileStreamHeader GetHeader(byte[] buffer)
		{
			//try
			if(buffer == null)
				throw new ArgumentNullException("The input buffer is null.");
			if(buffer.Length < 6)
				throw new ArgumentOutOfRangeException("The buffer length at least must be 6.");
			byte _PathMode = (byte)((buffer[0] & 0xF0) >> 4);
			if(_PathMode != (byte)PathMode.INT8 && _PathMode != (byte)PathMode.INT16 && _PathMode != (byte)PathMode.INT24 && _PathMode != (byte)PathMode.INT32)
				throw new Exception("Value of PathMode field is no supporetd.");
			byte _PathEncoding = (byte)(buffer[0] & 0x0F);
			if(_PathEncoding != (byte)PathEncoding.ASCII && _PathEncoding != (byte)PathEncoding.UTF7 && _PathEncoding != (byte)PathEncoding.UTF8 && _PathEncoding > (byte)PathEncoding.UTF16)
				throw new Exception("Value of PathEncoding field is no supporetd.");
			byte _FileMode = 0;
			byte _FileAccess = 0;
			byte _FileShare = 0;
			string path = null;
			int n = 0; //length of the path string in the buffer
			switch(_PathMode)
			{
				case (byte)PathMode.INT8:
					n = buffer[1];
					_FileMode = buffer[2];
					_FileAccess = buffer[3];
					_FileShare = buffer[4];

					break;
				case (byte)PathMode.INT16:
					n = (buffer[1] << 8) | buffer[2];
					_FileMode = buffer[3];
					_FileAccess = buffer[4];
					_FileShare = buffer[5];
					break;
				case (byte)PathMode.INT24:
					n = (buffer[1] << 16) | (buffer[2]  << 8) | buffer[3];
					_FileMode = buffer[4];
					_FileAccess = buffer[5];
					_FileShare = buffer[6];
					break;
				case (byte)PathMode.INT32:
					n = (buffer[1] << 24) | (buffer[2]  << 16) | (buffer[3]  << 8) | buffer[4];
					_FileMode = buffer[5];
					_FileAccess = buffer[6];
					_FileShare = buffer[7];
					break;
			}
			if(_FileMode != (byte)FileMode.Append && _FileMode != (byte)FileMode.Create && _FileMode != (byte)FileMode.CreateNew && _FileMode != (byte)FileMode.Open && _FileMode != (byte)FileMode.OpenOrCreate && _FileMode != (byte)FileMode.Truncate)
				throw new  ArgumentException("Value of FileMode field is no supporetd.");
			if(_FileAccess != (byte)FileAccess.Read && _FileAccess != (byte)FileAccess.ReadWrite && _FileAccess != (byte)FileAccess.Write)
				throw new ArgumentException("Value of FileAccess field is no supporetd.");
			if(_FileShare != (byte)FileShare.Inheritable && _FileShare != (byte)FileShare.None && _FileShare != (byte)FileShare.Read && _FileShare != (byte)FileShare.ReadWrite && _FileShare != (byte)FileShare.Write)
				throw new ArgumentException("Value of FileShare field is no supporetd.");
			int encoding_num = 1;
			if(_PathEncoding == (byte)PathEncoding.ASCII)
				encoding_num = 1;
			else
				encoding_num = 2;
			//(PathMode,PathEncoding)+FileMode+FileAccess+FileShare+_PathMode+n*encoding_num
			if(4 + _PathMode  + n * encoding_num > buffer.Length)
				throw new Exception("The buffer is in inconvenient state.");
			switch(_PathEncoding)
			{
				case (byte)PathEncoding.ASCII:
					path = System.Text.ASCIIEncoding.ASCII.GetString(buffer, 4 + _PathMode, n);
					break;
				case (byte)PathEncoding.UTF7:
					path = System.Text.UTF7Encoding.UTF7.GetString(buffer, 4 + _PathMode, n);
					break;
				case (byte)PathEncoding.UTF8:
					path = System.Text.UTF8Encoding.UTF8.GetString(buffer, 4 + _PathMode, n);
					break;
				case (byte)PathEncoding.UTF16:
					path = System.Text.UnicodeEncoding.Unicode.GetString(buffer, 4 + _PathMode, n);
					break;
				default:
					throw new ArgumentException("PathEncoding is not supported.");
			}
			FileStreamHeader fsh = new FileStreamHeader(path, (FileMode)_FileMode, (FileAccess)_FileAccess, (FileShare)_FileShare, (PathEncoding)_PathEncoding);
			return fsh;
		}
		//**************************************************************************************************************//
	}
}