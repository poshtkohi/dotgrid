/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Text;

namespace DotGrid.DotSec
{
	/// <summary>
	/// Writes primitive types in binary to a secure stream and supports writing strings in a specific encoding.
	/// </summary>
	[Serializable]
	public class SecureBinaryWriter : IDisposable
	{
		/// <summary>
		/// Base stream of this SecureBinaryWriter instance.
		/// </summary>
		protected Stream OutStream;
		private Encoding m_encoding;
		private RijndaelEncryption rijndael = null;
		private BinaryWriter bwr = null;
		private bool disposed = false;
		private int DataSize = 32 * 1024; //max data size : 2^14 or 16KB
		//private static readonly int HeaderSize = 1024;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the SecureBinaryWriter class that writes to a stream.
		/// </summary>
		/// <param name="rijndael">A RijndaelEncryption class for encrypting data.If this variable is null then this SecureBinaryReader will be changed to normal .NET BiranrWriter class.</param>
		public SecureBinaryWriter(RijndaelEncryption rijndael)
		{
			this.rijndael = rijndael;
			m_encoding = Encoding.UTF8;
			if(this.rijndael == null)
			{
				DataSize = 256 * 1024; //for none secure connections maximum 64 KB read
				bwr = new BinaryWriter(Stream.Null, m_encoding);
				return ;
			}
			OutStream = Stream.Null;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the SecureBinaryWriter class based on the supplied stream and using UTF-8 as the encoding for strings.
		/// </summary>
		/// <param name="output">The supplied stream. </param>
		/// <param name="rijndael">A RijndaelEncryption class for encrypting data.If this variable is null then this SecureBinaryReader will be changed to normal .NET BiranrWriter class.</param>
		public SecureBinaryWriter(Stream output, RijndaelEncryption rijndael)
		{
			/*if(rijndael == null)
				throw new ArgumentNullException("Rijndael is a null reference.");*/
			this.rijndael = rijndael;
			m_encoding = Encoding.UTF8;
			if(this.rijndael == null)
			{
				DataSize = 256 * 1024; //for none secure connections maximum 64 KB read
				bwr = new BinaryWriter(output, m_encoding);
				return ;
			}
			OutStream = output;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the SecureBinaryWriter class based on the supplied stream and a specific character encoding.
		/// </summary>
		/// <param name="output">The supplied stream. </param>
		/// <param name="rijndael">A RijndaelEncryption class for encrypting data.If this variable is null then this SecureBinaryReader will be changed to normal .NET BiranrWriter class.</param>
		/// <param name="encoding">The character encoding.</param>
		public SecureBinaryWriter(Stream output, RijndaelEncryption rijndael, Encoding encoding)
		{
			if(output == null || encoding == null) 
				throw new ArgumentNullException("Output or Encoding is a null reference.");
			if(!output.CanWrite)
				throw new ArgumentException("Stream does not support writing or already closed.");
			/*if(rijndael == null)
				throw new ArgumentNullException("Rijndael is a null reference.");*/
			this.rijndael = rijndael;
			m_encoding = encoding;
			if(this.rijndael == null)
			{
				DataSize = 256 * 1024; //for none secure connections maximum 64 KB read
				bwr = new BinaryWriter(output, m_encoding);
				return ;
			}
			OutStream = output;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets whether this stream is in secure or non-secure mode.
		/// </summary>
		public bool IsSecure
		{
			get 
			{
				if(rijndael != null)
					return true;
				else return false;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the underlying stream of the BinaryWriter.
		/// </summary>
		public virtual Stream BaseStream 
		{
			get 
			{
				if(this.rijndael == null)
					return OutStream;
				else
					return OutStream;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Closes the current BinaryWriter and the underlying stream.
		/// </summary>
		public virtual void Close() 
		{
			Dispose(true);
		}
		//**************************************************************************************************************//
		void IDisposable.Dispose() 
		{
			Dispose(true);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Releases any consumed system resources.
		/// </summary>
		/// <param name="disposing">Disposing condition.</param>
		protected virtual void Dispose (bool disposing)
		{
			if(this.rijndael == null)
			{
				disposed = true;
				bwr.Close();
				bwr = null;
				OutStream = null;
				m_encoding = null;
				rijndael = null;
			}
			else
			{
				if(disposing && OutStream != null)
					OutStream.Close();
				disposed = true;
				bwr = null;
				m_encoding = null;
				rijndael = null;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Clears all buffers for the current writer and causes any buffered data to be written to the underlying device.
		/// </summary>
		public virtual void Flush() 
		{
			if(rijndael == null)
			{
				bwr.BaseStream.Flush();
				return ;
			}
			OutStream.Flush();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sets the position within the current stream.
		/// </summary>
		/// <param name="offset">A byte offset relative to origin.</param>
		/// <param name="origin">A field of SeekOrigin indicating the reference point from which the new position is to be obtained.</param>
		/// <returns>The position with the current stream.</returns>
		public virtual long Seek(int offset, SeekOrigin origin) 
		{
			if(rijndael == null)
				return bwr.Seek(offset, origin);
			else
				return OutStream.Seek(offset, origin);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a byte array to the underlying stream.
		/// </summary>
		/// <param name="value">A byte array containing the data to write.</param>
		public virtual void Write(byte[] value) 
		{
			if(disposed)
				throw new ObjectDisposedException ("SecureBinaryWriter", "Cannot write to a closed SecureBinaryWriter");
			if(value == null)
				throw new ArgumentNullException("Value is a null reference.");
			if(rijndael == null)
			{
				bwr.Write(value, 0, value.Length);
				return ;
			}
			Write(value, 0, value.Length);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a length-prefixed string to this stream in the current encoding of the BinaryWriter, and advances the current position of the stream in accordance with the encoding used and the specific characters being written to the stream.
		/// </summary>
		/// <param name="value">The value to write.</param>
		public virtual void Write(string value) 
		{
			if(disposed)
				throw new ObjectDisposedException ("SecureBinaryWriter", "Cannot write to a closed SecureBinaryWriter");
			if(value == null)
				throw new ArgumentNullException("Value is a null reference.");
			if(rijndael == null)
			{
				bwr.Write(value);
				return ;
			}
			else
			{
				byte[] enc = m_encoding.GetBytes(value);
				Write(enc, 0, enc.Length);	
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes an unsigned byte to the current stream and advances the stream position by one byte.
		/// </summary>
		/// <param name="value">The unsigned byte to write.</param>
		public virtual void Write(byte value) 
		{
			if(disposed)
				throw new ObjectDisposedException ("SecureBinaryWriter", "Cannot write to a closed SecureBinaryWriter");
			if(rijndael == null)
			{
				bwr.Write(value);
				return ;
			}
			else
			{
				byte[] buffer = new byte[1];
				buffer[0] = value;
				WriteInternal(buffer);
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a four-byte signed integer to the current stream and advances the stream position by four bytes.
		/// </summary>
		/// <param name="value">The four-byte signed integer to write.</param>
		public virtual void Write(int value) 
		{
			if(disposed)
				throw new ObjectDisposedException ("SecureBinaryWriter", "Cannot write to a closed SecureBinaryWriter");
			if(rijndael == null)
			{
				bwr.Write(value);
				return ;
			}
			else
			{
				byte[] buffer = new byte[4];
				buffer [0] = (byte) value;
				buffer [1] = (byte) (value >> 8);
				buffer [2] = (byte) (value >> 16);
				buffer [3] = (byte) (value >> 24);
				WriteInternal(buffer);
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes an eight-byte signed integer to the current stream and advances the stream position by eight bytes.
		/// </summary>
		/// <param name="value">The eight-byte signed integer to write.</param>
		public virtual void Write(long value) 
		{
			if(disposed)
				throw new ObjectDisposedException ("SecureBinaryWriter", "Cannot write to a closed SecureBinaryWriter");
			if(rijndael == null)
			{
				bwr.Write(value);
				return ;
			}
			byte[] buffer = new byte[8];
			for (int i = 0, sh = 0; i < 8; i++, sh += 8)
				buffer [i] = (byte) (value >> sh);
			WriteInternal(buffer);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a Unicode character to the current stream and advances the current position of the stream in accordance with the Encoding used and the specific characters being written to the stream.
		/// </summary>
		/// <param name="value">The character to write.</param>
		public virtual void Write(char value) 
		{
			if(disposed)
				throw new ObjectDisposedException ("BinaryWriter", "Cannot write to a closed SecureBinaryWriter");
			if(rijndael == null)
			{
				bwr.Write(value);
				return ;
			}
			char[] dec = new char[1];
			dec[0] = value;
			byte[] enc = m_encoding.GetBytes(dec, 0, 1);
			WriteInternal(enc);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a character array to the current stream and advances the current position of the stream in accordance with the Encoding used and the specific characters being written to the stream.
		/// </summary>
		/// <param name="value">A character array containing the data to write.</param>
		public virtual void Write(char[] value) 
		{
			if(disposed)
				throw new ObjectDisposedException ("SecureBinaryWriter", "Cannot write to a closed SecureBinaryWriter");
			if(rijndael == null)
			{
				bwr.Write(value);
				return ;
			}
			if(value == null)
				throw new ArgumentNullException("Chars is a null reference.");
			byte[] enc = m_encoding.GetBytes(value, 0, value.Length);
			Write(enc, 0, enc.Length);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a region of a byte array to the current stream.
		/// </summary>
		/// <param name="value">A byte array containing the data to write.</param>
		/// <param name="offset">The starting point in buffer at which to begin writing.</param>
		/// <param name="length">The number of bytes to write.</param>
		public virtual void Write(byte[] value, int offset, int length)
		{
			if(disposed)
				throw new ObjectDisposedException ("SecureBinaryWriter", "Cannot write to a closed SecureBinaryWriter");
			if (value == null)
				throw new ArgumentNullException("Value is a null reference.");
			if(length <= 0)
				throw new ArgumentOutOfRangeException("Length can not be negative or zero.");
			/*if(length > DataSize)
				throw new ArgumentOutOfRangeException(String.Format("Length can not be greater than {0} bytes.", DataSize));*/
			if(offset < 0)
				throw new ArgumentOutOfRangeException("Offset can not be negative.");
			if(offset + length > value.Length) 
				throw new ArgumentOutOfRangeException("Index of value is out of range by provided offset and length.");
			if(rijndael == null)
			{
				bwr.Write(value, offset, length);
				return ;
			}	
			if(length <= DataSize)
			{
				byte[] temp = new byte[length];
				for(int i = 0 ; i < length ; i++)
					temp[i] = value[offset + i];
				WriteInternal(temp);
				return ;
			}
			else throw new ArgumentOutOfRangeException(String.Format("length can not be greater than {0} bytes.", DataSize));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a maximum DataSize byte array to the underlying stream.
		/// </summary>
		/// <param name="value">A byte array containing the data to write.</param>
		private void WriteInternal(byte[] value)
		{
			SecureDataHeader sdh = new SecureDataHeader(value, this.rijndael);
			byte[] buffer = sdh.Buffer;
			if(rijndael == null)
			{
				bwr.Write(buffer, 0, buffer.Length);
				return ;
			}
			OutStream.Write(buffer, 0, buffer.Length);
			//OutStream.Flush();
		}
		//**************************************************************************************************************//
	}
}