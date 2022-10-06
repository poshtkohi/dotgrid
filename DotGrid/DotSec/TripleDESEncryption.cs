/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace DotGrid.DotSec
{
	/// <summary>
	/// A complete class for triple DES encryption.
	/// </summary>
	[Serializable]
	public class  TripleDESEncryption
	{
		private byte[] key;
		private byte[] IV;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of TripleDESEncryption with key and iv.
		/// </summary>
		/// <param name="key">The secret key for the symmetric algorithm.</param>
		/// <param name="IV">The initialization vector (IV) for the symmetric algorithm.</param>
		public TripleDESEncryption(byte[] key, byte[] IV)
		{
			if(key == null)
				throw new ArgumentNullException("Key is a null reference.");
			if(IV == null)
				throw new ArgumentNullException("IV is a null reference.");
			this.key = key;
			this.IV = IV;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of TripleDESEncryption with random generated key and iv and desired key size.
		/// </summary>
		/// <param name="KeySize">Desired key size.</param>
		public TripleDESEncryption(int KeySize)
		{
			TripleDESCryptoServiceProvider  des = new TripleDESCryptoServiceProvider();
			des.KeySize = KeySize;
			des.GenerateKey();
			des.GenerateIV();
			this.key = des.Key;
			this.IV = des.IV;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of RijndaelEncryption with random generated key and iv.
		/// </summary>
		public TripleDESEncryption()
		{
			TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider();
			des.GenerateKey();
			des.GenerateIV();
			this.key = des.Key;
			this.IV = des.IV;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Encrypts the input data using the secret key.
		/// </summary>
		/// <param name="dataForEncryption">The cipher text to be encrypted.</param>
		/// <returns>The resulting encryption of the dataForEncryption parameter in plain text.</returns>
		/// <returns></returns>
		public byte[] encrypt(byte []dataForEncryption)
		{
			if(key == null)
				throw new ArgumentNullException("Key is a null reference.");
			if(IV == null)
				throw new ArgumentNullException("IV is a null reference.");
			if(dataForEncryption == null)
				throw new ArgumentNullException("dataForEncryption is a null reference.");
			TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider();
			ICryptoTransform encryptor = des.CreateEncryptor(this.key,this.IV);
			MemoryStream msEncrypt = new MemoryStream();
			CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
			csEncrypt.Write(dataForEncryption, 0, dataForEncryption.Length);
			csEncrypt.FlushFinalBlock();
			byte[] result =  msEncrypt.ToArray();
			msEncrypt.Close();
			csEncrypt.Close();
			encryptor.Dispose();
			des.Clear();
			return result;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Encrypts the input data using the secret key.
		/// </summary>
		/// <param name="dataForEncryption">The cipher text to be encrypted.</param>
		/// <param name="length">The maximum number of bytes to encrypt.</param>
		/// <returns>The resulting encryption of the dataForEncryption parameter in plain text.</returns>
		/// <returns></returns>
		public byte[] encrypt(byte []dataForEncryption, int length)
		{
			if(key == null)
				throw new ArgumentNullException("Key is a null reference.");
			if(IV == null)
				throw new ArgumentNullException("IV is a null reference.");
			if(dataForEncryption == null)
				throw new ArgumentNullException("dataForEncryption is a null reference.");
			TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider();
			ICryptoTransform encryptor = des.CreateEncryptor(this.key,this.IV);
			MemoryStream msEncrypt = new MemoryStream();
			CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
			csEncrypt.Write(dataForEncryption, 0, length);
			csEncrypt.FlushFinalBlock();
			byte[] result =  msEncrypt.ToArray();
			msEncrypt.Close();
			csEncrypt.Close();
			encryptor.Dispose();
			des.Clear();
			return result;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Decrypts the input data using the secret key.
		/// </summary>
		/// <param name="dataForDecryption">The cipher text to be decrypted.</param>
		/// <returns>The resulting decryption of the dataForDecryption parameter in plain text.</returns>
		public byte[] decrypt(byte []dataForDecryption)
		{
			if(key == null)
				throw new ArgumentNullException("Key is a null reference.");
			if(IV == null)
				throw new ArgumentNullException("IV is a null reference.");
			if(dataForDecryption == null)
				throw new ArgumentNullException("dataForDecryption is a null reference.");
			TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider();
			ICryptoTransform decryptor = des.CreateDecryptor(this.key,this.IV);
			MemoryStream msDecrypt = new MemoryStream(dataForDecryption);
			CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
			byte[] fromEncrypt = new byte[dataForDecryption.Length];
			int n = csDecrypt.Read(fromEncrypt, 0, fromEncrypt.Length);
			byte[] buffer = new byte[n];
			for(int i = 0 ; i < buffer.Length ; i++)
				buffer[i] = fromEncrypt[i];
			msDecrypt.Close();
			csDecrypt.Close();
			des.Clear();
			return buffer;

		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the secret key for the symmetric algorithm.
		/// </summary>
		public byte[] Key 
		{
			get 
			{ 
				return this.key; 
			}
			set 
			{
				this.key = value; 
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the initialization vector (IV) for the symmetric algorithm.
		/// </summary>
		public byte[] Iv 
		{
			get 
			{ 
				return this.IV; 
			}
			set 
			{
				this.IV = value;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="len"></param>
		/// <returns></returns>
		public static string BinaryToBase64String(byte[] buffer, int len)
		{
			return Convert.ToBase64String(buffer, 0, len);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public static byte[] Base64StringToBinary(string buffer)
		{
			return Convert.FromBase64String(buffer);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public static string BinaryToString(byte[] buffer)
		{
			UTF8Encoding utf8 = new UTF8Encoding();
			return utf8.GetString(buffer);
		}
		//**************************************************************************************************************//
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns></returns>
		public static byte[] StringToBinary(string buffer)
		{
			UTF8Encoding utf8 = new UTF8Encoding();
			return utf8.GetBytes(buffer); 
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Exports Key and IV of this TripleDESEncryption instance to a file.
		/// </summary>
		/// <param name="filename">The desired exported file name.</param>
		/// <returns>Retuens 0, if export operation has no error.</returns>
		public int ExportKeyToFile(string filename)
		{
			if(key == null)
				throw new ArgumentNullException("Key is a null reference.");
			if(IV == null)
				throw new ArgumentNullException("IV is a null reference.");
			if(File.Exists(filename))
				return -1;
			else
			{
				try
				{
					FileStream fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write, FileShare.None);
					string content = "(Key:" +  TripleDESEncryption.BinaryToBase64String(this.Key, this.Key.Length) +
						")     (IV:" + TripleDESEncryption.BinaryToBase64String(this.IV, this.IV.Length) + ")";
					UTF8Encoding utf8 = new UTF8Encoding();
					byte []buffer = utf8.GetBytes(content);
					fs.Write(buffer, 0, buffer.Length);
					fs.Close();
					return 0;
				}
				catch
				{
					return -3;
				}
			}
		}
		//**************************************************************************************************************//
	}
}