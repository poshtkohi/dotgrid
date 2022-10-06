/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.DotSec
{
	/// <summary>
	/// A class for SecureDataHeader.
	/// </summary>
	internal class SecureDataHeader
	{
		private RijndaelEncryption rijndael;
		private byte[] data;
		private static readonly int DataSize = 32 * 1024; //max data size : 2*2^14 or 32KB
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the SecureDataHeader class.
		/// </summary>
		/// <param name="data">The data buffer for encryption.</param>
		/// <param name="rijndael">A RijndaelEncryption class for encrypting data.</param>
		public SecureDataHeader(byte[] data, RijndaelEncryption rijndael)
		{
			if(data == null)
				throw new ArgumentNullException("Data buffer is a null reference.");
			if(rijndael == null)
				throw new ArgumentNullException("Rijndael is a null reference.");
			if(data.Length > DataSize)
				throw new ArgumentOutOfRangeException(String.Format("Length can not be greater than {0} bytes.", DataSize));
			this.data = data;
			this.rijndael = rijndael;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of SecureDataHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				MD5 md5 = new MD5();
				byte[] hash = md5.MD5hash(this.data);
				byte[] temp = new byte[this.data.Length + hash.Length];
				for(int i = 0 ; i < this.data.Length ; i++)
					temp[i] = data[i];
				for(int i = 0 ; i < hash.Length ; i++)
					temp[i + data.Length] = hash[i];
				md5 = null;
				hash = null;
				byte[] encrypted = rijndael.encrypt(temp);
				byte[] buffer = new byte[2 + encrypted.Length]; // length + (Encrypted-Data-Hash).Length
				buffer[0] = (byte)((encrypted.Length & 0xFF00) >> 8);
				buffer[1] = (byte) (encrypted.Length & 0x00FF);
				for(int i = 0 ; i < encrypted.Length ; i++)
					buffer[2 + i] = encrypted[i];
				encrypted = null;
				return buffer;
			}
		}
		//**************************************************************************************************************//
	}
}