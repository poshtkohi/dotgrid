/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

using DotGrid.Shared.Enums;
using DotGrid.DotSec;
using DotGrid.Shared.Enums.DotDFS;
using DotGrid.Shared.Headers.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Implements DotDfsServer daemon in DFSM mode.
	/// </summary>
	internal class FileStreamRequest
	{
		private SecureBinaryReader reader;
		private SecureBinaryWriter writer;
		private FileStream fs;
		private int tcpBufferSize = 0;// 64KB  for none secure and 32KB for secure connections
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		public FileStreamRequest(SecureBinaryReader reader, SecureBinaryWriter writer)
		{
			this.reader = reader;
			this.writer = writer;
			if(this.reader.IsSecure)
				tcpBufferSize  = 32 * 1024;
			else tcpBufferSize = 64 * 1024;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Runs and manges new DotDfs FileStream session with the underlying thread OS.
		/// </summary>
		public void Run()
		{
			Console.WriteLine("New FileStream mode.");//
			//----------------FileStreamHeader-----------------------------
			FileStreamHeader fsh = null;
			try { fsh = GetFileStreamHeader(); if(fsh == null){ThreadExit(); return ;} } 
			catch(Exception e) { Send((new ExceptionHandlingHeader(e)).Buffer); ThreadExit(); return ; }
			//----------------FileStream-----------------------------------
			fs = null;
			try { fs = new FileStream(fsh.Path, fsh.FileMode, fsh.FileAccess, fsh.FileShare); }
			catch(Exception e) { Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer); ThreadExit(); return ; }
			if(Send((byte) eXception.NO) == -1) { ThreadExit(); return ; } //response to client that no excpetion has occured.
			//----------------Main Session---------------------------------
			while(true)
			{
				int b = ReceiveByte();
				if(b == -1) { ThreadExit(); return ; } // timeout or closed connection
				if(b != -1)
				{
					switch((Method)(b & 0x0F))
					{
						case Method.Write:
							if(Write(b) == -1) { ThreadExit(); return ; }
							//fs.Flush();
							break;
						case Method.Read:
							if(Read(b) == -1) { ThreadExit(); return ; }
							break;
						case Method.Flush:
							if(Flush() == -1) { ThreadExit(); return ; }
							break;
						case Method.Close:
							Close();
							ThreadExit();
							return ;
						case Method.SetLength:
							if(SetLength(b) == -1) { ThreadExit(); return ; }
							break;
						case Method.CanSeek:
							if(CanSeek() == -1) { ThreadExit(); return ; }
							break;
						case Method.Lock:
							if(LockOrUnlock(b, true) == -1) { ThreadExit(); return ; }
							break;
						case Method.UnLock:
							if(LockOrUnlock(b, false) == -1) { ThreadExit(); return ; }
							break;
						case Method.Seek:
							if(Seek(b) == -1) { ThreadExit(); return ; }
							break;
						case Method.Position:
							if(PositionOrLength(true) == -1) { ThreadExit(); return ; }
							break;
						case Method.Length:
							if(PositionOrLength(false) == -1) { ThreadExit(); return ; }
							break;
						default:
							//Console.WriteLine("Not Supported method. b: {0}.", b);//
							if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not Supported method."))).Buffer) == -1) { ThreadExit(); return ; }
							continue;
					}
				}
				//GC.Collect();
				//Thread.Sleep(1); // only for giga-bit networks no for 100mbs networks
			}
			//-------------------------------------------------------------
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Read buffer from FileStream instance and writes it to network stream.
		/// </summary>
		/// <param name="b">One last received byte.</param>
		/// <returns>Error code.</returns>
		private int Read(int b)
		{
			int n = 0;
			RWMode _RWMode = (RWMode)(((byte)b & 0xF0) >> 4);
			byte[] buffer = null;
			switch(_RWMode)
			{
				case RWMode.INT8:
					n = ReceiveByte();
					if(n == -1) 
						return -1;
					break;
				case RWMode.INT16:
				{
					buffer = Receive(2);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 2)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad RWMode format for ReadWriteHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = (buffer[0] << 8) | buffer[1];
					break;
				}
				case RWMode.INT24:
				{
					buffer = Receive(3);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 3)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad RWMode format for ReadWriteHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = (buffer[0] << 16) | (buffer[1]  << 8) | buffer[2];
					break;
				}
				case RWMode.INT32:
				{
					buffer = Receive(4);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 4)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad RWMode format for ReadWriteHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = (buffer[0] << 24) | (buffer[1]  << 16) | (buffer[2]  << 8) | buffer[3];
					break;
				}
				default: 
				{ 
					if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not supported RWMode value."))).Buffer) == -1) 
						return -1;
					else 
						return 0;
				}
			}
			if(n <= 0)
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException("RWMode field can not be negative or zero."))).Buffer) == -1) 
					return -1;
				else 
					return 0;
			}
			if(n > tcpBufferSize)
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException(String.Format("Requested buffer length can not be greater than tcpBufferSize length meaning {0} bytes.", tcpBufferSize)))).Buffer) == -1) 
					return -1;
				else 
					return 0;
			}
			//if(Send((byte) eXception.NO) == -1) return -1;
			buffer = new byte[n];
			int read = 0;
			try { read = fs.Read(buffer, 0, buffer.Length); }
			catch(Exception e) { if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1) return -1; }
			if(Send((byte) eXception.NO) == -1) 
				return -1; // informs user that the file reading was done successfully.
			ReadWriteHeader rwh = new ReadWriteHeader(read, Method.Read);
			if(Send(rwh.Buffer) == -1) 
				return -1;
			if(read > 0)
			{
				if(Send(buffer, 0, read) == -1) 
					return -1; // send buffer to client
				//if(Send((byte) eXception.NO) == -1) return -1;
			}
			return 0;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Receives buffer and writes it to FileStream instance.
		/// </summary>
		/// <param name="b">One last received byte.</param>
		/// <returns>Error code.</returns>
		private int Write(int b)
		{
			int n = 0;
			RWMode _RWMode = (RWMode)(((byte)b & 0xF0) >> 4);
			byte[] buffer = null;
			switch(_RWMode)
			{
				case RWMode.INT8:
					n = ReceiveByte();
					if(n == -1) return -1;
					break;
				case RWMode.INT16:
				{
					buffer = Receive(2);
					if(buffer == null || buffer.Length != 2) return -1;
					n = (buffer[0] << 8) | buffer[1];
					break;
				}
				case RWMode.INT24:
				{
					buffer = Receive(3);
					if(buffer == null || buffer.Length != 3) return -1;
					n = (buffer[0] << 16) | (buffer[1]  << 8) | buffer[2];
					break;
				}
				case RWMode.INT32:
				{
					buffer = Receive(4);
					if(buffer == null || buffer.Length != 4) return -1;
					n = (buffer[0] << 24) | (buffer[1]  << 16) | (buffer[2]  << 8) | buffer[3];
					break;
				}
				default: 
				{ 
					if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad RWMode format."))).Buffer) == -1) return -1;
					else return 0;
				}
			}
			if(n <= 0)
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException("RWMode field can not be negative or zero."))).Buffer) == -1) return -1;
				else return 0;
			}
			if(n > tcpBufferSize)
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException(String.Format("Requested buffer length can not be greater than tcpBufferSize length meaning {0} bytes.", tcpBufferSize)))).Buffer) == -1) 
					return -1;
				else 
					return 0;
			}
			if(Send((byte) eXception.NO) == -1) return -1; 
			int m = 0;
			int e = 0;
			buffer = new byte[n];
			//Console.WriteLine("n:" + n);
			while(n - m > 0)
			{
				if((e = Receive(buffer, m, n - m)) == -1) return -1;
				m += e;
			}
			try { fs.Write(buffer, 0, buffer.Length); }
			catch(Exception eX) 
			{
				if(Send((new ExceptionHandlingHeader(eX.GetBaseException())).Buffer) == -1) return -1; 
			}
			if(Send((byte)eXception.NO) == -1) return -1;
			else return 0;
			/*if(n <= tcpBufferSize)
			{
				buffer = Receive(n);
				if(buffer == null) return -1;
				try { fs.Write(buffer, 0, buffer.Length); }
				catch(Exception e) 
				{
					if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1) return -1; 
				}
				if(Send((byte)eXception.NO) == -1) return -1;
				else return 0;
			}
			else
			{
				int i = 0;
				int a = n / tcpBufferSize;
				int q = n % tcpBufferSize;
				while(true)
				{
					buffer = Receive(tcpBufferSize);
					//Console.WriteLine(buffer.Length);
					if(buffer == null) return -1;
					try {fs.Write(buffer, 0, buffer.Length); }
					catch(Exception e) 
					{
						if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1) return -1; 
					}
					if(Send((byte)eXception.NO) == -1) return -1;
					i++;
					if(q != 0 && i == a)
					{
						buffer = Receive(q);
						if(buffer == null) return -1;
						try { fs.Write(buffer, 0, buffer.Length); }
						catch(Exception e) 
						{
							if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1) return -1; 
						}
						if(Send((byte)eXception.NO) == -1) return -1;
						break;
					}
					if(q == 0 && i == a)
					{
						break;
					}
					Thread.Sleep(1);
				}
				return 0;
			}*/
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		/// <returns>Error code.</returns>
		private int Flush()
		{
			try { fs.Flush(); }
			catch(Exception e) { if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1) return -1; }
			if(Send((byte)eXception.NO) == -1) 
				return -1;
			else 
				return 0;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Closes the file and releases any resources associated with the current file stream.
		/// </summary>
		/// <returns></returns>
		private void Close()
		{
			try { fs.Close(); }
			catch(Exception e) { if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1) return ; }
			Send((byte)eXception.NO);
			return ;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sets the length of this stream to the given value.
		/// </summary>
		/// <param name="b">One last received byte.</param>
		/// <returns>Error code.</returns>
		private int SetLength(int b)
		{
			ulong n = 0;
			LongMode _Mode = (LongMode)(((byte)b & 0xF0) >> 4);
			byte[] buffer = null;
			switch(_Mode)
			{
				case LongMode.INT8:
					int bb = ReceiveByte();
					if(bb == -1) 
						return -1;
					n = (ulong)bb;
					break;
				case LongMode.INT16:
				{
					buffer = Receive(2);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 2)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad LongMode format for SetLengthHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = ((ulong)buffer[0] << 8) | (ulong)buffer[1];
					break;
				}
				case LongMode.INT24:
				{
					buffer = Receive(3);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 3)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad LongMode format for SetLengthHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = ((ulong)buffer[0] << 16) | ((ulong)buffer[1]  << 8) | (ulong)buffer[2];
					break;
				}
				case LongMode.INT32:
				{
					buffer = Receive(4);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 4)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad LongMode format for SetLengthHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = ((ulong)buffer[0] << 24) | ((ulong)buffer[1]  << 16) | ((ulong)buffer[2]  << 8) | (ulong)buffer[3];
					break;
				}
				case LongMode.INT40:
				{
					buffer = Receive(5);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 5)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad LongMode format for SetLengthHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = ((ulong)buffer[0] << 32) | ((ulong)buffer[1] << 24) | ((ulong)buffer[2]  << 16) | ((ulong)buffer[3]  << 8) | (ulong)buffer[4];
					break;
				}
				case LongMode.INT48:
				{
					buffer = Receive(6);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 6)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad LongMode format for SetLengthHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = ((ulong)buffer[0] << 40) | ((ulong)buffer[1] << 32) | ((ulong)buffer[2] << 24) | ((ulong)buffer[3]  << 16) | ((ulong)buffer[4]  << 8) | (ulong)buffer[5];
					break;
				}
				case LongMode.INT56:
				{
					buffer = Receive(7);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 7)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad LongMode format for SetLengthHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = ((ulong)buffer[0] << 48) | ((ulong)buffer[1] << 40) | ((ulong)buffer[2] << 32) | ((ulong)buffer[3] << 24) | ((ulong)buffer[4]  << 16) | ((ulong)buffer[5]  << 8) | (ulong)buffer[6];
					break;
				}
				case LongMode.INT64:
				{
					buffer = Receive(8);
					if(buffer == null) 
						return -1;
					if(buffer.Length != 8)
					{
						if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad LongMode format for SetLengthHeader."))).Buffer) == -1) 
							return -1;
						return 0;
					}
					n = ((ulong)buffer[0]  << 56) | ((ulong)buffer[1] << 48) | ((ulong)buffer[2] << 40) | ((ulong)buffer[3] << 32) | ((ulong)buffer[4] << 24) | ((ulong)buffer[5]  << 16) | ((ulong)buffer[6]  << 8) | (ulong)buffer[7];
					break;
				}
				default: 
				{ 
					if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not supported LongMode format for SetLengthHeader."))).Buffer) == -1) 
						return -1;
					else return 0;
				}
			}
			if(n < 0)
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException("LongMode field for SetLength header can not be negative."))).Buffer) == -1) 
					return -1;
				else 
					return 0;
			}
			try { fs.SetLength((long)n); }
			catch(Exception e) { if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1) return -1; }
			if(Send((byte) eXception.NO) == -1) 
				return -1; // informs user that the file reading was done successfully.
			return 0;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Lock or Unlock the FileStream instance.
		/// </summary>
		/// <param name="b">One last received byte.</param>
		/// <param name="Lock">If Lock is true then this header will be LockHeader, otherwise is UnlockHeader.</param>
		/// <returns>Error code.</returns>
		private int LockOrUnlock(int b, bool Lock)
		{
			LongMode _Mode1 = (LongMode)(((byte)b & 0xF0) >> 4);
			int bb = ReceiveByte();
			if(bb == -1) 
				return -1;
			LongMode _Mode2 = (LongMode)bb;
			if(Enum.IsDefined(typeof(LongMode), _Mode1) && Enum.IsDefined(typeof(LongMode), _Mode2))
			{
				byte[] PositionBuffer = Receive((int) _Mode1);
				if(PositionBuffer == null) 
					return -1;
				if(PositionBuffer.Length != (int) _Mode1) 
					if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad format for LongUnlockHeader."))).Buffer) == -1) 
						return -1;
				byte[] LengthBuffer = Receive((int) _Mode2);
				if(LengthBuffer == null) 
					return -1;
				if(LengthBuffer.Length != (int) _Mode2) 
					if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad format for LongUnlockHeader."))).Buffer) == -1) 
						return -1;
				try 
				{
					if(Lock) 
						fs.Lock((long)LongValueHeader.GetLongNumberFromBytes(PositionBuffer), (long)LongValueHeader.GetLongNumberFromBytes(LengthBuffer));
					else 
						fs.Unlock((long)LongValueHeader.GetLongNumberFromBytes(PositionBuffer), (long)LongValueHeader.GetLongNumberFromBytes(LengthBuffer));
				}
				catch(Exception e) { if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1) return -1; }
			}
			else { if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not supported LongMode for LongUnlockHeader."))).Buffer) == -1) return -1; }
			if(Send((byte) eXception.NO) == -1) 
				return -1;
			return 0;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sets the current position of this stream to the given value.
		/// </summary>
		/// <param name="b">One last received byte.</param>
		/// <returns>Error code.</returns>
		private int Seek(int b)
		{
			LongMode _Mode = (LongMode)(((byte)b & 0xF0) >> 4);
			if(Enum.IsDefined(typeof(LongMode), _Mode))
			{
				byte[] OffsetBuffer = Receive((int) _Mode);
				if(OffsetBuffer == null) 
					return -1;
				int bb = ReceiveByte();
				if(bb == -1)
					return -1;
				SeekOrigin origin = (SeekOrigin)bb;
				if(OffsetBuffer.Length != (int) _Mode) 
					if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad format for SeekHeader."))).Buffer) == -1) 
						return -1;
				if(!Enum.IsDefined(typeof(SeekOrigin), origin))
				{
					if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Value of SeekOrigin not supported."))).Buffer) == -1) 
						return -1;
					return 0;
				}
				try 
				{
					long NewPosition = fs.Seek((long)LongValueHeader.GetLongNumberFromBytes(OffsetBuffer), origin);
					LongValueHeader lvh = new LongValueHeader(NewPosition);
					if(Send((byte) eXception.NO) == -1) 
						return -1;
					if(Send(lvh.Buffer) == -1) 
						return -1;
					OffsetBuffer = null;
					lvh = null;
				}
				catch(Exception e) { if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1)  return -1; }
			}
			else 
			{ 
				if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not supported LongMode for LongUnlockHeader."))).Buffer) == -1) 
					return -1;
			}
			return 0;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets position or Length of the current FileStream instance.
		/// </summary>
		/// <param name="Position">If Position is true then this method will find Position, otherwise Length.</param>
		/// <returns>Error code.</returns>
		private int PositionOrLength(bool Position)
		{
			try 
			{
				long PosistionOrLenth;
				if(Position)
					PosistionOrLenth = fs.Position;
				else
					PosistionOrLenth = fs.Length;
				LongValueHeader lvh = new LongValueHeader(PosistionOrLenth);
				if(Send((byte) eXception.NO) == -1) 
					return -1;
				if(Send(lvh.Buffer) == -1) 
					return -1;
				lvh = null;
			}
			catch(Exception e) { if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1)  return -1; }
			return 0;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <returns>Error code.</returns>
		private int CanSeek()
		{
			bool canSeek = false;
			try { canSeek = fs.CanSeek ;}
			catch(Exception e) { if(Send((new ExceptionHandlingHeader(e.GetBaseException())).Buffer) == -1)  return -1; }
			if(Send((byte)eXception.NO) == -1) 
				return -1;
			if(canSeek) { if(Send((byte)CanSeekEnum.TRUE) == -1) return -1; }
			else { if(Send((byte)CanSeekEnum.FALSE) == -1) return -1; }
			return 0;
		}
		//**************************************************************************************************************//
		private FileStreamHeader GetFileStreamHeader()
		{
			byte[] buffer = Receive(4096);
			return FileStreamHeader.GetHeader(buffer);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Exits current client connection and release all system resources.
		/// </summary>
		private void ThreadExit()
		{
			if(this.reader != null)
			{
				try { this.reader.Close(); }
				catch {}
			}
			if(fs != null) { fs.Close(); fs = null;}
			this.reader = null;
			this.writer = null;
			Console.WriteLine("Exit FileStream mode.");//
			GC.Collect();
			return ;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Send error or success messages to dfsClient.
		/// </summary>
		/// <param name="error">Error code.</param>
		private int SendErrorMessage(byte error)
		{
			return Send(error);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a maximum of n bytes from the current network stream into buffer and return it.
		/// </summary>
		/// <param name="n">states n bytes for reading from network stream</param>
		/// <returns>Return received buffer data, if there arent no data then null will be returned.</returns>
		private byte[] Receive(int n)
		{
			try
			{
				int m = 0;
				byte[] buffer = new byte[n];
				if((m = this.reader.Read(buffer, 0, buffer.Length)) == 0)
				{
					buffer = null;
					return null;
				}
				else
				{
					byte[] temp = new byte[m];
					for(int i = 0 ; i < m ; i++)
						temp[i] = buffer[i];
					buffer = null;
					return temp;
				}
			}
			catch
			{
				return null;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads count bytes from the stream with offset as the starting point in the byte array.
		/// </summary>
		/// <param name="array">The array to read data into.</param>
		/// <param name="offset">The starting point in the buffer at which to begin reading into the array.</param>
		/// <param name="count">The number of bytes to read.</param>
		/// <returns>The number of characters read into buffer. This might be less than the number of bytes requested if that many bytes are not available, or it might be -1 if there is an error.</returns>
		private int Receive(byte[] array, int offset, int count)
		{
			try
			{
				int m = 0;
				if((m = this.reader.Read(array, offset, count)) == 0)
				{
					return -1;
				}
				else
				{
					return m;
				}
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a bytes from the current network stream into buffer and return it.
		/// </summary>
		/// <returns>Return received buffer data.</returns>
		private int ReceiveByte()
		{
			try
			{
				return (int) this.reader.ReadByte();
			}
			catch
			{
				return -1;
			}
			/*int timeout = 0;
			int response = 0;
			while(true)
			{
				try
				{
					if((response = this.reader.Read()) != -1)
						return response;
				}
				catch
				{
					return -1;
				}
				timeout++;
				if(timeout >= _timeout)
					return -1;
				Thread.Sleep(1);
			}*/
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes to the network stream.
		/// </summary>
		/// <param name="buffer">buffer for writting to network stream.</param>
		/// <returns>If any errors occured, -1 will be returned otherwise 0.</returns>
		private int Send(byte[] buffer)
		{
			try
			{
				this.writer.Write(buffer, 0, buffer.Length);
				//this.writer.Flush();
				return 0;
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes to the network stream.
		/// </summary>
		/// <param name="buffer">buffer for writting to network stream.</param>
		/// <returns>If any errors occured, -1 will be returned otherwise 0.</returns>
		private int Send(byte buffer)
		{
			try
			{
				this.writer.Write(buffer);
				//this.writer.Flush();
				return 0;
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes to the network stream.
		/// </summary>
		/// <param name="buffer">buffer for writting to network stream.</param>
		/// <param name="offset">The starting point in buffer at which to begin writing. </param>
		/// <param name="count">The number of bytes to write.</param>
		/// <returns>If any errors occured, -1 will be returned otherwise 0.</returns>
		private int Send(byte[] buffer, int offset, int count)
		{
			try
			{
				this.writer.Write(buffer, offset, count);
				//this.writer.Flush();
				return 0;
			}
			catch
			{
				return -1;
			}
		}
		//**************************************************************************************************************//
	}
}