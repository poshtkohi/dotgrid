/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

using DotGrid.DotSec;
using DotGrid.Shared.Enums;

namespace DotGrid.Shared.Headers
{
	/// <summary>
	/// Summary description for SharedKeyHeader.
	/// </summary>
	public class SharedKeyHeader
	{
		private bool secure;
		private RSA rsa;
		private RijndaelEncryption rijndael;
		/// <summary>
		/// Initializes a new instance of the SetLengthHeader class.
		/// </summary>
		/// <param name="Secure">Determines secure or secureless connection.</param>
		/// <param name="ServerRSA">States RSA public key (e,n) of server.</param>
		/// <param name="rijndael">Shows shared key with clinet and server.</param>
		//**************************************************************************************************************//
		public SharedKeyHeader(bool Secure, RSA ServerRSA, RijndaelEncryption rijndael)
		{
			if(ServerRSA == null) 
				throw new ArgumentNullException("ServerRSA is a null reference.");
			if(rijndael == null) 
				throw new ArgumentNullException("Rijndael is a null reference.");
			this.secure = Secure;
			this.rsa = ServerRSA;
			this.rijndael = rijndael;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of  SharedKeyHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				byte[] buffer = new byte[rijndael.Key.Length + rijndael.Iv.Length + 16];
				Array.Copy(rijndael.Key, 0 , buffer, 0, rijndael.Key.Length);
				Array.Copy(rijndael.Iv, 0 , buffer, rijndael.Key.Length, rijndael.Iv.Length);
				byte[] temp = new MD5().MD5hash(buffer, 0, rijndael.Key.Length + rijndael.Iv.Length);
				Array.Copy(temp, 0, buffer, rijndael.Key.Length + rijndael.Iv.Length, temp.Length);
				buffer = rsa.EncryptData(buffer);
				temp = new byte[2 + buffer.Length]; // [(Secure,encryption)].Length + [len].length + [encrypted-key-iv-md5hash].Length
				temp[0] = (byte)Encryption.RIJNDAEL; // encryption
				temp[0] |= (byte)(Convert.ToInt32(secure) << 4);  // secure
				temp[1] = (byte)buffer.Length; // len
				Array.Copy(buffer, 0, temp, 2, buffer.Length);
				return temp;
			}
		}
		//**************************************************************************************************************//
	}
}
