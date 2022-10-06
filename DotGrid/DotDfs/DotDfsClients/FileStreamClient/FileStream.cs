/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading;
using System.Net.Sockets;
//using System.Security.Cryptography.X509Certificates;

using DotGrid.Net;
using DotGrid.DotSec;
using DotGrid.Shared.Enums;
using DotGrid.Serialization;
using DotGrid.Shared.Headers;
using DotGrid.Shared.Enums.DotDFS;
using DotGrid.Shared.Headers.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Exposes a Stream around a remote file, supporting both synchronous and asynchronous read and write operations.
	/// </summary>
	public class DotDfsFileStream : IDisposable
	{
		private SecureBinaryReader reader;
		private SecureBinaryWriter writer;
		private Socket socket;
		private FileStreamHeader fsh;
		private bool secure = true;
		private RijndaelEncryption rijndael;
		private bool disposed = false;
		private int tcpBufferSize = 0;// 64KB  for none secure and 32KB for secure connections
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the FileStream class with the specified path, creation mode, read/write permission, and sharing permission.
		/// </summary>
		/// <param name="remotePath">A remote relative or absolute path for the file that the current FileStream object will encapsulate.</param>
		/// <param name="mode">A FileMode constant that determines how to open or create the file.</param>
		/// <param name="access">A FileAccess constant that determines how the file can be accessed by the FileStream object.</param>
		/// <param name="share">A FileShare constant that determines how the file will be shared by processes.</param>
		/// <param name="encoding">A PathEncoding constant that determines how the path file will be encoded by processes.</param>
		/// <param name="dotDfsServerAddress">dotDfsServer Address.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination dotDfs server.</param>
		/// <param name="Secure">Determines secure or secureless connection based on DotGrid.DotSec transfer layer security.</param>
		public DotDfsFileStream(string remotePath, FileMode mode, FileAccess access, FileShare share, PathEncoding encoding, string dotDfsServerAddress, NetworkCredential nc, bool Secure)
		{
			if(remotePath == null)
				throw new ArgumentNullException("remotePath is a null reference.");
			if(dotDfsServerAddress == null)
				throw new ArgumentNullException("You must state a dotDfsServerAddress for the remote dotDfs server.");
			this.secure = Secure;
			byte[] buffer = AuthenticationHeaderBuilder(nc.UserName, nc.Password);
			IPHostEntry hostEntry = Dns.Resolve (dotDfsServerAddress);
			IPAddress ip = hostEntry.AddressList[0];
			socket = new Socket (ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(new IPEndPoint (ip, 2799));
			NetworkStream ns = new NetworkStream(socket, FileAccess.ReadWrite, true);
			//------PublicKeyAuthentication---------------
			rijndael = new RijndaelEncryption(128); // a random 128 bits rijndael shared key
			PublicKeyAuthentication(ns);
			this.reader = new SecureBinaryReader(socket, ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			this.writer = new SecureBinaryWriter(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			tcpBufferSize = 32 * 1024;
			if(Send(buffer) == -1) { ConnectionClose(); throw new ObjectDisposedException("The remote server closed the connection."); } //sends AuthenticationHeader
			switch(ReceiveByte()) //considers reponse for authorization.
			{
				case -1:
				{
					ConnectionClose();
					throw new ObjectDisposedException("The remote server closed the connection.");
				}
				case (byte)ClientAuthenticationError.OK:
					break;
				case (byte)ClientAuthenticationError.NO:
				{
					ConnectionClose();
					throw new Exception("Username or Password is wrong.");
				}
				case (byte)ClientAuthenticationError.BAD:
				{
					ConnectionClose();
					throw new Exception("Bad format for n bytes of username and m bytes of password (buffer.Length != 2+n+m and buffer.Length less than 2)");
				}
				default :
				{
					ConnectionClose();
					throw new Exception("The server replied with an unrecognized code for login state.");
				}
			}
			//--------------------------------------------
			if(!Secure)
			{
				this.reader = new SecureBinaryReader(socket, ns, null, System.Text.Encoding.ASCII);
				this.writer = new SecureBinaryWriter(ns, null, System.Text.Encoding.ASCII);
				tcpBufferSize = 256 * 1024;
			}
			//--------------------------------------------
			//Selection of DFSM mode on remote DotDFS server
			if(Send((byte)TransferChannelMode.FileStreamFromClient) == -1) 
			{ 
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			this.fsh = new FileStreamHeader(remotePath, mode, access, share, encoding);
			if(Send(fsh.Buffer) == -1) 
			{ 
				ConnectionClose();
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			ExceptionResponse(); //handle if one exception has been dropped by the dotDfs server
			//------------------------------------------------------------------------
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		void IDisposable.Dispose() 
		{
			Dispose(true);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose (bool disposing)
		{
			if(disposing && this.reader != null)
				this.reader.Close();
			disposed = true;
			this.reader = null;
			this.writer = null;
			this.fsh = null;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Investigates if server reply with an exception response then an exception will be dropped.
		/// </summary>
		private void ExceptionResponse()
		{
			int response = ReceiveByte();
			if(response == -1) { ConnectionClose(); throw new ObjectDisposedException("The remote server closed the connection."); }
			if((response & 0x0F) == (int)eXception.OK)
			{
				int n = 0;
				byte _EMode = (byte)((response & 0xF0) >> 4);
				switch(_EMode)
				{
					case (byte)EMode.INT8:
						n = ReceiveByte();
						if(n == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						break;
					case (byte)EMode.INT16:
					{
						int b0 = ReceiveByte();
						if(b0 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b1 = ReceiveByte();
						if(b1 == -1) { ConnectionClose(); throw new ObjectDisposedException("The remote server closed the connection."); }
						n = (b0 << 8) | b1;
						break;
					}
					case (byte)EMode.INT24:
					{
						int b0 = ReceiveByte();
						if(b0 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection.");
						}
						int b1 = ReceiveByte();
						if(b1 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b2 = ReceiveByte();
						if(b2 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						n = (b0 << 16) | (b1  << 8) | b2;
						break;
					}
					case (byte)EMode.INT32:
					{
						int b0 = ReceiveByte();
						if(b0 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b1 = ReceiveByte();
						if(b1 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b2 = ReceiveByte();
						if(b2 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						int b3 = ReceiveByte();
						if(b3 == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						n = (b0 << 24) | (b1  << 16) | (b2  << 8) | b3;
						break;
					}
					default: 
					{ 
						ConnectionClose(); 
						throw new Exception("The server replied on bad state for EMode enum."); 
					}
				}
				if(n <= 0) 
				{ 
					ConnectionClose(); 
					throw new Exception("The exception buffer length replied by server is less than or equal zero."); 
				}
				byte[] buffer = Receive(n);
				if(buffer == null) 
				{ 
					ConnectionClose(); 
					throw new ObjectDisposedException("The remote server closed the connection."); 
				}
				if(buffer.Length != n) 
				{
					buffer = null;  
					ConnectionClose(); 
					throw new Exception("The server replied on bad state for exception buffer and ELength field."); 
				}
				Exception ee;
				try
				{
					ee = (Exception)SerializeDeserialize.DeSerialize(buffer); 
				}
				catch(Exception e)
				{
					ConnectionClose();
					throw new Exception("The exception buffer replied by server is in an invalid state.", e);
				}
				throw ee;
			}
			if((response & 0x0F) == (int)eXception.NO) 
				return ;
			else 
			{ 
				ConnectionClose(); 
				throw new Exception("The server replied on bad state for exception handling."); 
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Does public key authentication.
		/// </summary>
		/// <param name="ns">Network stream.</param>
		private void PublicKeyAuthentication(NetworkStream ns)
		{
			this.reader = new SecureBinaryReader(socket, ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			this.writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			tcpBufferSize = 256 * 1024;
			byte[] buffer;
			buffer = Receive(3 + 128); // public-key.Length + modulus.Length
			if(buffer == null)
			{
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			//Console.WriteLine(buffer.Length);
			if(buffer.Length > 3 + 128 || buffer.Length < 128 + 1)
			{
				ConnectionClose(); 
				throw new ArgumentOutOfRangeException("The server replied the RSAPublicHeader in bad format."); 
			}
			else
			{
				byte[] e = null;
				switch(buffer.Length)
				{
					case 131 /*128 + 3*/://  for Microsoft .NET RSA implementation. Microsoft generates e in public key (e,n) with 3 bytes.
						e = new byte[3];
						e[0] = buffer[0];
						e[1] = buffer[1];
						e[2] = buffer[2];
						break;
					case 130/*128 + 2*/://for other RSA implementation
						e = new byte[2];
						e[0] = buffer[0];
						e[1] = buffer[1];
						break;
					case 129/*128 + 1*/://  for MONO .NET RSA implementation. MONO generates e in public key (e,n) with 3 bytes.
						e = new byte[1];
						e[0] = buffer[0];
						break;
					default:
						ConnectionClose(); 
						throw new ArgumentOutOfRangeException("Client implementation does'nt support public key in (e,n) with greater than 3 bytes.");
				}
				byte[] modulus = new byte[128];
				Array.Copy(buffer,e.Length , modulus, 0, modulus.Length); 
				RSA rsa = new RSA(e, modulus); // server RSA public key
				SharedKeyHeader skh = new SharedKeyHeader(secure, rsa, rijndael);
				if(Send(skh.Buffer) == -1)
				{ 
					ConnectionClose(); 
					throw new ObjectDisposedException("The remote server closed the connection.");
				}
				//Console.WriteLine("hello");
				ExceptionResponse();
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a block of bytes from the stream and writes the data in a given buffer.
		/// </summary>
		/// <param name="array">When this method returns, contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The byte offset in array at which to begin reading.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		public int Read(byte[] array, int offset, int count)
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot read from a closed FileStream");
			if(count == 0) { throw new ArgumentException("Count can not be zero."); }
			if(array == null) { throw new ArgumentNullException("Array can not be null."); }
			if(offset < 0 || count < 0) { throw new ArgumentOutOfRangeException("offset or count can not be negative."); }
			if(offset + count > array.Length) { throw new ArgumentOutOfRangeException("Index of array is out of range by provided offset and count."); }
			if(count <= tcpBufferSize)
			{
				return ReadInternal(array, offset, count);
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
						ConnectionClose(); 
						throw new ObjectDisposedException("The remote server closed the connection."); 
					}
					if(temp == 0)
						break;
					sum += temp;
					i++;
					if(q != 0 && i == a)
					{
						if((temp = ReadInternal(array, tcpBufferSize*i, q)) == -1) 
						{ 
							ConnectionClose(); 
							throw new ObjectDisposedException("The remote server closed the connection."); 
						}
						if(temp == 0) 
							break;
						sum += temp;
						break;
					}
					if(q == 0 && i == a)
						break;
					//Thread.Sleep(1);
				}
				return sum;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a block of bytes from the stream and writes the data in a given buffer with tcpBufferSize length.
		/// </summary>
		/// <param name="array">When this method returns, contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The byte offset in array at which to begin reading.</param>
		/// <param name="count">The maximum number of bytes to read.</param>
		/// <returns>The total number of bytes read into the buffer. This might be less than the number of bytes requested if that number of bytes are not currently available, or zero if the end of the stream is reached.</returns>
		private int ReadInternal(byte[] array, int offset, int count)
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot read from a closed FileStream");
			if(count == 0) 
				throw new ArgumentException("Count can not be zero."); 
			if(array == null) 
				throw new ArgumentNullException("Array can not be null.");
			if(offset < 0 || count < 0) 
				throw new ArgumentOutOfRangeException("offset or count can not be negative."); 
			if(offset + count > array.Length) 
				throw new ArgumentOutOfRangeException("Index of array is out of range by provided offset and count."); 
			//--------Send ReadWriteHeader----------
			ReadWriteHeader rwh = new ReadWriteHeader(count, Method.Read);
			byte[] buffer = rwh.Buffer;
			//rwh = null;
			if(Send(buffer) == -1) 
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection.");
			}
			ExceptionResponse(); // investigate if there is no error during file reading and correctness of method arguments on server side.
			//--------Receive array from dotDfs Server---
			int b = ReceiveByte();
			if(b == -1) 
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection.");
			}
			int n = 0;
			RWMode _RWMode = (RWMode)(((byte)b & 0xF0) >> 4);
			switch(_RWMode)
			{
				case RWMode.INT8:
					n = ReceiveByte();
					if(n == -1) 
					{ 
						ConnectionClose(); 
						throw new ObjectDisposedException("The remote server closed the connection."); 
					}
					break;
				case RWMode.INT16:
				{
					buffer = Receive(2);
					if(buffer == null) 
					{ 
						ConnectionClose(); 
						throw new ObjectDisposedException("The remote server closed the connection."); 
					}
					if(buffer.Length != 2)
						throw new ArgumentException("The server replied with a bad RWMode format.");
					n = (buffer[0] << 8) | buffer[1];
					break;
				}
				case RWMode.INT24:
				{
					buffer = Receive(3);
					if(buffer == null) 
					{ 
						ConnectionClose(); 
						throw new ObjectDisposedException("The remote server closed the connection."); 
					}
					if(buffer.Length != 3)
						throw new ArgumentException("The server replied with a bad RWMode format.");
					n = (buffer[0] << 16) | (buffer[1]  << 8) | buffer[2];
					break;
				}
				case RWMode.INT32:
				{
					buffer = Receive(4);
					if(buffer == null) 
					{ 
						ConnectionClose(); 
						throw new ObjectDisposedException("The remote server closed the connection."); 
					}
					if(buffer.Length != 4)
						throw new ArgumentException("The server replied with a bad RWMode format.");
					n = (buffer[0] << 24) | (buffer[1]  << 16) | (buffer[2]  << 8) | buffer[3];
					break;
				}
				default: 
				{ 
					throw new ArgumentException("Bad RWMode format replied from server.");
				}
			}
			if(n == 0) 
				return 0; // returned 0 means that the end of the stream is reached.
			if(n < 0) 
			{ 
				ConnectionClose(); 
				throw new ArgumentException("RWMode field can not be negative or zero."); 
			}
			if(n > array.Length) 
			{ 
				ConnectionClose(); 
				throw new ArgumentException("Server replied with a returned buffer size which is greater than array size.");
			}
			//---------read buffer content to array-----------------
			int m = 0;
			int e = 0;
			while(n - m > 0)
			{
				if((e = Receive(array, offset + m, n - m)) == -1) 
				{ 
					ConnectionClose(); 
					throw new ObjectDisposedException("The remote server closed the connection.");
				}
				m += e;
			}
			/*if((m = Receive(array, offset, n)) == -1) 
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			if( m != n)
			{ 
				byte[] bb = Receive(10);
				Console.WriteLine(bb[9]);
				Console.WriteLine("m:{0},n:{1}", m, n);
				ConnectionClose(); 
				throw new ArgumentException("The server returned the real buffer size in wrong length."); 
			}*/
			//ExceptionResponse();
			buffer = null;
			//GC.Collect();
			return n;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a block of bytes to this stream using data from a buffer.
		/// </summary>
		/// <param name="array">The array to which bytes are written.</param>
		/// <param name="offset">The byte offset in array at which to begin writing.</param>
		/// <param name="count">The maximum number of bytes to write.</param>
		/// <returns></returns>
		public void Write(byte[] array, int offset, int count)
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot write to a closed FileStream");
			if(count == 0) return ;
			if(array == null) { throw new ArgumentNullException("Array can not be null."); }
			if(offset < 0 || count < 0) { throw new ArgumentOutOfRangeException("offset or count can not be negative."); }
			if(offset + count > array.Length) { throw new ArgumentOutOfRangeException("Index of array is out of range by provided offset and count."); }
			/*byte[] buffer = new byte[count];
			for(int j = 0 ; j < count ; j++)
				buffer[j] = array[offset + j];*/
			//--------Send array to dotDfs Server---
			if(count <= tcpBufferSize)
			{
				WriteInternal(array, offset, count);
				//GC.Collect();
				return ;
			}
			else
			{
				int i = 0;
				int a = count / tcpBufferSize;
				int q = count % tcpBufferSize;
				while(true)
				{
					WriteInternal(array, tcpBufferSize*i, tcpBufferSize);
					i++;
					if(q != 0 && i == a)
					{
						WriteInternal(array, tcpBufferSize*i, q);
						break;
					}
					if(q == 0 && i == a)
					{
						break;
					}
					//if(!secure)
					//	Thread.Sleep(1);
				}
				//GC.Collect();
				return ;
			}
			//--------------------------------------
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Writes a block of bytes to this stream using data from a buffer with tcpBufferSize length.
		/// </summary>
		/// <param name="array">The array to which bytes are written.</param>
		/// <param name="offset">The byte offset in array at which to begin writing.</param>
		/// <param name="count">The maximum number of bytes to write.</param>
		/// <returns></returns>
		private void WriteInternal(byte[] array, int offset, int count)
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot write to a closed FileStream");
			if(count == 0) return ;
			if(array == null) { throw new ArgumentNullException("Array can not be null."); }
			if(offset < 0 || count < 0) { throw new ArgumentOutOfRangeException("offset or count can not be negative."); }
			if(offset + count > array.Length) { throw new ArgumentOutOfRangeException("Index of array is out of range by provided offset and count."); }
			//--------Send ReadWriteHeader----------
			ReadWriteHeader rwh = new ReadWriteHeader(count, Method.Write);
			if(Send(rwh.Buffer) == -1) { ConnectionClose(); throw new ObjectDisposedException("The remote server closed the connection."); }
			ExceptionResponse();
			//--------Send array to dotDfs Server---
			if(Send(array, offset, count) == -1) { ConnectionClose(); throw new ObjectDisposedException("The remote server closed the connection."); }
			ExceptionResponse();
			//--------------------------------------
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		public void Flush()
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot flush a closed FileStream");
			if(Send((byte)Method.Flush) == -1) 
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			ExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Closes the file and releases any resources associated with the current file stream.
		/// </summary>
		public void Close()
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot close a closed FileStream");
			if(Send((byte)Method.Close) == -1)
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			ExceptionResponse();
			ConnectionClose();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sets the length of this stream to the given value.
		/// </summary>
		/// <param name="value">The new length of the stream.</param>
		public void SetLength(long value)
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot set length of a closed FileStream");
			SetLengthHeader slh = new SetLengthHeader(value);
			if(Send(slh.Buffer) == -1) 
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection.");
			}
			ExceptionResponse();
			//Console.WriteLine("set length finish");
			//GC.Collect();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Prevents other processes from changing the FileStream while permitting read access.
		/// </summary>
		/// <param name="position">The beginning of the range to unlock.</param>
		/// <param name="length">The range to be unlocked.</param>
		public void Lock(long position, long length)
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot lock a closed FileStream");
			LockOrUnlock(true, position, length);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Allows access by other processes to all or part of a file that was previously locked.
		/// </summary>
		/// <param name="position">The beginning of the range to lock. The value of this parameter must be equal to or greater than zero (0).</param>
		/// <param name="length">The range to be locked.</param>
		public void UnLock(long position, long length)
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot unlock a closed FileStream");
			LockOrUnlock(false, position, length);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Lock or Unlock the FileStream instance.
		/// </summary>
		/// <param name="Lock">If Lock is true then this header will be LockHeader, otherwise is UnlockHeader.</param>
		/// <param name="position">The beginning of the range to lock. The value of this parameter must be equal to or greater than zero (0).</param>
		/// <param name="length">The range to be locked.</param>
		private void LockOrUnlock(bool Lock, long position, long length)
		{
			LockUnlockHeader luh = new LockUnlockHeader(Lock, position, length);
			if(Send(luh.Buffer) == -1) 
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			ExceptionResponse();
			//GC.Collect();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sets the current position of this stream to the given value.
		/// </summary>
		/// <param name="offset">The point relative to origin from which to begin seeking.</param>
		/// <param name="origin">Specifies the beginning, the end, or the current position as a reference point for origin, using a value of type SeekOrigin.</param>
		public long Seek(long offset, SeekOrigin origin)
		{
			if(disposed)
				throw new ObjectDisposedException ("FileStream", "Cannot seek a closed FileStream");
			long value = 0;
			SeekHeader sh = new SeekHeader(offset, origin);
			if(Send(sh.Buffer) == -1) 
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			ExceptionResponse();
			int b = ReceiveByte();
			if(b == -1)
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			LongMode _Mode = (LongMode)b;
			if(Enum.IsDefined(typeof(LongMode), _Mode))
			{
				byte[] buffer = Receive((int) _Mode);
				if(buffer == null) 
				{ 
					ConnectionClose(); 
					throw new ObjectDisposedException("The remote server closed the connection."); 
				}
				if(buffer.Length != (int)_Mode)
					throw new ArgumentException("The server replied with a bad format returned LongMode value for Seek method.");
				value = (long)LongValueHeader.GetLongNumberFromBytes(buffer);
				buffer = null;
			}
			else throw new ArgumentOutOfRangeException("The server replied with an undefined returned LongMode value for Seek method.");
			//GC.Collect();
			return value;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets position or Length of the current FileStream instance.
		/// </summary>
		/// <param name="Position">If Position is true then this method will find Position, otherwise Length.</param>
		/// <returns>Position or Length value of current FileStream instance with attention to Position field.</returns>
		private long GetPositionOrLength(bool Position)
		{
			long value = 0;
			Method _Method;
			if(Position)
				_Method = Method.Position;
			else
				_Method = Method.Length;
			if(Send((byte)_Method) == -1) 
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			ExceptionResponse();
			int b = ReceiveByte();
			if(b == -1)
			{ 
				ConnectionClose(); 
				throw new ObjectDisposedException("The remote server closed the connection."); 
			}
			LongMode _Mode = (LongMode)b;
			if(Enum.IsDefined(typeof(LongMode), _Mode))
			{
				byte[] buffer = Receive((int) _Mode);
				if(buffer == null) 
				{ 
					ConnectionClose(); 
					throw new ObjectDisposedException("The remote server closed the connection."); 
				}
				if(buffer.Length != (int)_Mode)
					throw new ArgumentException("The server replied with a bad format returned LongMode value for Seek method.");
				value = (long)LongValueHeader.GetLongNumberFromBytes(buffer);
				buffer = null;
			}
			else throw new ArgumentOutOfRangeException("The server replied with an undefined returned LongMode value for Seek method.");
			//GC.Collect();
			return value;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the current position of this stream.
		/// </summary>
		public long Position
		{
			get
			{
				if(disposed)
					throw new ObjectDisposedException ("FileStream", "Cannot get position of a closed FileStream");
				return GetPositionOrLength(true);
			}
			set
			{
				if(disposed)
					throw new ObjectDisposedException ("FileStream", "Cannot set position of a closed FileStream");
				SetLength(value);
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the length in bytes of the stream.
		/// </summary>
		public long Length
		{
			get
			{
				if(disposed)
					throw new ObjectDisposedException ("FileStream", "Cannot get length of a closed FileStream");
				return GetPositionOrLength(false);
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a value indicating whether the current stream supports reading.
		/// </summary>
		public bool CanRead
		{
			get
			{
				if(disposed)
					throw new ObjectDisposedException ("FileStream", "Cannot get CanRead of a closed FileStream");
				if(this.fsh.FileAccess == FileAccess.Read)
					return true;
				else return false;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a value indicating whether the current stream supports writing.
		/// </summary>
		public bool CanWrite
		{
			get
			{
				if(disposed)
					throw new ObjectDisposedException ("FileStream", "Cannot get CanWrite of a closed FileStream");
				if(this.fsh.FileAccess == FileAccess.Write)
					return true;
				else return false;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		public bool CanSeek
		{
			get
			{
				if(disposed)
					throw new ObjectDisposedException ("FileStream", "Cannot get CanSeek of a closed FileStream");
				if(Send((byte)Method.CanSeek) == -1)
				{ 
					ConnectionClose();
					throw new ObjectDisposedException("The remote server closed the connection."); 
				}
				ExceptionResponse();
				int response = 0;
				if((response = ReceiveByte()) == -1)
				{ 
					ConnectionClose(); 
					throw new ObjectDisposedException("The remote server closed the connection."); 
				}
				switch((CanSeekEnum)response)
				{
					case CanSeekEnum.TRUE:
						return true;
					case CanSeekEnum.FALSE:
						return false;
					default:
						throw new ArgumentException("The server replied on an undefined CanSeekEnum field.");
				}
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the name of the FileStream that was passed to the constructor.
		/// </summary>
		public string Name
		{
			get
			{
				if(disposed)
					throw new ObjectDisposedException ("FileStream", "Cannot get Name of a closed FileStream");
				return this.fsh.Path;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Close the connected connection to dotDfs server.
		/// </summary>
		private void ConnectionClose()
		{
			Dispose(true);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Builds AuthenticationHeader filed for user login.
		/// </summary>
		/// <param name="username">Username of the user.</param>
		/// <param name="password">Password of the user.</param>
		/// <returns>bytes of AuthenticationHeader.</returns>
		private byte[] AuthenticationHeaderBuilder(string username, string password)
		{
			if(username == null || password == null)
				throw new Exception("Username or Password is empty.");
			if(username.Length > 256 || password.Length > 256)
				throw new Exception("Username or Password length can not be more than 256 characters.");
			byte[] temp = new byte[2 +  username.Length + password.Length];
			temp[0] = (byte)username.Length;
			temp[1] = (byte)password.Length;
			InsertStringToBuffer(username + password, temp, 2);
			return temp;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Insert the str to buffer with the initial index of buffer.
		/// </summary>
		/// <param name="str">Desried string for inserting to buffer, str must be ASCFII String.</param>
		/// <param name="buffer">Buffer stream.</param>
		/// <param name="index">Starting position in buffer for inserting str onto buffer.</param>
		private void InsertStringToBuffer(string str, byte[] buffer, int index)
		{
			if(str == null)
				return ;
			if(buffer.Length - index < str.Length)
				throw new Exception("Length of str is greater than buffer length.");
			if(index < 0)
				throw new Exception("index can not be negative.");
			for(int i = 0 ; i < str.Length ; i++)
				buffer[i + index] = (byte)str[i];
			return ;
		}
		//**************************************************************************************************************//
		/*/// <summary>
		/// Reads a maximum of 4096 bytes from the current network stream into buffer and return it.
		/// </summary>
		/// <returns>Return received buffer data, if there are'nt no data then null will be returned.</returns>
		private byte[] Receive()
		{
			try
			{
			int m = 0;
			byte[] buffer = new byte[tcpBufferSize];
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
		}*/
		//**************************************************************************************************************//
		/// <summary>
		/// Reads a maximum of n bytes from the current network stream into buffer and return it.
		/// </summary>
		/// <param name="n">states n bytes for reading from network stream</param>
		/// <returns>Return received buffer data, if there are'nt no data then null will be returned.</returns>
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
				/*catch(EndOfStreamException)
				{
						return -1;
				}*/
			catch/*(Exception)*/
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
		/// <param name="buffer">buffer for writing to network stream.</param>
		/// <returns>If any errors occurred, -1 will be returned otherwise 0.</returns>
		private int Send(byte[] buffer)
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
		/// <param name="buffer">buffer for writing to network stream.</param>
		/// <returns>If any errors occurred, -1 will be returned otherwise 0.</returns>
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
		/// <param name="buffer">buffer for writing to network stream.</param>
		/// <param name="offset">The starting point in buffer at which to begin writing. </param>
		/// <param name="count">The number of bytes to write.</param>
		/// <returns>If any errors occurred, -1 will be returned otherwise 0.</returns>
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
		/*private static bool CertificateValidation (X509Certificate certificate, int[] certificateErrors)
		{
			if(certificateErrors.Length > 0) 
			{
				Console.WriteLine (certificate.ToString (true));
				// X509Certificate.ToString(true) doesn't show dates :-(
				Console.WriteLine ("\tValid From:  {0}", certificate.GetEffectiveDateString ());
				Console.WriteLine ("\tValid Until: {0}{1}", certificate.GetExpirationDateString (), Environment.NewLine);
				// multiple errors are possible using SslClientStream
				foreach (int error in certificateErrors) 
				{
					ShowCertificateError (error);
				}
			}
			// whatever the reason we do not stop the SSL connection
			return true;
		}
		private static void ShowCertificateError (int error) 
		{
			string message = null;
			switch (error) 
			{
				case -2146762490:
					message = "CERT_E_PURPOSE 0x800B0106";
					break;
				case -2146762481:
					message = "CERT_E_CN_NO_MATCH 0x800B010F";
					break;
				case -2146869223:
					message = "TRUST_E_BASIC_CONSTRAINTS 0x80096019";
					break;
				case -2146869232:
					message = "TRUST_E_BAD_DIGEST 0x80096010";
					break;
				case -2146762494:
					message = "CERT_E_VALIDITYPERIODNESTING 0x800B0102";
					break;
				case -2146762495:
					message = "CERT_E_EXPIRED 0x800B0101";
					break;
				case -2146762486:
					message = "CERT_E_CHAINING 0x800B010A";
					break;
				case -2146762487:
					message = "CERT_E_UNTRUSTEDROOT 0x800B0109";
					break;
				default:
					message = "unknown (try WinError.h)";
					break;
			}
			Console.WriteLine ("Error #{0}: {1}", error, message);
		}*/
		//**************************************************************************************************************//
	}
}