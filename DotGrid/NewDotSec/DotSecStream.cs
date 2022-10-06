/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using DotGrid.DotSec; 
using DotGrid.Serialization;

namespace DotGrid.NewDotSec
{
	/// <summary>
	/// Summary description for DotSecStream.
	/// </summary>
	public class DotSecStream
	{
		private Socket socket;
		/// <summary>
		/// 
		/// </summary>
		//protected Stream m_stream;
		private RijndaelEncryption rijndael;
		//private byte[] buffer;
		//private const int BufferSize = 256 * 1024;
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="rijndael"></param>
		public DotSecStream(Socket socket, RijndaelEncryption rijndael)
		{
			if(socket == null)
				throw new ArgumentNullException("socket is null");
			this.socket = socket;
			if (!socket.Blocking)
				throw new IOException ();
			this.rijndael = rijndael;
			//m_stream = new NetworkStream(socket, FileAccess.ReadWrite, false);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		public void WriteByte(byte value)
		{
			byte[] byteBuffer = new byte[1];
			byteBuffer[0] = value;
			Write(byteBuffer, 0, byteBuffer.Length);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
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
		/// 
		/// </summary>
		/// <returns></returns>
		public object ReadObject()
		{
			byte[] buffer = new byte[4];
			int n = Read(buffer, 0, 4);
			if(n != 4)
			{
				throw new ArgumentOutOfRangeException("The remote endpoint replied bad format for Object Header."); 
			}
			int size = (buffer[0] << 24) | (buffer[1]  << 16) | (buffer[2]  << 8) | buffer[3]; // Object Length
			if(size <= 0)
				throw new ArgumentOutOfRangeException("The remote endpoint replied bad format for Object Header."); 
			buffer = new byte[size];
			Read(buffer, 0, buffer.Length);
			return SerializeDeserialize.DeSerialize(buffer);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public int ReadByte()
		{
			byte[] byteBuffer = new byte[1];
			int n = Read(byteBuffer, 0, byteBuffer.Length);
			if(n <= 0)
				return -1;
			return byteBuffer[0];
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		/// <param name="length"></param>
		public void Write(byte[] value, int index, int length)
		{
			if(rijndael == null)
			{
				WriteToOriginalSocket(value, 0, value.Length);
				//m_stream.Write(value, 0, value.Length);
				return ;
			}
			else
			{
				//byte[] buffer = new byte[10];
				//m_stream.Write(buffer, 0, 4);
				//m_stream.Write(value, 0, value.Length);
				/*if(buffer == null)
					buffer = new byte[BufferSize];*/
				MD5 md5 = new MD5();
				byte[] hash = md5.MD5hash(value, index, length);
				//int BufferHashLength = length + hash.Length;
				byte[] temp = new byte[length + hash.Length];
				for(int i = index ; i < length ; i++)
					temp[i] = value[i];
				for(int i = 0 ; i < hash.Length ; i++)
					temp[i + length] = hash[i];
				md5 = null;
				hash = null;
				byte[] encrypted = rijndael.encrypt(temp);
				byte[] buffer = new byte[4 + encrypted.Length]; // length + (Encrypted-Data-Hash).Length
				buffer[0] = (byte)((encrypted.Length & 0xFF000000) >> 24);
				buffer[1] = (byte)((encrypted.Length & 0x00FF0000) >> 16);
				buffer[2] = (byte)((encrypted.Length & 0x0000FF00) >> 8);
				buffer[3] = (byte)(encrypted.Length & 0x000000FF);
				Console.WriteLine("encrypted length: " + encrypted.Length);//
				for(int i = 0 ; i < encrypted.Length ; i++)
					buffer[4 + i] = encrypted[i];
				Console.WriteLine("buffer length: " + buffer.Length);//
				Console.WriteLine("encrypted length: {0}", (buffer[0] << 24) | (buffer[1]  << 16) | (buffer[2]  << 8) | buffer[3]);
				//WriteToOriginalSocket(buffer, 0, buffer.Length); //
				//return ;
			    //buffer = new byte[1];
				//WriteToOriginalSocket(buffer, 0, 4);
				//return ;
				WriteToOriginalSocket(buffer, 0, buffer.Length);
				//return ;
				//encrypted = null;
				//buffer = null;
				//return ;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public int Read(byte[] value, int index, int length)
		{
			if(rijndael == null)
			{
				//return ReadFromOriginialSocket(value, index, length);
				return ReadInternal(value, index, length);
			}
			else
			{
				//return ReadInternal(value, index, length);
				byte[] temp = new byte[4];
				int n = ReadInternal(temp, 0, temp.Length);
				//Console.WriteLine("n: " + n);
				//return 1000;
				//return n;
				if(n == 0) return 0;
				if(n != temp.Length)
					throw new IOException("The stream format is an bad state.");
				int len = (temp[0] << 24) | (temp[1]  << 16) | (temp[2]  << 8) | temp[3];
				//Console.WriteLine("encrypted length: " + len);//
				temp = new byte[len];
				n =  ReadInternal(temp, 0, temp.Length);
				if(n == 0 || n != temp.Length || n < 32) // 32 meaning the buffer only involves encrypted MD5 hash without no encrypted data
					throw new IOException("The stream only involves encrypted MD5 hash without no encrypted data.");
				temp = this.rijndael.decrypt(temp);
				byte[] hash = new byte[16]; // 128 bits from MD5 hash buffer
				for(int i = 0 ; i < hash.Length ; i++)
					hash[i] = temp[temp.Length - 16 + i];
				byte[] data = new byte[temp.Length - 16];
				for(int i = 0 ; i < data.Length ; i++)
					data[i] = temp[i];
				MD5 md5 = new MD5();
				temp = md5.MD5hash(data);
				for(int i = 0 ; i < temp.Length ; i++)
					if(hash[i] != temp[i])
						throw new System.Security.SecurityException("The hash with the data is wrong.");
				for(int i =  0 ; i < data.Length ; i++)
					value[i + index] = data[i];
				return data.Length;
			}
		}
		//**************************************************************************************************************//
		private int ReadInternal(byte[] array, int offset, int count)
		{
			int m = 0;
			int e = 0;
			//socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
			while(count - m > 0)
			{
				if((e = socket.Receive(array, offset + m, count - m, SocketFlags.None)) == -1) 
					throw new ObjectDisposedException("the remote endpoint closed the connection.");
				m += e;
			}
			return m;
		}
		/*private int ReadFromOriginialSocket(byte[] array, int offset, int count)
		{
			int m = 0;
			int e = 0;
			while(count - m > 0)
			{
				if((e = socket.Receive(array, offset + m, count - m, 0)) == -1) 
					throw new ObjectDisposedException("the remote endpoint closed the connection.");
				m += e;
			}
			return m;
		}*/
		/*private int ReadFromOriginialSocket ([In,Out] byte [] buffer, int offset, int size)
		{
			int res;

			if (buffer == null)
				throw new ArgumentNullException ("buffer is null");
			if(offset<0 || offset>buffer.Length) 
			{
				throw new ArgumentOutOfRangeException("offset exceeds the size of buffer");
			}
			if(size < 0 || offset+size>buffer.Length) 
			{
				throw new ArgumentOutOfRangeException("offset+size exceeds the size of buffer");
			}

			try 
			{
				res = socket.Receive (buffer, offset, size, 0);
			} 
			catch (Exception e) 
			{
				throw new IOException ("Read failure", e);
			}
			
			return res;
		}*/
		//**************************************************************************************************************//
		private void WriteToOriginalSocket(byte [] buffer, int offset, int size)
		{
			if(buffer == null)
				throw new ArgumentNullException ("buffer");

			if(offset < 0 || offset > buffer.Length)
				throw new ArgumentOutOfRangeException("offset exceeds the size of buffer");

			if(size < 0 || size > buffer.Length - offset)
				throw new ArgumentOutOfRangeException("offset+size exceeds the size of buffer");
			try 
			{
				int count = 0;
				while (size - count > 0) 
				{
					count += socket.Send (buffer, offset + count, size - count, SocketFlags.None);
				}
			} 
			catch (Exception e) 
			{
				throw new IOException ("Write failure", e); 
			}
		}
		//**************************************************************************************************************//
	}
}