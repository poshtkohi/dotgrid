/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections;
using System.Net.Sockets;

using DotGrid.Net;
using DotGrid.DotSec;
using DotGrid.DotDfs;
using DotGrid.Shared.Enums;
using DotGrid.Shared.Headers;
using DotGrid.Shared.Headers.DotDFS;
using DotGrid.Shared.Enums.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Provides fundamental methods for remote directory and file operations based on DotDFS protocol in FTSM mode.
	/// </summary>
	public class DirectoryMovementClient
	{
		private string dotDfsServerAddress;
		private NetworkCredential nc;
		private bool secure;
		private RijndaelEncryption rijndael;
		private SecureBinaryReader reader;
		private SecureBinaryWriter writer;
		private DotGridSocket socket;
		private bool closed = false;
		//**************************************************************************************************************//
		/// <summary>
		/// A constructor for DirectoryMovementClient class.
		/// </summary>
		/// <param name="dotDfsServerAddress">The remote DotDFS server address.</param>
		/// <param name="nc">Provides credentials for password-based authentication schemes to destination DotDFS server.</param>
		/// <param name="secure">Determines secure or secureless connection based on DotGrid.DotSec transfer layer security.</param>
		public DirectoryMovementClient(string dotDfsServerAddress, NetworkCredential nc, bool secure)
		{
			this.dotDfsServerAddress = dotDfsServerAddress;
			this.nc = nc;
			this.secure = secure;
			this.rijndael = rijndael;
			IPHostEntry hostEntry = Dns.Resolve(dotDfsServerAddress);
			IPAddress ip = hostEntry.AddressList[0];
			Socket sock = new Socket (ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			if(sock == null)
				throw new ObjectDisposedException("Could not instantiate from Socket class.");
			sock.Connect(new IPEndPoint (ip, 2799));
			this.rijndael = new RijndaelEncryption(128);
			PublicKeyAuthentication(sock);
			socket = new DotGridSocket(reader, writer);
			try { socket.WriteByte((byte)TransferChannelMode.DirectoryMovementUploadFromClient); }
			catch(Exception e) { Close(); throw e; }
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Creates all directories and subdirectories as specified by path.
		/// </summary>
		/// <param name="path">The directory path to create.</param>
		public void CreateDirectory(string path)
		{
			if(path == null)
				throw new ArgumentNullException("path is null.");
			socket.WriteByte((byte)DirectoryMovementMethods.CreateDirectory);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Deletes a directory and its contents.
		/// </summary>
		/// <param name="path">The name of the empty directory to remove. This directory must be writable or empty. </param>
		public void DeleteDirectory(string path)
		{
			if(path == null)
				throw new ArgumentNullException("path is null.");
			socket.WriteByte((byte)DirectoryMovementMethods.DeleteDirectory);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Deletes the specified file. An exception is not thrown if the specified file does not exist.
		/// </summary>
		/// <param name="path">The name of the file to be deleted. </param>
		public void DeleteFile(string path)
		{
			if(path == null)
				throw new ArgumentNullException("path is null.");
			socket.WriteByte((byte)DirectoryMovementMethods.DeleteFile);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Determines whether the given path refers to an existing directory on remote DotDFS disk.
		/// </summary>
		/// <param name="path">The path to test.</param>
		/// <returns>true if path refers to an existing directory; otherwise, false.</returns>
		public bool ExistsDirectory(string path)
		{
			if(path == null)
				throw new ArgumentNullException("path is null.");
			socket.WriteByte((byte)DirectoryMovementMethods.ExistsDirectory);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
			byte b = socket.ReadByte();
			if(b == 0)
				return false;
			if(b == 1)
				return true;
			else throw new ArgumentException("bad returned boolean value.");
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Determines whether the specified file exists.
		/// </summary>
		/// <param name="path">The file to check. </param>
		/// <returns>true if the caller has the required permissions and path contains the name of an existing file; otherwise, false. This method also returns false if path is a null reference (Nothing in Visual Basic) or a zero-length string. If the caller does not have sufficient permissions to read the specified file, no exception is thrown and the method returns false regardless of the existence of path.</returns>
		public bool ExistsFile(string path)
		{
			if(path == null)
				throw new ArgumentNullException("path is null.");
			socket.WriteByte((byte)DirectoryMovementMethods.ExistsFile);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
			byte b = socket.ReadByte();
			if(b == 0)
				return false;
			if(b == 1)
				return true;
			else throw new ArgumentException("bad returned boolean value.");
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the current working directory of the application.
		/// </summary>
		/// <returns>A string containing the path of the current working directory.</returns>
		public string GetCurrentDirectory()
		{
			socket.WriteByte((byte)DirectoryMovementMethods.GetCurrentDirectory);
			socket.CheckExceptionResponse();
			return (string) socket.ReadObject();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the names of subdirectories in the specified directory.
		/// </summary>
		/// <param name="path">The path for which an array of subdirectory names is returned.</param>
		/// <returns>An array of type String containing the names of subdirectories in path.</returns>
		public string[] GetDirectories(string path)
		{
			if(path == null)
				throw new ArgumentNullException("path is null.");
			socket.WriteByte((byte)DirectoryMovementMethods.GetDirectories);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
			return (string[]) socket.ReadObject();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Returns the names of files in the specified directory.
		/// </summary>
		/// <param name="path">The directory from which to retrieve the files.</param>
		/// <returns>A String array of file names in the specified directory.</returns>
		public string[] GetFiles(string path)
		{
			if(path == null)
				throw new ArgumentNullException("path is null.");
			socket.WriteByte((byte)DirectoryMovementMethods.GetFiles);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
			return (string[]) socket.ReadObject();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Moves a file or a directory and its contents to a new location.
		/// </summary>
		/// <param name="sourceDirName">The path of the file or directory to move. </param>
		/// <param name="destDirName">The path to the new location for sourceDirName. </param>
		public void MoveDirectory(string sourceDirName, string destDirName)
		{
			if(sourceDirName == null)
				throw new ArgumentNullException("sourceDirName is null.");
			if(destDirName == null)
				throw new ArgumentNullException("destDirName is null.");
			string[] path =new string[2];
			path[0] = sourceDirName;
			path[1] = destDirName;
			socket.WriteByte((byte)DirectoryMovementMethods.MoveDirectory);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Moves a specified file to a new location, providing the option to specify a new file name.
		/// </summary>
		/// <param name="sourceFileName">The name of the file to move. </param>
		/// <param name="destFileName">The new path for the file. </param>
		private void MoveFile(string sourceFileName, string destFileName)
		{
			if(sourceFileName == null)
				throw new ArgumentNullException("sourceFileName is null.");
			if(destFileName == null)
				throw new ArgumentNullException("destFileName is null.");
			string[] path =new string[2];
			path[0] = sourceFileName;
			path[1] = destFileName;
			socket.WriteByte((byte)DirectoryMovementMethods.MoveFile);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Downloads a remote file located at DotDFS server to a local file through a single TCP connection.
		/// </summary>
		/// <param name="localFilename">The local file name.</param>
		/// <param name="remoteFilename">The remote file name.</param>
		/// <param name="offset">The point relative to origin from which to begin file transferring.</param>
		/// <param name="length">The maximum number of bytes to transfer.With 0 or -1 length, the length parameter will be set with the real length of the remote file.</param>
		/// <param name="tcpBufferSize">Specifies both TCP Window and file read/write buffer sizes.</param>
		/// <param name="transferredBytes">Finds value of current transferred bytes to remote DotDFS server.</param>
		public void DownloadFile(string localFilename, string remoteFilename, long offset, long length, int tcpBufferSize, ref long transferredBytes)
		{
			if(localFilename == null)
				throw new ArgumentNullException("localFilename is null.");
			if(remoteFilename == null)
				throw new ArgumentNullException("remoteFilename is null.");
			if(tcpBufferSize <= 0)
				tcpBufferSize = 256 * 1024;
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset can not be negative.");
			/*if(length < 0)
				throw new ArgumentOutOfRangeException("length can not be negative.");*/
			FileStream fs =  new FileStream(localFilename, FileMode.Create, FileAccess.Write, FileShare.None);
			try
			{
				fs.Seek(offset, SeekOrigin.Begin);
				socket.FromDownloadDirectoryClient = true;
				socket.WriteByte((byte)DirectoryMovementMethods.DownloadFile);
				socket.WriteObject(new DownloadFileInfo(remoteFilename, tcpBufferSize, offset, length));
				socket.CheckExceptionResponse();
				long lastOffset = 0;
				long lastLength = 0;
				long written = 0;
				lock(this)
				{
					while(true)
					{
						if(closed)
							break;
						FileTransferModeHearderInfo info;
						info = ReadFileBlock();
						if(info == null)// meaning end read.
							break;
						if(info.SeekValue != lastOffset + lastLength)
						{
							fs.Seek(info.SeekValue, SeekOrigin.Begin);
							fs.Write(info.Data, 0, info.Data.Length);
						}
						else fs.Write(info.Data, 0, info.Data.Length);
						lastOffset = info.SeekValue;
						lastLength = info.Data.Length;
						written += info.Data.Length;
						transferredBytes = written;
						socket.CheckExceptionResponse();
					}
				}
				socket.FromDownloadDirectoryClient = false;
			}
			catch(Exception e)
			{
				socket.FromDownloadDirectoryClient = false;
				try { fs.Close(); } 
				catch { } 
				fs = null;
				throw e;
			}
			try { fs.Close(); } 
			catch { } 
			fs = null;
			return ;
		}
		//**************************************************************************************************************//
		private FileTransferModeHearderInfo ReadFileBlock()
		{
			LongMode _l1;
			LongMode _l2;
			byte b = socket.ReadByte();
			if(b == 0)
				return null; // meaning end read.
			_l1 = (LongMode)(b & 0x0F);
			_l2 = (LongMode)((b & 0xF0) >> 4);
			if(!Enum.IsDefined(typeof(LongMode), _l1) && !Enum.IsDefined(typeof(LongMode), _l2))
				throw new ArgumentOutOfRangeException("Not supported LongMode for FileTransferModeHeader.");
			byte[] val1 = socket.Read((int)_l1);
			byte[] val2 = socket.Read((int)_l2);
			if(val1.Length != (int)_l1 && val2.Length != (int)_l2)
				throw new ArgumentException("Bad format for FileTransferModeHeader.");
			long seekValue = (long)LongValueHeader.GetLongNumberFromBytes(val1);
			long readValue = (long)LongValueHeader.GetLongNumberFromBytes(val2);
			byte[] buffer = new byte[readValue];
			int n = socket.Read(buffer, buffer.Length);
			if(n != buffer.Length)
				throw new ArgumentException("Bad format for FileTransferModeHeader.");
			return new FileTransferModeHearderInfo(seekValue, buffer);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Uploads a local file to remote DotDFS server through a single TCP connection.
		/// </summary>
		/// <param name="localFilename">The local file name.</param>
		/// <param name="remoteFilename">The remote file name.</param>
		/// <param name="offset">The point relative to origin from which to begin file transferring. With 0 or -1 length, the length parameter will be set with the real length of the local file.</param>
		/// <param name="length">The maximum number of bytes to transfer, negative or zero length state all content file to transfer.</param>
		/// <param name="tcpBufferSize">Specifies both TCP Window and file read/write buffer sizes.</param>
		/// <param name="transferredBytes">Finds value of current transferred bytes to remote DotDFS server.</param>
		public void UploadFile(string localFilename, string remoteFilename, long offset, long length, int tcpBufferSize, ref long transferredBytes)
		{
			if(localFilename == null)
				throw new ArgumentNullException("localFilename is null.");
			if(remoteFilename == null)
				throw new ArgumentNullException("remoteFilename is null.");
			if(tcpBufferSize <= 0)
				tcpBufferSize = 256 * 1024;
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset can not be negative.");
			/*if(length < 0)
				throw new ArgumentOutOfRangeException("length can not be negative.");*/
			FileStream fs =  new FileStream(localFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
			if(length <= 0)
				length = fs.Length;
			try
			{
				fs.Seek(offset, SeekOrigin.Begin);
				socket.WriteByte((byte)DirectoryMovementMethods.UploadFile);
				socket.WriteObject(remoteFilename);
				socket.CheckExceptionResponse();
				long transferred = 0;
				long a = length /tcpBufferSize;
				long b = length % tcpBufferSize;
				lock(this)
				{
					if(a == 0)
					{
						byte[] array = new byte[length];
						int n = fs.Read(array, 0, array.Length);
						if(n >= 0)
						{
							SendFileHeader(array, fs.Position, n);
							transferred += n;
							transferredBytes = transferred;
						}
					}
					if(a > 0)
					{
						for(int i = 0 ; i < a ; i++)
						{
							if(closed)
								break;
							byte[] array = new byte[tcpBufferSize];
							int n = fs.Read(array, 0, array.Length);
							if(n <= 0)
								break;
							SendFileHeader(array, fs.Position, n);
							transferred += n;
							transferredBytes = transferred;
						}
					}
					if(b >= 0)
					{
						byte[] array = new byte[b];
						int n = fs.Read(array, 0, array.Length);
						if(n >= 0)
						{
							SendFileHeader(array, fs.Position, n);
							transferred += n;
							transferredBytes = transferred;
						}
					}
				}
				/*long transferred = 0;
				lock(this)
				{
					while(true)
					{
						if(transferred >= length)
							break;
						if(closed)
							break;
						byte[] array = new byte[tcpBufferSize];
						int n = fs.Read(array, 0, array.Length);
						if(n <= 0)
							break;
						byte[] seekValue = LongValueHeader.GetBytesOfLongNumber((ulong)(fs.Position - n));
						byte[] readValue = LongValueHeader.GetBytesOfLongNumber((ulong)n);
						byte b = (byte)(seekValue.Length | (readValue.Length << 4));
						if(closed)
							break;
						byte[] buffer = new byte[1 + seekValue.Length + readValue.Length + n];
						buffer[0] = b;
						Array.Copy(seekValue, 0, buffer, 1, seekValue.Length);
						Array.Copy(readValue, 0, buffer, 1 + seekValue.Length, readValue.Length);
						Array.Copy(array, 0, buffer, 1 + seekValue.Length + readValue.Length, n);
						socket.Write(buffer);
						socket.CheckExceptionResponse();
						transferred += n;
						transferredBytes = transferred;
					}
				}*/
				socket.WriteByte(0); // signaling the server about finalization of transferring file blocks.
			}
			catch(Exception e)
			{
				try { fs.Close(); } 
				catch { }
				throw e;
			}
			try { fs.Close(); } 
			catch { } 
			return ;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sends a file header to DotDFS server.
		/// </summary>
		/// <param name="array">The buffer to send.</param>
		/// <param name="position">The current position of local file pointer.</param>
		/// <param name="n">Value of read local file buffer.</param>
		private void SendFileHeader(byte[] array, long position, long n)
		{
			byte[] seekValue = LongValueHeader.GetBytesOfLongNumber((ulong)(position - n));
			byte[] readValue = LongValueHeader.GetBytesOfLongNumber((ulong)n);
			byte b = (byte)(seekValue.Length | (readValue.Length << 4));
			if(closed)
				return ;
			byte[] buffer = new byte[1 + seekValue.Length + readValue.Length + n];
			buffer[0] = b;
			Array.Copy(seekValue, 0, buffer, 1, seekValue.Length);
			Array.Copy(readValue, 0, buffer, 1 + seekValue.Length, readValue.Length);
			Array.Copy(array, 0, buffer, 1 + seekValue.Length + readValue.Length, n);
			socket.Write(buffer);
			socket.CheckExceptionResponse();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the size of the current remote file.
		/// </summary>
		/// <param name="path">The file to get size.</param>
		/// <returns>The size of the current remote file.</returns>
		public long GetFileSize(string path)
		{
			if(path == null)
				throw new ArgumentNullException("path is null.");
			socket.WriteByte((byte)DirectoryMovementMethods.GetFileSize);
			socket.WriteObject(path);
			socket.CheckExceptionResponse();
			return (long) socket.ReadObject();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Closes all remote connections and releases all consumed system handles.
		/// </summary>
		public void Close()
		{
			closed = true;
			if(this.socket == null)
				throw new ObjectDisposedException("could not close a disposed object");
			if(this.socket != null)
			{
				this.socket.Close();
				this.socket = null;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Does Public Key Authentication protocol based on DotSec.
		/// </summary>
		/// <param name="sock">The connected socket to remote endpoint.</param>
		private void PublicKeyAuthentication(Socket sock)
		{
			NetworkStream ns = new NetworkStream(sock, FileAccess.ReadWrite, true);
			reader = new SecureBinaryReader(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			DotGridSocket socket = new DotGridSocket(reader, writer);
			byte[] buffer;
			try { buffer = socket.Read(3 + 128);  } /*public-key.Length + modulus.Length */ 
			catch(Exception e) { Close(); throw e; }
			//Console.WriteLine(buffer.Length);
			if(buffer.Length > 3 + 128 || buffer.Length < 128 + 1)
			{
				Close(); 
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
						Close(); 
						throw new ArgumentOutOfRangeException("Client implementation does'nt support public key in (e,n) with greater than 3 bytes.");
				}
				byte[] modulus = new byte[128];
				Array.Copy(buffer,e.Length , modulus, 0, modulus.Length); 
				RSA rsa = new RSA(e, modulus); // server RSA public key
				SharedKeyHeader skh = new SharedKeyHeader(secure, rsa, rijndael);
				try { socket.Write(skh.Buffer); }   
				catch(Exception ee) { Close(); throw ee; }
				try { socket.CheckExceptionResponse(); } 
				catch(Exception ee) { Close(); throw ee; }
			}
			reader = new SecureBinaryReader(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			writer = new SecureBinaryWriter(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			//this.socket = new DotGridSocket(sock, rijndael);
			socket = new DotGridSocket(reader, writer);
			buffer = AuthenticationHeaderBuilder(nc.UserName, nc.Password);
			try { socket.Write(buffer); } 
			catch(Exception ee) { Close(); throw ee; } //sends AuthenticationHeader
			byte response;
			try { response = socket.ReadByte(); }  
			catch(Exception ee) { Close(); throw ee; }
			switch(response) //considers reponse for authorization.
			{
				case (byte)ClientAuthenticationError.OK:
					break;
				case (byte)ClientAuthenticationError.NO:
				{
					Close();
					throw new System.Security.SecurityException("Username or Password is wrong.");
				}
				case (byte)ClientAuthenticationError.BAD:
				{
					Close();
					throw new ArgumentException("Bad format for n bytes of username and m bytes of password (buffer.Length != 2+n+m and buffer.Length less than 2)");
				}
				default :
				{
					Close();
					throw new ArgumentException("The server replied with an unrecognized code for login state.");
				}
			}
			if(!secure)
			{
				reader = new SecureBinaryReader(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
				writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
				//this.socket = new DotGridSocket(sock, null);
			}
			//reader = null;
			//writer = null;
			buffer = null;
			GC.Collect();
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
	}
}