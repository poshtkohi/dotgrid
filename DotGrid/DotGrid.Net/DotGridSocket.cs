/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Collections;
using System.Net.Sockets;

using DotGrid;
using DotGrid.DotSec;
using DotGrid.Shared.Enums;
using DotGrid.Shared.Headers;
using DotGrid.Serialization;

namespace DotGrid.Net
{
	/// <summary>
	/// This class enables DotGrid socket operations that have low-level implementation.
	/// </summary>
	public class DotGridSocket
	{
		private Socket socket = null;
		private SecureBinaryReader reader;
		private SecureBinaryWriter writer;
		private int tcpBufferSize = 256 * 1024;// 64KB  for none secure and 32KB for secure connections based on DotSec model.
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the DotGridSocket class.
		/// </summary>
		/// <param name="reader">A SecureBinaryReader instance that implements DotSec protocol.</param>
		/// <param name="writer">A SecureBinaryWriter instance that implements DotSec protocol.</param>
		public DotGridSocket(SecureBinaryReader reader, SecureBinaryWriter writer)
		{
			if(reader == null)
				throw new ArgumentNullException("reader can not be null");
			if(writer == null)
				throw new ArgumentNullException("writer can not be null");
			this.reader = reader;
			this.writer = writer;
			if(this.reader.IsSecure)
				this.tcpBufferSize = 32 * 1024;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Determines the status of one or more sockets.
		/// </summary>
		/// <param name="checkRead">An IList of Socket instances to check for readability. </param>
		/// <param name="checkWrite">An IList of Socket instances to check for writability. </param>
		/// <param name="checkError">An IList of Socket instances to check for errors.</param>
		/// <param name="microSeconds">The time-out value, in microseconds. A -1 value indicates an infinite time-out.</param>
		public static void Select(IList checkRead,IList checkWrite,IList checkError,int microSeconds)
		{
			if(checkRead != null)	
				SelectInternal(checkRead, SelectMode.SelectRead, microSeconds);
			if(checkWrite != null)	
				SelectInternal(checkWrite, SelectMode.SelectWrite, microSeconds);
			if(checkError != null)	
				SelectInternal(checkError, SelectMode.SelectError, microSeconds);
		}

		private static void SelectInternal(IList _IList, SelectMode mode, int microSeconds)
		{
			if(_IList != null)	
			{
				for(int  i = 0 ; i < _IList.Count ; i++)
				{
					if(!((Socket)_IList[i]).Poll(microSeconds, mode))
					{
						_IList.RemoveAt(i);
						i = 0;
					}
				}
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the DotGridSocket class.
		/// </summary>
		/// <param name="socket">A Socket class for next usages.</param>
		/// <param name="reader">A SecureBinaryReader instance that implements DotSec protocol.</param>
		/// <param name="writer">A SecureBinaryWriter instance that implements DotSec protocol.</param>
		public DotGridSocket(Socket socket, SecureBinaryReader reader, SecureBinaryWriter writer)
		{
			if(socket == null)
				throw new ArgumentNullException("socket can not be null");
			if(reader == null)
				throw new ArgumentNullException("reader can not be null");
			if(writer == null)
				throw new ArgumentNullException("writer can not be null");
			this.socket = socket;
			this.reader = reader;
			this.writer = writer;
			if(this.reader.IsSecure)
				this.tcpBufferSize = 32 * 1024;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="rijndael"></param>
		public DotGridSocket(Socket socket, RijndaelEncryption rijndael)
		{
			if(socket == null)
				throw new ArgumentNullException("socket can not be null");
			this.socket = socket;
			if(rijndael != null)
			{
				NetworkStream ns = new NetworkStream(socket, FileAccess.ReadWrite, true);
				this.reader = new SecureBinaryReader(ns, rijndael, System.Text.Encoding.ASCII);
				this.writer = new SecureBinaryWriter(ns, rijndael, System.Text.Encoding.ASCII);
				this.tcpBufferSize = 32 * 1024;
			}
			else
			{
				NetworkStream ns = new NetworkStream(socket, FileAccess.ReadWrite, true);
				this.reader = new SecureBinaryReader(ns, null, System.Text.Encoding.ASCII);
				this.writer = new SecureBinaryWriter(ns, null, System.Text.Encoding.ASCII);
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets whether this stream is in secure or non-secure mode.
		/// </summary>
		public bool IsSecure
		{
			get
			{
				return this.reader.IsSecure;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Exposes access to the underlying stream of the DotGridSocket.
		/// </summary>
		public Stream BaseStream
		{
			get
			{
				if(this.reader == null)
					throw new ObjectDisposedException("Could not access to a closed object");
				return this.reader.BaseStream;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Exposes access to the underlying socket of the DotGridSocket.
		/// </summary>
		public Socket BaseSocket
		{
			get
			{
				/*if(this.reader == null)
					throw new ObjectDisposedException("Could not access to a closed object");
				return this.reader.BaseSocket;*/
				/*if(this.socket == null)
					return this.reader.BaseSocket;
				else*/
				return this.socket;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a exception object to remote endpoint (the remote client) based on DotGrid ExceptionHandlingHeader protocol.
		/// </summary>
		/// <param name="e">The Exception object.</param>
		public void WriteException(Exception e)
		{
			byte[] temp = new ExceptionHandlingHeader(e).Buffer;
			Write(temp, 0, temp.Length);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Notifies the remote endpoint (the remote client) that no exception has occurred based on DotGrid ExceptionHandlingHeader protocol.
		/// </summary>
		public void WriteNoException()
		{
			WriteByte((byte)eXception.NO);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Investigates if remote endpoint (thew remote server) reply with an exception response then an exception will be dropped.
		/// </summary>
		public void CheckExceptionResponse()
		{
			int response = ReadByte();
			if((response & 0x0F) == (int)eXception.OK)
			{
				int n = 0;
				byte _EMode = (byte)((response & 0xF0) >> 4);
				switch(_EMode)
				{
					case (byte)EMode.INT8:
						n = ReadByte();
						break;
					case (byte)EMode.INT16:
					{
						int b0 = ReadByte();
						int b1 = ReadByte();
						n = (b0 << 8) | b1;
						break;
					}
					case (byte)EMode.INT24:
					{
						int b0 = ReadByte();
						int b1 = ReadByte();
						int b2 = ReadByte();
						n = (b0 << 16) | (b1  << 8) | b2;
						break;
					}
					case (byte)EMode.INT32:
					{
						int b0 = ReadByte();
						int b1 = ReadByte();
						int b2 = ReadByte();
						int b3 = ReadByte();
						n = (b0 << 24) | (b1  << 16) | (b2  << 8) | b3;
						break;
					}
					default: 
					{ 
						Close(); 
						throw new Exception("The server replied on bad state for EMode enum."); 
					}
				}
				if(n <= 0) 
				{ 
					Close(); 
					throw new Exception("The exception buffer length replied by server is less than or equal zero."); 
				}
				byte[] buffer = Read(n);
				if(buffer.Length != n) 
				{
					buffer = null;  
					Close(); 
					throw new Exception("The server replied on bad state for exception buffer and ELength field."); 
				}
				Exception ee;
				try
				{
					//throw new Exception("The server has dropped the following exception.", (Exception)DotGrid.Serialization.DeSerialize(buffer)); 
					ee = (Exception)SerializeDeserialize.DeSerialize(buffer); 
				}
				catch(Exception e)
				{
					Close();
					throw new Exception("The exception buffer replied by server is in an invalid state.", e);
				}
				throw ee;
			}
			if((response & 0x0F) == (int)eXception.NO) 
				return ;
			else //must to be considered
			{ 
				Close(); 
				throw new Exception("The server replied on bad state for exception handling."); 
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Close the connected connection to remote endpoint.
		/// </summary>
		public void Close()
		{
			//this.BaseSocket.Shutdown(SocketShutdown.Both);
			//this.BaseSocket.Close();
			if(this.BaseSocket != null)
			{
				//this.BaseSocket.Shutdown(SocketShutdown.Both);
				this.BaseSocket.Close();
				//Console.WriteLine("exit");
			}
			else
			{
				this.reader.Close();
				this.writer.Close();
			}
			this.reader = null;
			this.writer = null;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads an object from network stream.
		/// </summary>
		/// <returns>Returned object from network stream.</returns>
		public object ReadObject()
		{
			byte[] buffer = Read(4);
			if(buffer.Length != 4)
			{
				Close();
				throw new ArgumentOutOfRangeException("The remote endpoint replied bad format for Object Header."); 
			}
			int size = (buffer[0] << 24) | (buffer[1]  << 16) | (buffer[2]  << 8) | buffer[3]; // Object Length
			if(size <= 0)
			{
				Close();
				throw new ArgumentOutOfRangeException("The remote endpoint replied bad format for Object Header."); 
			}
			buffer = new byte[size];
			Read(buffer, buffer.Length);
			return SerializeDeserialize.DeSerialize(buffer);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes an object to network stream.
		/// </summary>
		/// <param name="obj">Favorite object for sending to network stream.</param>
		public void WriteObject(object obj)
		{
			byte[] temp = SerializeDeserialize.Serialize(obj);
			byte[] buffer = new byte[4 + temp.Length];  // Length + Object
			buffer[0] = (byte)((temp.Length & 0xFF000000) >> 24);
			buffer[1] = (byte)((temp.Length & 0x00FF0000) >> 16);
			buffer[2] = (byte)((temp.Length & 0x0000FF00) >> 8);
			buffer[3] = (byte) (temp.Length & 0x000000FF);
			Array.Copy(temp, 0, buffer, 4, buffer.Length - 4);
			temp = null;
			Write(buffer, 0, buffer.Length);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads any length buffer from SecureBinaryReader stream.
		/// </summary>
		/// <param name="array">When this method returns, contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		public int Read(byte[] array, int count)
		{
			if(this.reader == null)
				throw new ObjectDisposedException("Could not access to a closed object");
			if(count <= tcpBufferSize)
			{
				return ReadInternal(array, 0, count);
			}
			else
			{
				int temp = 0;
				int sum = 0;
				int i = 0;
				int a = count / tcpBufferSize;
				int q = count % tcpBufferSize;
				while(true)
				{
					if((temp = ReadInternal(array, tcpBufferSize*i, tcpBufferSize)) == -1) 
					{ 
						throw new ObjectDisposedException("The remote endpoint closed the connection");
					}
					if(temp == 0)
						break;
					sum += temp;
					i++;
					if(q != 0 && i == a)
					{
						if((temp = ReadInternal(array, tcpBufferSize*i, q)) == -1) 
						{ 
							return -1;
						}
						if(temp == 0) 
							break;
						sum += temp;
						break;
					}
					if(q == 0 && i == a)
						break;
				}
				return sum;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes any length buffer to SecureBinaryWriter stream.
		/// </summary>
		/// <param name="array">When this method returns, contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The byte offset in array at which to begin reading.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		public int Write(byte[] array, int offset, int count)
		{
			if(this.writer == null)
				throw new ObjectDisposedException("Could not access to a closed object");
			if(count <= tcpBufferSize)
			{
				Send(array, offset, count);
				return count;
			}
			else
			{
				int i = 0;
				int a = array.Length / tcpBufferSize;
				int q = array.Length % tcpBufferSize;
				while(true)
				{
					Send(array, tcpBufferSize*i, tcpBufferSize);
					i++;
					if(q != 0 && i == a)
					{
						Send(array, tcpBufferSize*i, q);
						break;
					}
					if(q == 0 && i == a)
						break;
				}
				return count;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes any buffer to SecureBinaryWriter stream.
		/// </summary>
		/// <param name="array">When this method returns, contains the specified byte array with the values between 0 and Array.Length - 1 replaced by the bytes read from the current source.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		public int Write(byte[] array)
		{
			return Write(array, 0, array.Length);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads maximum 32KB or 64KB length buffer from SecureBinaryReader stream.
		/// </summary>
		/// <param name="array">When this method returns, contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The byte offset in array at which to begin reading.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		private int ReadInternal(byte[] array, int offset, int count)
		{
			if(this.reader == null)
				throw new ObjectDisposedException("Could not access to a closed object");
			int m = 0;
			int e = 0;
			while(count - m > 0)
			{
				if((e = Receive(array, offset + m, count - m)) == -1) 
					return -1;
				m += e;
			}
			return m;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a maximum of n bytes from the current network stream into buffer and return it.
		/// </summary>
		/// <param name="n">states n bytes for reading from network stream</param>
		/// <returns>Return received buffer data, if there are'nt no data then null will be returned.</returns>
		public byte[] Read(int n)
		{
			if(this.reader == null)
				throw new ObjectDisposedException("Could not access to a closed object");
			try
			{
				int m = 0;
				byte[] buffer = new byte[n];
				if((m = this.reader.Read(buffer, 0, buffer.Length)) == 0)
				{
					buffer = null;
					throw new ObjectDisposedException("The remote endpoint closed the connection");
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
				throw new ObjectDisposedException("The remote endpoint closed the connection");
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
			if(this.reader == null)
				throw new ObjectDisposedException("Could not access to a closed object");
			try
			{
				int m = 0;
				if((m = this.reader.Read(array, offset, count)) == 0)
				{
					throw new ObjectDisposedException("The remote endpoint closed the connection");
				}
				else
				{
					return m;
				}
			}
			catch
			{
				throw new ObjectDisposedException("The remote endpoint closed the connection");
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a bytes from the current network stream into buffer and return it.
		/// </summary>
		/// <returns>Return received buffer data.</returns>
		public byte ReadByte()
		{
			if(this.reader == null)
				throw new ObjectDisposedException("Could not access to a closed object");
			try
			{
				return this.reader.ReadByte();
			}
			catch
			{
				throw new ObjectDisposedException("The remote endpoint closed the connection");
			}
		}
		/// <summary>
		/// 
		/// </summary>
		public bool FromDownloadDirectoryClient
		{
			set
			{
				this.reader.FromDownloadDirectoryClient  = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes to the network stream.
		/// </summary>
		/// <param name="buffer">buffer for writing to network stream.</param>
		/// <returns>If any errors occurred, -1 will be returned otherwise 0.</returns>
		private void Send(byte[] buffer)
		{
			if(this.writer == null)
				throw new ObjectDisposedException("Could not access to a closed object");
			try
			{
				this.writer.Write(buffer);
			}
			catch
			{
				throw new ObjectDisposedException("The remote endpoint closed the connection");
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes to the network stream.
		/// </summary>
		/// <param name="buffer">buffer for writing to network stream.</param>
		/// <returns>If any errors occurred, -1 will be returned otherwise 0.</returns>
		public void WriteByte(byte buffer)
		{
			if(this.writer == null)
				throw new ObjectDisposedException("Could not access to a closed object");
			try
			{
				this.writer.Write(buffer);
			}
			catch
			{
				throw new ObjectDisposedException("The remote endpoint closed the connection");
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes to the network stream.
		/// </summary>
		/// <param name="buffer">buffer for writing to network stream.</param>
		/// <param name="offset">The starting point in buffer at which to begin writing. </param>
		/// <param name="count">The number of bytes to write.</param>
		private void Send(byte[] buffer, int offset, int count)
		{
			if(this.writer == null)
				throw new ObjectDisposedException("Could not access to a closed object");
			try
			{
				this.writer.Write(buffer, offset, count);
			}
			catch
			{
				throw new ObjectDisposedException("The remote endpoint closed the connection");
			}
		}
		//**************************************************************************************************************//
	}
}