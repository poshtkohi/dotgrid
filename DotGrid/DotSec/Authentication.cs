/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/


using System;
using System.Net.Sockets;

using DotGrid.DotSec;
using DotGrid.Shared.Enums;
using DotGrid.Shared.Headers;

namespace DotGrid.DotSec
{
	/// <summary>
	/// Summary description for Authentication.
	/// </summary>
	public class Authentication
	{
		//private RSA ServerRSA;
		//private NetworkStream ns;
		//**************************************************************************************************************//
		/*/// <summary>
		/// Does Public Key Authentication protocol.
		/// </summary>
		/// <param name="ns">The network stream.</param>
		/// <returns>If PublicKeyAuthentication protocol opartions is done without error, true will be returned.</returns>
		*//*static public bool PublicKeyAuthentication()
		{
			SecureBinaryReader reader = new SecureBinaryReader(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			SecureBinaryWriter writer = new SecureBinaryWriter(ns, null, System.Text.ASCIIEncoding.ASCII); // none secure connection
			RSAPublicHeader rph = new RSAPublicHeader(ServerRSA);
			if(Send(rph.Buffer) == -1)
				return false;
			byte[] buffer = null;//Receive(4096);
			if(buffer == null) 
				return false;
			if(buffer.Length < 2 + 3 * 16) // Minimum { [(Secure,encryption)].Length + [len].Length + [(key,iv,md5hash)].Length }
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad RSAPublicHeader format."))).Buffer) == -1) 
					return false;
				return false;
			}
			switch((Encryption)(buffer[0] & 0x0F))
			{
				case Encryption.RIJNDAEL:
					//Console.WriteLine(Encryption.RIJNDAEL);//
					break;
					//case Encryption.T3DES:
				default:
					if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not supported encryption algorithm."))).Buffer) == -1) 
						return false;
					return false;
			}
			switch((buffer[0] & 0xF0) >> 4)
			{
				case 0:
					this.secure = false;
					break;
				case 1:
					this.secure = true;
					break;
				default:
					if(Send((new ExceptionHandlingHeader(new ArgumentOutOfRangeException("Not supported Secure field in RSAPublic header."))).Buffer) == -1) 
						return false;
					return false;
			}
			int len = (int)buffer[1];
			byte[] temp = new byte[len];
			Array.Copy(buffer, 2, temp, 0, len);
			try { temp = ServerRSA.DecryptData(temp); }
			catch
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad format for RSA inputs."))).Buffer) == -1) 
					return false;
				return false;
			}
			if(temp.Length != 3 * 16)
			{
				if(Send((new ExceptionHandlingHeader(new ArgumentException("Bad format for RSA inputs."))).Buffer) == -1) 
					return false;
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
					if(Send((new ExceptionHandlingHeader(new System.Security.SecurityException("The hash with the data is wrong."))).Buffer) == -1) 
						return false;
					return false;
				}
			hash = temp = buffer = newHash = null;
			byte[] key = new byte[16];
			byte[] iv = new byte[16];
			Array.Copy(KeyIv, 0, key, 0, key.Length);
			Array.Copy(KeyIv, key.Length, iv, 0, iv.Length);
			rijndael = new RijndaelEncryption(key, iv);
			key = iv = null;
			if(Send((byte)eXception.NO) == -1) 
				return false;
			return true;
		}*/
		//**************************************************************************************************************//
	}
}
