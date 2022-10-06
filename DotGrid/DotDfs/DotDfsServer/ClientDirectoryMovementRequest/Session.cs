/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Collections;

using DotGrid.Net;
using DotGrid.DotDfs;
using DotGrid.Shared.Headers.DotDFS;
using DotGrid.Shared.Enums.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for Session.
	/// </summary>
	internal class ClientDirectoryMovementRequest
	{
		private DotGridSocket socket;
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		public ClientDirectoryMovementRequest(DotGridSocket socket)
		{
			this.socket = socket;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Runs the session.
		/// </summary>
		public void Run()
		{
			//Console.WriteLine("---------------------------------------------------------");
			//Console.WriteLine("New DotDFS file transfer session in FTSM mode.");
			while(true)
			{
				DirectoryMovementMethods method;
				try { method = (DirectoryMovementMethods)socket.ReadByte(); }
				catch { return ; }
				switch(method)
				{
					case DirectoryMovementMethods.CloseTransferChannel:
						goto End;
					case DirectoryMovementMethods.CreateDirectory:
						if(CreateDirectory() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.DeleteDirectory:
						if(DeleteDirectory() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.DeleteFile:
						if(DeleteFile() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.ExistsDirectory:
						if(ExistsDirectory() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.ExistsFile:
						if(ExistsFile() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.GetCurrentDirectory:
						if(GetCurrentDirectory() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.GetDirectories:
						if(GetDirectories() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.GetFiles:
						if(GetFiles() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.MoveDirectory:
						if(MoveDirectory() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.MoveFile:
						if(MoveFile() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.DownloadFile:
						if(DownloadFile() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.UploadFile:
						if(UploadFile() == -1)
							goto End;
						else break;
					case DirectoryMovementMethods.GetFileSize:
						if(GetFileSize() == -1)
							goto End;
						else break;
					default:
						try { socket.WriteException(new ArgumentException("Directory movement method not supported.")); break; }
						catch { goto End; }
				}
			}
			End:
				//socket.Close();
				GC.Collect();
			    return ;
		}
		//**************************************************************************************************************//
		private int CreateDirectory()
		{
			string path;
			try { path = (string)socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { Directory.CreateDirectory(path); }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { socket.WriteNoException(); return 0; }
			catch { return -1; }
		}
		//**************************************************************************************************************//
		private int DeleteDirectory()
		{
			string path;
			try { path = (string)socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { Directory.Delete(path, true); }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { socket.WriteNoException(); return 0; }
			catch { return -1; }
		}
		//**************************************************************************************************************//
		private int DeleteFile()
		{
			string path;
			try { path = (string)socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { File.Delete(path); }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { socket.WriteNoException(); return 0; }
			catch { return -1; }
		}
		//**************************************************************************************************************//
		private int ExistsDirectory()
		{
			string path;
			try { path = (string)socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try 
			{ 
				byte exists = 0;
				if(Directory.Exists(path))
					exists = 1;
				try { socket.WriteNoException(); }
				catch { return -1; }
				try { socket.WriteByte(exists); return 0; }
				catch {return -1; }
			}
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
		}
		//**************************************************************************************************************//
		private int ExistsFile()
		{
			string path;
			try { path = (string)socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try 
			{ 
				byte exists = 0;
				if(File.Exists(path))
					exists = 1;
				try { socket.WriteNoException(); }
				catch { return -1; }
				try { socket.WriteByte(exists); return 0; }
				catch {return -1; }
			}
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
		}
		//**************************************************************************************************************//
		private int GetCurrentDirectory()
		{
			try 
			{
				string path = Directory.GetCurrentDirectory();
				try { socket.WriteNoException(); }
				catch { return -1; }
				try { socket.WriteObject(path); return 0; }
				catch {return -1; }
			}
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
		}
		//**************************************************************************************************************//
		private int GetDirectories()
		{
			string path;
			try { path = (string)socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try 
			{
				string[] dirs = Directory.GetDirectories(path);
				try { socket.WriteNoException(); }
				catch { return -1; }
				try { socket.WriteObject(dirs); return 0; }
				catch {return -1; }
			}
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
		}
		//**************************************************************************************************************//
		private int GetFiles()
		{
			string path;
			try { path = (string)socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try 
			{
				string[] files = Directory.GetFiles(path);
				try { socket.WriteNoException(); }
				catch { return -1; }
				try { socket.WriteObject(files); return 0; }
				catch {return -1; }
			}
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
		}
		//**************************************************************************************************************//
		private int MoveDirectory()
		{
			string[] path;
			try { path = (string[])socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { Directory.Move(path[0], path[1]); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { socket.WriteNoException(); return 0;}
			catch { return -1; }
		}
		//**************************************************************************************************************//
		private int MoveFile()
		{
			string[] path;
			try { path = (string[])socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { File.Move(path[0], path[1]); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { socket.WriteNoException(); return 0;}
			catch { return -1; }
		}
		//**************************************************************************************************************//
		private int DownloadFile()
		{
			DownloadFileInfo info;
			try { info = (DownloadFileInfo) socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			FileStream fs;
			try 
			{ 
				fs = new FileStream(info.RemoteFilename, FileMode.Open, FileAccess.Read, FileShare.None); 
				if(info.Length <= 0)
					info.Length = fs.Length;
				fs.Seek(info.Offset, SeekOrigin.Begin); 
			}
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { socket.WriteNoException(); }
			catch { try { fs.Close(); } catch { } fs = null; return -1; }
			//long transferred = 0;
			int tcpBufferSize = 256 * 1024;
			if(info.TcpBufferSize > 0)
				tcpBufferSize = info.TcpBufferSize;
			long a = info.Length /tcpBufferSize;
			long b = info.Length % tcpBufferSize;
			try
			{
				if(a == 0)
				{
					byte[] array = new byte[info.Length];
					int n = fs.Read(array, 0, array.Length);
					if(n >= 0)
					{
						SendFileHeader(array, fs.Position, n);
						//transferred += n;
					}
				}
				if(a > 0)
				{
					byte[] array = new byte[tcpBufferSize];
					for(int i = 0 ; i < a ; i++)
					{
						int n = fs.Read(array, 0, array.Length);
						if(n <= 0)
							break;
						SendFileHeader(array, fs.Position, n);
						//transferred += n;
					}
				}
				if(b >= 0)
				{
					byte[] array = new byte[b];
					int n = fs.Read(array, 0, array.Length);
					if(n >= 0)
					{
						SendFileHeader(array, fs.Position, n);
						//transferred += n;
					}
				}
				socket.WriteByte(0); // signaling the client about finalization of transferring file blocks.
			}
			catch(ObjectDisposedException) { try { fs.Close(); } catch { } fs = null; return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; }
				catch { try { fs.Close(); } catch { } fs = null; return -1; }
			}
			try { fs.Close(); }
			catch { }
			fs = null;
			return 0;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Sends a file header to DotDFS client.
		/// </summary>
		/// <param name="array">The buffer to send.</param>
		/// <param name="position">The current position of local file pointer.</param>
		/// <param name="n">Value of read local file buffer.</param>
		private void SendFileHeader(byte[] array, long position, long n)
		{
			byte[] seekValue = LongValueHeader.GetBytesOfLongNumber((ulong)(position - n));
			byte[] readValue = LongValueHeader.GetBytesOfLongNumber((ulong)n);
			byte b = (byte)(seekValue.Length | (readValue.Length << 4));
			byte[] buffer = new byte[1 + seekValue.Length + readValue.Length + n];
			buffer[0] = b;
			Array.Copy(seekValue, 0, buffer, 1, seekValue.Length);
			Array.Copy(readValue, 0, buffer, 1 + seekValue.Length, readValue.Length);
			Array.Copy(array, 0, buffer, 1 + seekValue.Length + readValue.Length, n);
			socket.Write(buffer);
			socket.WriteNoException();
		}
		//**************************************************************************************************************//
		private int UploadFile()
		{
			string path;
			try { path = (string) socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			FileStream fs;
			try { fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None); }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try { socket.WriteNoException(); }
			catch { try { fs.Close(); } catch { } fs = null; return -1; }
			long lastOffset = 0;
			long lastLength = 0;
			long written = 0;
			while(true)
			{
				FileTransferModeHearderInfo info;
				try 
				{
					info = ReadFileBlock();
					if(info == null) // meaning end read.
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
				}
				catch(ObjectDisposedException) { try { fs.Close(); } catch { } fs = null; return -1; }
				catch(Exception e)
				{
					try { socket.WriteException(e) ; }
					catch { try { fs.Close(); } catch { } fs = null; return -1; }
				}
				try { socket.WriteNoException(); }
				catch { try { fs.Close(); } catch { } fs = null; return -1; }
			}
			fs.Close();
			return 0;
		}
		//**************************************************************************************************************//
		private int GetFileSize()
		{
			string path;
			try { path = (string)socket.ReadObject(); }
			catch(ObjectDisposedException) { return -1; }
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
			try 
			{ 
				FileInfo info = new FileInfo(path);
				try { socket.WriteNoException(); }
				catch { return -1; }
				try { socket.WriteObject(info.Length); return 0; }
				catch {return -1; }
			}
			catch(Exception e)
			{
				try { socket.WriteException(e) ; return 0; }
				catch { return -1; }
			}
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
	}
}