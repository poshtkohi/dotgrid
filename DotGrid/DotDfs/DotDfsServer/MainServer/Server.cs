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
using DotGrid.Shared.Enums.DotDFS;
using DotGrid.Shared.Enums;
using DotGrid.Shared.Headers;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for Server.
	/// </summary>
	internal class Server
	{
		private RSA rsa;
		private Thread thread;
		private Hashtable sessions;
		private DotGridSocket socket;
		private ArrayList connections;
		private SecureBinaryReader reader;
		private SecureBinaryWriter writer;
		private NetworkCredential nc = new NetworkCredential("user", "pass");////
		//private int __i;
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sock"></param>
		/// <param name="Sessions"></param>
		/// <param name="ServerRSA"></param>
		/// <param name="connections"></param>
		public Server(Socket sock, ref Hashtable Sessions, ref RSA ServerRSA, ref ArrayList connections)
		{
			//this.__i = __i;
			rsa = ServerRSA;
			sessions = Sessions;
			this.connections =  connections;
			reader = new SecureBinaryReader(new NetworkStream(sock, FileAccess.ReadWrite), null, System.Text.Encoding.ASCII);
			writer = new SecureBinaryWriter(new NetworkStream(sock, FileAccess.ReadWrite), null, System.Text.Encoding.ASCII);
			socket = new DotGridSocket(sock, reader, writer);
			thread = new Thread(new ThreadStart(ProtocolInterpreter));
			thread.Start();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		private void ProtocolInterpreter()
		{
			//Console.WriteLine("connetions: " + connections.Count);//
			//Console.WriteLine("session: " + sessions.Count);//
			object[] obj = new object[2];
			obj[0] = thread;
			obj[1] = socket.BaseSocket;
			connections.Add(obj); //
			//Console.WriteLine(connections.Count);
			if(!PublicKeyAuthentication(this.socket.BaseSocket)) {connections.Remove(obj); ThreadExit(); return ; }
			//Console.WriteLine("__i: "+__i);
			TransferChannelMode channelMode;
			try { channelMode = (TransferChannelMode)socket.ReadByte(); }
			catch { connections.Remove(obj); ThreadExit(); return ;}
			if(!Enum.IsDefined(typeof(TransferChannelMode), channelMode))
			{
				try { socket.WriteException(new ArgumentException("Not supported TransferChannelMode parameter.")); }
				catch { }
				connections.Remove(obj); //
				ThreadExit();
				return ;
			}
			if(channelMode == TransferChannelMode.SingleFileTransferUploadFromClient)
			{
				FileTransferInfo info = GetFileTransferInfo();
				if(info == null) { connections.Remove(obj); ThreadExit(); return ; }
				try { socket.WriteNoException(); } 
				catch {connections.Remove(obj); ThreadExit(); return ; }
				if(sessions.Contains(info.GUID))
				{
					SessionClientUploadRequest session = (SessionClientUploadRequest)sessions[info.GUID];
					session.AddNewClientStream(socket);
					connections.Remove(obj);//
					obj[0] = null;
					connections.Add(obj);
					return ;
				}
				else
				{
					//Console.WriteLine(info.WriteFileName);
					SessionClientUploadRequest session = new SessionClientUploadRequest(socket, ref sessions, info, ref connections);
					session.Run();
					connections.Remove(obj);//
					return ;
				}
			}
			if(channelMode == TransferChannelMode.SingleFileTransferDownloadFromClient)
			{
				FileTransferInfo info = GetFileTransferInfo();
				if(info == null) { connections.Remove(obj); ThreadExit(); return ; }
				try { socket.WriteNoException(); } 
				catch {connections.Remove(obj); ThreadExit(); return ; }
				if(sessions.Contains(info.GUID))
				{
					SessionClientDownloadRequest session = (SessionClientDownloadRequest)sessions[info.GUID];
					session.AddNewClientStream(socket);
					connections.Remove(obj);//
					obj[0] = null;
					connections.Add(obj);
					return ;
				}
				else
				{
					SessionClientDownloadRequest session = new SessionClientDownloadRequest(socket, ref sessions, info, ref connections);
					session.Run();
					connections.Remove(obj);//
					return ;
				}
			}
			if(channelMode == TransferChannelMode.DirectoryMovementUploadFromClient)
			{
				ClientDirectoryMovementRequest request = new ClientDirectoryMovementRequest(socket);
				request.Run();
				connections.Remove(obj);//
				ThreadExit();
				return ;
			}
			if(channelMode == TransferChannelMode.FileStreamFromClient)
			{
				FileStreamRequest request = new FileStreamRequest(reader, writer);
				request.Run();
				connections.Remove(obj);//
				return ;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private FileTransferInfo GetFileTransferInfo()
		{
			try { FileTransferInfo info = (FileTransferInfo)socket.ReadObject(); return info; }
			catch(Exception e) 
			{
				try { socket.WriteException(e); }
				catch { }
				return null;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sock"></param>
		/// <returns></returns>
		private bool PublicKeyAuthentication(Socket sock)
		{
			NetworkStream ns = new NetworkStream(sock, FileAccess.ReadWrite, false);
			SecureBinaryReader reader = new SecureBinaryReader(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			SecureBinaryWriter writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			this.socket = new DotGridSocket(sock, reader, writer);
			RSAPublicHeader rph = new RSAPublicHeader(rsa);
			try { socket.Write(rph.Buffer); }   catch { return false; }
			byte[] buffer;
			try { buffer = socket.Read(4096); }  catch { return false; }
			if(buffer.Length < 2 + 3 * 16) // Minimum { [(Secure,encryption)].Length + [len].Length + [(key,iv,md5hash)].Length }
			{
				try { socket.WriteException(new ArgumentException("Bad RSAPublicHeader format.")); }  catch { }
				return false;
			}
			switch((Encryption)(buffer[0] & 0x0F))
			{
				case Encryption.RIJNDAEL:
					//Console.WriteLine(Encryption.RIJNDAEL);//
					break;
					/*case Encryption.T3DES:
						*/
				default:
					try { socket.WriteException(new ArgumentOutOfRangeException("Not supported encryption algorithm.")); }   catch { }
					return false;
			}
			bool secure = false;
			switch((buffer[0] & 0xF0) >> 4)
			{
				case 0:
					secure = false;
					break;
				case 1:
					secure = true;
					break;
				default:
					try { socket.WriteException(new ArgumentOutOfRangeException("Not supported Secure field in RSAPublic header.")); }  catch { }
					return false;
			}
			int len = (int)buffer[1];
			//Console.WriteLine("len: " + len);//
			//Console.WriteLine("secure: " + secure);//
			byte[] temp = new byte[len];
			Array.Copy(buffer, 2, temp, 0, len);
			try { temp = rsa.DecryptData(temp); }
			catch
			{
				try { socket.WriteException(new ArgumentException("Bad format for RSA inputs.")); }   catch { }
				return false;
			}
			if(temp.Length != 3 * 16)
			{
				try { socket.WriteException(new ArgumentException("Bad format for RSA inputs.")); }   catch { }
				return false;
			}
			byte[] KeyIv = new byte[2 * 16];
			byte[] hash = new byte[16];
			Array.Copy(temp, 0, KeyIv, 0, KeyIv.Length);
			Array.Copy(temp, KeyIv.Length, hash, 0, hash.Length);
			byte[] newHash = new MD5().MD5hash(KeyIv);
			for(int i = 0 ; i < newHash.Length ; i++)
				if(newHash[i] != hash[i])
				{
					try { socket.WriteException(new System.Security.SecurityException("The hash with the data is wrong.")); }   catch { }
					return false;
				}
			hash = temp = buffer = newHash = null;
			byte[] key = new byte[16];
			byte[] iv = new byte[16];
			Array.Copy(KeyIv, 0, key, 0, key.Length);
			Array.Copy(KeyIv, key.Length, iv, 0, iv.Length);
			RijndaelEncryption rijndael = new RijndaelEncryption(key, iv);
			key = iv = null;
			try { socket.WriteNoException(); }   catch { return false; }
			reader = new SecureBinaryReader(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			writer = new SecureBinaryWriter(ns, rijndael, System.Text.Encoding.ASCII); // secure connection for username and password
			this.socket = new DotGridSocket(sock, reader, writer);
			try { buffer = this.socket.Read(4096); }   catch { return false; }
			if(buffer == null || buffer.Length < 2)
			{
				try { this.socket.WriteByte((byte)ClientAuthenticationError.BAD); }   catch { }
				return false;
			}
			int n = buffer[0]; //length of username
			int m = buffer[1]; //length of password
			if(buffer.Length != 2 + n + m)
			{
				try { this.socket.WriteByte((byte)ClientAuthenticationError.BAD); }   catch { }
				return false;
			}
			string username = System.Text.ASCIIEncoding.ASCII.GetString(buffer, 2, n);
			string password = System.Text.ASCIIEncoding.ASCII.GetString(buffer, 2 + n , m);
			//Console.WriteLine("\n\nuser: {0}\npass: {1}", username, password);//
			if(username == nc.UserName && password == nc.Password)
			{
				try { this.socket.WriteByte((byte)ClientAuthenticationError.OK); }   catch { return false; }
			}
			else
			{
				try { this.socket.WriteByte((byte)ClientAuthenticationError.NO); }   catch { }
				return false;
			}
			if(!secure)
			{
				reader = new SecureBinaryReader(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
				writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
				this.socket = new DotGridSocket(sock, reader, writer);
			}
			reader = null;
			writer = null;
			buffer = null;
			GC.Collect();
			return true;
		}
		//**************************************************************************************************************//
		private void ThreadExit()
		{
			try { socket.Close(); }
			catch { }
			thread = null;
			GC.Collect();
			return ;
		}
		//**************************************************************************************************************//
	}
}