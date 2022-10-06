/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Text;
using System.Net.Sockets;

namespace DotGrid.DotSec
{
	/// <summary>
	/// Reads a secure or none secure primitive data types as binary values in a specific encoding.
	/// </summary>
	public class SecureBinaryReader/* : IDisposable*/ 
	{
		private Socket socket = null;
		private Stream m_stream;
		private Encoding m_encoding;
		private RijndaelEncryption rijndael = null;
		private byte[] m_buffer = null;
		private byte[] n_buffer = null;
		//private BinaryReader br  = null;
		private int m_encoding_max_byte;
		private bool m_disposed = false;
		private int DataSize = 32 * 1024; //(max raw data size : 2*2^14 2 2*16KB
		//private static readonly int HeaderSize = 1024;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the SecureBinaryReader class based on the supplied stream and using UTF8Encoding.
		/// </summary>
		/// <param name="input">A stream. </param>
		/// <param name="rijndael">A RijndaelEncryption class for encrypting data.If this variable is null then this SecureBinaryReader will be changed to normal .NET BiranrReader class.</param>
		public SecureBinaryReader(Stream input, RijndaelEncryption rijndael)
		{
			if(input == null) 
				throw new ArgumentNullException("Input is a null reference.");
			this.rijndael = rijndael;
			m_encoding = Encoding.UTF8;
			if(this.rijndael == null)
			{
				//br = new BinaryReader(input, m_encoding);
				DataSize = 256 * 1024; //for none secure connections maximum 64 KB read
				//return ;
			}
			m_stream = input;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the BinaryReader class based on the supplied stream and a specific character encoding.
		/// </summary>
		/// <param name="socket">Base socket of this instance.</param>
		/// <param name="input">A stream. </param>
		/// <param name="rijndael">A RijndaelEncryption class for encrypting data.If this variable is null then this SecureBinaryReader will be changed to normal .NET BiranrReader class.</param>
		/// <param name="encoding">The character encoding.</param>
		public SecureBinaryReader(Socket socket, Stream input, RijndaelEncryption rijndael, Encoding encoding)
		{
			if(input == null) 
				throw new ArgumentNullException("Input is a null reference.");
			if(socket == null) 
				throw new ArgumentNullException("socket is a null reference.");
			if (!input.CanRead)
				throw new ArgumentException("The stream doesn't support reading.");
			this.rijndael = rijndael;
			this.socket = socket;
			m_encoding = encoding;
			if(this.rijndael == null)
			{
				//br = new BinaryReader(input, m_encoding);
				DataSize = 256 * 1024; //for none secure connections maximum 64 KB read
				//return ;
			}
			m_stream = input;
			m_stream = input;
			m_encoding_max_byte = m_encoding.GetMaxByteCount(1);
			n_buffer = new byte [32];
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the BinaryReader class based on the supplied stream and a specific character encoding.
		/// </summary>
		/// <param name="input">A stream. </param>
		/// <param name="rijndael">A RijndaelEncryption class for encrypting data.If this variable is null then this SecureBinaryReader will be changed to normal .NET BiranrReader class.</param>
		/// <param name="encoding">The character encoding.</param>
		public SecureBinaryReader(Stream input, RijndaelEncryption rijndael, Encoding encoding) 
		{
			if (input == null || encoding == null) 
				throw new ArgumentNullException("Input or Encoding is a null reference.");
			if (!input.CanRead)
				throw new ArgumentException("The stream doesn't support reading.");
			this.rijndael = rijndael;
			m_encoding = encoding;
			if(this.rijndael == null)
			{
				//br = new BinaryReader(input, m_encoding);
				DataSize = 256 * 1024; //for none secure connections maximum 64 KB read
				//return ;
			}
			m_stream = input;
			m_encoding_max_byte = m_encoding.GetMaxByteCount(1);
			n_buffer = new byte [32];
		}
		//**************************************************************************************************************//
		/// <summary>
		///  Exposes access to the underlying socket of the SecureBinaryReader.
		/// </summary>
		public Socket BaseSocket
		{
			get 
			{
				return this.socket;
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
				if(rijndael != null)
					return true;
				else return false;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Exposes access to the underlying stream of the SecureBinaryReader.
		/// </summary>
		public virtual Stream BaseStream 
		{
			get 
			{
				if(this.rijndael == null)
					return this.m_stream;
				else
					return m_stream;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Closes the current reader and the underlying stream.
		/// </summary>
		public virtual void Close() 
		{
			Dispose(true);
			m_disposed = true;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Releases any consumed system resources.
		/// </summary>
		/// <param name="disposing">Disposing condition.</param>
		protected virtual void Dispose (bool disposing)
		{
			/*if(this.rijndael == null)
			{
				m_disposed = true;
				br.Close();
				br = null;
				m_buffer = null;
				m_encoding = null;
				m_stream = null;
				m_buffer = null;
				n_buffer = null;
				rijndael = null;
			}
			else
			{*/
			if(disposing && m_stream != null)
			{
				m_stream.Close ();
				//this.socket.Close();
			}
			m_disposed = true;
			//br = null;
			m_buffer = null;
			m_encoding = null;
			m_stream = null;
			m_buffer = null;
			n_buffer = null;
			rijndael = null;
			//}
		}
		//**************************************************************************************************************//
		/*void IDisposable.Dispose() 
		{
			Dispose (true);
		}*/
		//**************************************************************************************************************//
		/// <summary>
		/// Returns the next available character and does not advance the byte or character position.
		/// </summary>
		/// <returns>The next available character, or -1 if no more characters are available or the stream does not support seeking.</returns>
		public virtual int PeekChar() 
		{
			if(m_stream==null) 
			{	
				if (m_disposed)
					throw new ObjectDisposedException ("SecureBinaryReader", "Cannot read from a closed BinaryReader.");
				throw new IOException("Stream is invalid");
			}
			if ( !m_stream.CanSeek )
			{
				return -1;
			}
			char[] result = new char[1];
			byte[] bytes;
			int bcount;
			int ccount = ReadCharBytes (result, 0, 1, out bytes, out bcount);
			// Reposition the stream
			m_stream.Position -= bcount;
			// If we read 0 characters then return -1
			if(ccount == 0) 
			{
				return -1;
			}
			// Return the single character we read
			return result[0];
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads count characters from the stream with index as the starting point in the character array.
		/// </summary>
		/// <param name="buffer">The buffer to read data into.</param>
		/// <param name="index">The starting point in the buffer at which to begin reading into the buffer.</param>
		/// <param name="count">The number of characters to read.</param>
		/// <returns>The number of characters read into buffer.</returns>
		public virtual int Read(char[] buffer, int index, int count) 
		{
			if(m_stream == null) 
			{
				if (m_disposed)
					throw new ObjectDisposedException ("SecureBinaryReader", "Cannot read from a closed BinaryReader.");
				throw new IOException("Stream is invalid");
			}
			if (buffer == null) 
			{
				throw new ArgumentNullException("buffer is null");
			}
			if (index < 0) 
			{
				throw new ArgumentOutOfRangeException("index is less than 0");
			}
			if (count < 0) 
			{
				throw new ArgumentOutOfRangeException("count is less than 0");
			}
			if (buffer.Length - index < count) 
			{
				throw new ArgumentException("buffer is too small");
			}
			int bytes_read;
			byte[] bytes;
			return ReadCharBytes (buffer, index, count, out bytes, out bytes_read);
		}
		//**************************************************************************************************************//
		private int ReadCharBytes(char[] buffer, int index, int count, out byte[] bytes, out int bytes_read) 
		{
			int chars_read = 0;
			bytes_read = 0;
			while(chars_read < count)
			{
				CheckBuffer(bytes_read + 1);
				int read_byte = ReadByte();
				if(read_byte == -1) 
				{
					// EOF 
					bytes = n_buffer;
					return(chars_read);
				}
				n_buffer[bytes_read] = (byte)read_byte;
				bytes_read++;
				chars_read = m_encoding.GetChars(n_buffer, 0, bytes_read, buffer, index);
			}
			bytes = n_buffer;
			return(chars_read);
		}
		//**************************************************************************************************************//
		/* Ensures that m_buffer is at least length bytes
		 * long, growing it if necessary
		 */
		private void CheckBuffer(int length)
		{
			if(n_buffer.Length <= length) 
			{
				byte[] new_buffer = new byte[length];
				Array.Copy(n_buffer, new_buffer,
					n_buffer.Length);
				n_buffer = new_buffer;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected int Read7BitEncodedInt() 
		{
			int ret = 0;
			int shift = 0;
			byte b;
			do 
			{
				b = ReadByte();
				ret = ret | ((b & 0x7f) << shift);
				shift += 7;
			} while ((b & 0x80) == 0x80);
			return ret;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads characters from the underlying stream and advances the current position of the stream in accordance with the Encoding used and the specific character being read from the stream.
		/// </summary>
		/// <returns>The next character from the input stream, or -1 if no characters are currently available.</returns>
		public virtual int Read() 
		{
			char[] decode = new char[1];
			int count = Read(decode, 0, 1);
			if(count == 0) 
			{
				//No chars available 
				return(-1);
			}
			return decode[0];
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a Boolean value from the current stream and advances the current position of the stream by one byte.
		/// </summary>
		/// <returns>true if the byte is nonzero; otherwise, false.</returns>
		public virtual bool ReadBoolean() 
		{
			FillBuffer(1);
			// Return value:
			//  true if the byte is non-zero; otherwise false.
			return(n_buffer[0] != 0);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bytes"></param>
		protected virtual void FillBuffer (int bytes)
		{
			if(m_disposed)
				throw new ObjectDisposedException ("SecureBinaryReader", "Cannot read from a closed BinaryReader.");
			if(m_stream == null)
				throw new IOException("Stream is invalid");
			CheckBuffer(bytes);
			//Cope with partial reads 
			int pos = 0;
			while(pos < bytes) 
			{
				int n = Read(n_buffer, pos, bytes - pos);
				if(n==0) 
				{
					throw new EndOfStreamException();
				}
				pos += n;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads count bytes from the current stream into a byte array and advances the current position by count bytes.
		/// </summary>
		/// <param name="count">The number of bytes to read.</param>
		/// <returns>A byte array containing data read from the underlying stream. This might be less than the number of bytes requested if the end of the stream is reached.</returns>
		public virtual byte[] ReadBytes(int count) 
		{
			/*if(this.rijndael == null)
			{
				return br.ReadBytes(count);
			}*/
			if(m_stream == null) 
			{

				if (m_disposed)
					throw new ObjectDisposedException ("SecureBinaryReader", "Cannot read from a closed BinaryReader.");

				throw new IOException("Stream is invalid");
			}
			if(count < 0) 
			{
				throw new ArgumentOutOfRangeException("count is less than 0");
			}
			/* Can't use FillBuffer() here, because it's OK to
				 * return fewer bytes than were requested
				 */
			byte[] buf = new byte[count];
			int pos = 0;
			while(pos < count) 
			{
				int n = Read(buf, pos, count-pos);
				if(n == 0) 
				{
					/* EOF */
					break;
				}
				pos += n;
			}	
			if(pos != count) 
			{
				byte[] new_buffer = new byte[pos];
				Array.Copy(buf, new_buffer, pos);
				return(new_buffer);
			}
			
			return(buf);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads the next character from the current stream and advances the current position of the stream in accordance with the Encoding used and the specific character being read from the stream.
		/// </summary>
		/// <returns>A character read from the current stream.</returns>
		public virtual char ReadChar() 
		{
			int ch = Read();
			if(ch == -1) 
			{
				throw new EndOfStreamException();
			}
			return((char)ch);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads count characters from the current stream, returns the data in a character array, and advances the current position in accordance with the Encoding used and the specific character being read from the stream.
		/// </summary>
		/// <param name="count">The number of characters to read.</param>
		/// <returns>A character array containing data read from the underlying stream. This might be less than the number of characters requested if the end of the stream is reached.</returns>
		public virtual char[] ReadChars(int count) 
		{
			if(count < 0) 
			{
				throw new ArgumentOutOfRangeException("count is less than 0");
			}
			char[] full = new char[count];
			int chars = Read(full, 0, count);
			if(chars == 0) 
			{
				throw new EndOfStreamException();
			} 
			else if(chars != full.Length) 
			{
				char[] ret = new char[chars];
				Array.Copy(full, 0, ret, 0, chars);
				return ret;
			} 
			else 
			{
				return full;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a decimal value from the current stream and advances the current position of the stream by sixteen bytes.
		/// </summary>
		/// <returns>A decimal value read from the current stream.</returns>
		unsafe public virtual decimal ReadDecimal() 
		{
			FillBuffer(16);
			decimal ret;
			byte* ret_ptr = (byte *)&ret;
			for (int i = 0; i < 16 ; i++) 
			{
					
				//internal representation of decimal is 
				//ss32, hi32, lo32, mi32, 
				//but in stream it is 
				//lo32, mi32, hi32, ss32
				// So we have to rerange this int32 values
					 			  
				if (i < 4) 
				{
					// lo 8 - 12			  
					ret_ptr [i + 8] = n_buffer [i];
				} 
				else if (i < 8) 
				{
					// mid 12 - 16
					ret_ptr [i + 8] = n_buffer [i];
				} 
				else if (i < 12) 
				{
					// hi 4 - 8
					ret_ptr [i - 4] = n_buffer [i];
				} 
				else if (i < 16) 
				{
					// ss 0 - 4
					ret_ptr [i - 12] = n_buffer [i];
				}				
			}
			return ret;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads an 8-byte floating point value from the current stream and advances the current position of the stream by eight bytes.
		/// </summary>
		/// <returns>An 8-byte floating point value read from the current stream.</returns>
		public virtual double ReadDouble() 
		{
			FillBuffer(8);
			return(BitConverter.ToDouble(n_buffer, 0));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a 2-byte signed integer from the current stream and advances the current position of the stream by two bytes.
		/// </summary>
		/// <returns>A 2-byte signed integer read from the current stream.</returns>
		public virtual short ReadInt16() 
		{
			FillBuffer(2);
			return((short) (n_buffer[0] | (n_buffer[1] << 8)));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a 4-byte signed integer from the current stream and advances the current position of the stream by four bytes.
		/// </summary>
		/// <returns>A 4-byte signed integer read from the current stream.</returns>
		public virtual int ReadInt32() 
		{
			FillBuffer(4);
			return(n_buffer[0] | (n_buffer[1] << 8) |
				(n_buffer[2] << 16) | (n_buffer[3] << 24));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads an 8-byte signed integer from the current stream and advances the current position of the stream by four bytes.
		/// </summary>
		/// <returns>An 8-byte signed integer read from the current stream.</returns>
		public virtual long ReadInt64() 
		{
			FillBuffer(8);
			uint ret_low  = (uint) (n_buffer[0]            |
				(n_buffer[1] << 8)  |
				(n_buffer[2] << 16) |
				(n_buffer[3] << 24)
				);
			uint ret_high = (uint) (n_buffer[4]        |
				(n_buffer[5] << 8)  |
				(n_buffer[6] << 16) |
				(n_buffer[7] << 24)
				);
			return (long) ((((ulong) ret_high) << 32) | ret_low);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a signed byte from this stream and advances the current position of the stream by one byte.
		/// </summary>
		/// <returns>A signed byte read from the current stream.</returns>
		[CLSCompliant(false)]
		public virtual sbyte ReadSByte() 
		{
			FillBuffer(1);
			return((sbyte)n_buffer[0]);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a string from the current stream. The string is prefixed with the length, encoded as an integer seven bits at a time.
		/// </summary>
		/// <returns>The string being read.</returns>
		public virtual string ReadString() 
		{
			
			// Inspection of BinaryWriter-written files
			// shows that the length is given in bytes,
			// not chars
			int len = Read7BitEncodedInt();
			FillBuffer(len);
			char[] str = m_encoding.GetChars(n_buffer, 0, len);
			return(new String(str));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a 4-byte floating point value from the current stream and advances the current position of the stream by four bytes.
		/// </summary>
		/// <returns>A 4-byte floating point value read from the current stream.</returns>
		public virtual float ReadSingle() 
		{
			
			FillBuffer(4);
			return(BitConverter.ToSingle(n_buffer, 0));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a 2-byte unsigned integer from the current stream using little endian encoding and advances the position of the stream by two bytes.
		/// </summary>
		/// <returns>A 2-byte unsigned integer read from this stream.</returns>
		[CLSCompliant(false)]
		public virtual ushort ReadUInt16() 
		{
			FillBuffer(2);
			return((ushort) (n_buffer[0] | (n_buffer[1] << 8)));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a 4-byte unsigned integer from the current stream and advances the position of the stream by four bytes.
		/// </summary>
		/// <returns>A 4-byte unsigned integer read from this stream.</returns>
		[CLSCompliant(false)]
		public virtual uint ReadUInt32() 
		{
			FillBuffer(4);
			return((uint) (n_buffer[0] |
				(n_buffer[1] << 8) |
				(n_buffer[2] << 16) |
				(n_buffer[3] << 24)));
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads an 8-byte unsigned integer from the current stream and advances the position of the stream by eight bytes.
		/// </summary>
		/// <returns>An 8-byte unsigned integer read from this stream.</returns>
		[CLSCompliant(false)]
		public virtual ulong ReadUInt64() 
		{
			FillBuffer(8);
			uint ret_low  = (uint) (n_buffer[0]            |
				(n_buffer[1] << 8)  |
				(n_buffer[2] << 16) |
				(n_buffer[3] << 24)
				);
			uint ret_high = (uint) (n_buffer[4]        |
				(n_buffer[5] << 8)  |
				(n_buffer[6] << 16) |
				(n_buffer[7] << 24)
				);
			return (((ulong) ret_high) << 32) | ret_low;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads count bytes from the stream with index as the starting point in the byte array.
		/// </summary>
		/// <param name="value">The buffer to read data into.</param>
		/// <param name="index">The starting point in the buffer at which to begin reading into the buffer.</param>
		/// <param name="count">The number of characters to read.</param>
		/// <returns>The number of characters read into buffer.</returns>
		public virtual int Read(byte[] value, int index, int count) 
		{
			/*if(rijndael == null)
				return br.Read(value, index, count);*/
			if(m_stream == null) 
			{
				if(m_disposed)
					throw new ObjectDisposedException ("SecureBinaryReader", "Cannot read from a closed SecureBinaryReader.");
				throw new IOException("Stream is invalid.");
			}
			if(value == null) 
				throw new ArgumentNullException("buffer is null.");
			if(index < 0) 
				throw new ArgumentOutOfRangeException("index is less than 0.");
			if(count < 0) 
				throw new ArgumentOutOfRangeException("count is less than 0.");
			//if(value.Length - index < count) 
			//	throw new ArgumentException("buffer is too small.");
			if(rijndael == null)
			{
				return this.ReadFromOriginialStream(value, index, count);
			}
			if(rijndael == null)
			{
				if(count <= DataSize)
				{
					byte[] temp = new byte[count];
					int n = Read(temp);
					if(n == 0) return 0;
					for(int  i = 0 ; i < n ; i++)
						value[i +  index] = temp[i];
					return n;
				}
				else throw new ArgumentOutOfRangeException(String.Format("count can not be greater than {0} bytes.", DataSize));
			}
			else
			{
				if(count <= 2 * DataSize)
				{
					byte[] temp = new byte[count];
					int n = Read(temp);
					if(n == 0) return 0;
					for(int  i = 0 ; i < n ; i++)
						value[i +  index] = temp[i];
					return n;
				}
				else throw new ArgumentOutOfRangeException(String.Format("count can not be greater than {0} bytes.", DataSize));
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads 32 KB from the stream and copies count bytes from encrypted data with index as the starting point in the byte array if related methods request few bytes for reading and there are any data on stream, temproray this method reads tha data into m_buffer and hold it after new requset if the m_buffer the request replied from m_buffer if m_buffer will not be null.
		/// </summary>
		/// <param name="value">The buffer to read data into.</param>
		/// <returns>The number of characters read into buffer.</returns>
		protected int Read(byte[] value)
		{
			/*if(rijndael == null)
				return br.Read(value, 0, value.Length);*/
			if(m_stream==null)
			{
				if(m_disposed)
					throw new ObjectDisposedException ("SecureBinaryReader", "Cannot read from a closed SecureBinaryReader.");
				throw new IOException("Stream is invalid.");
			}
			if(value == null) 
				throw new ArgumentNullException("value is null.");
			if(rijndael == null)
			{
				return this.ReadFromOriginialStream(value, 0, value.Length);
			}
			if(m_buffer == null)
			{
				byte[] temp ;
				int n = 0;
				if(rijndael == null)
				{
					temp = new byte[DataSize];
					n = ReadPacket(temp);
				}
				else
				{
					temp = new byte[DataSize * 2]; // maximum 32KB read for secure connections
					n = ReadSecurePacket(temp);
				}
				if(n == 0) return 0;
				if(n <= value.Length)
				{
					for(int i = 0 ; i < n ; i++)
						value[i] = temp[i];
					return n;
				}
				else // n > value.Length
				{
					int reminder = n - value.Length;
					m_buffer = new byte[reminder];
					for(int i = 0 ; i < m_buffer.Length ; i++)
						m_buffer[i] = temp[value.Length + i];
					for(int i = 0 ; i < value.Length ; i++)
						value[i] = temp[i];
					return value.Length; //return n;
				}
			}
			else
			{
				if(m_buffer.Length <= value.Length)
				{
					for(int i = 0 ; i < m_buffer.Length ; i++)
						value[i] = m_buffer[i];
					int n = m_buffer.Length;
					m_buffer = null;
					return n;
				}
				else // m_buffer.Length > value.Length
				{
					int reminder = m_buffer.Length - value.Length;
					for(int i = 0 ; i < value.Length ; i++)
						value[i] = m_buffer[i];
					byte[] temp = new byte[reminder];
					for(int  i = 0 ; i < reminder ; i++)
						temp[i] = m_buffer[value.Length + i];
					m_buffer = temp;
					return value.Length;
				}
			}
		}
		//**************************************************************************************************************//
		private byte[] one_byte = new byte[1];
		/// <summary>
		/// Reads the next byte from the current stream and advances the current position of the stream by one byte.
		/// </summary>
		/// <returns>The next byte read from the current stream.</returns>
		public virtual byte ReadByte() 
		{
			/*if(this.rijndael == null)
			{
				return br.ReadByte();
			}*/
			if(m_stream == null) 
			{
				if (m_disposed)
					throw new ObjectDisposedException ("SecureBinaryReader", "Cannot read from a closed SecureBinaryReader.");

				throw new IOException ("Stream is invalid.");
			}
			if(rijndael == null)//
			{
				return (byte)this.m_stream.ReadByte();
			}
			int n;
			n = Read(one_byte);
			if(n == 0)
				throw new EndOfStreamException("The end of the stream was reached.");
			return one_byte[0];
		}
		//**************************************************************************************************************//
		private int ReadFromOriginialStream(byte[] array, int offset, int count)
		{
			int m = 0;
			int e = 0;
			while(count - m > 0)
			{
				if((e =this.m_stream.Read(array, offset + m, count - m)) == -1) 
					throw new ObjectDisposedException("the remote endpoint closed the connection.");
				m += e;
			}
			return m;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads and decrypt maximum 32 KB from the stream and copies count bytes from encrypted data with index as the starting point in the byte array.
		/// </summary>
		/// <param name="buffer">The buffer to read data into.</param>
		/// <returns>The number of characters read into buffer.</returns>
		private int ReadPacket(byte[] buffer)
		{
			/*int m = 0;
			int e = 0;
			while(buffer.Length - m > 0)
			{
				e = this.m_stream.Read(buffer, m, buffer.Length - m);
				m += e;
			}
			return m;*/
			if(rijndael == null)
			{
				return this.ReadFromOriginialStream(buffer, 0, buffer.Length);
			}
			else return m_stream.Read(buffer, 0, buffer.Length);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads and decrypt maximum 32 KB from the stream and copies count bytes from encrypted data with index as the starting point in the byte array.
		/// </summary>
		/// <param name="buffer">The buffer to read data into.</param>
		/// <returns>The number of characters read into buffer.</returns>
		private int ReadSecurePacket(byte[] buffer)
		{
			byte[] temp = new byte[2];
			int n = m_stream.Read(temp, 0, temp.Length);
			if(n == 0) return 0;
			if(n != temp.Length)
				throw new IOException("The stream format is an bad state.");
			int len = (temp[0] << 8) | temp[1];
			temp = new byte[len];
			int m = 0;
			int e = 0;
			while(len - m > 0)
			{
				e = this.m_stream.Read(temp, m, len - m);
				m += e;
			}
			n = m;
			/*n = m_stream.Read(temp, 0, temp.Length);
			if(n != 0 && n != temp.Length)
			{
				byte[] t = new byte[len  - n];
				int tt = m_stream.Read(t, 0, t.Length);
				byte[] ttt = new byte[tt + n];
				for(int i = 0 ; i < n ; i++)
					ttt[i] = temp[i];
				for(int i = 0 ; i < tt ; i++)
					ttt[i + n] = t[i];
				temp = ttt;
				n = temp.Length;
			}*/
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
				buffer[i] = data[i];
			return data.Length;
		}
		//**************************************************************************************************************//
	}
}