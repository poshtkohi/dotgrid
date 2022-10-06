/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Security.Cryptography;

namespace DotGrid.DotSec
{
	/// <summary>
	/// A complete class for Asymetric RSA encryption algorithm.
	/// </summary>
	[Serializable]
	public class RSA
	{
		private RSAParameters rsaPubParams;
		private RSAParameters rsaPrivateParams;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of RSA with public key (e,n) and private key (d,n).
		/// </summary>
		/// <param name="dPrivate">dPrivate is known as the secret exponent or decryption exponent.</param>
		/// <param name="n">n is known as the modulus.</param>
		/// <param name="ePublic">ePublic is known as the public exponent or encryption exponent.</param>
		public RSA(byte[] dPrivate, byte[] n, byte[] ePublic)
		{
			this.rsaPrivateParams.D = dPrivate;
			this.rsaPrivateParams.Modulus = n;
			this.rsaPubParams.Exponent = ePublic;
			this.rsaPubParams.Modulus = n;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of RSA with public key (e,n).
		/// </summary>
		/// <param name="ePublic">ePublic is known as the public exponent or encryption exponent.</param>
		/// <param name="n">n is known as the modulus.</param>
		public RSA(byte[] ePublic, byte[] n)
		{
			this.rsaPubParams.Exponent = ePublic;
			this.rsaPubParams.Modulus = n;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of RSA and generates random public-private keys.
		/// </summary>
		public RSA()
		{
			//System.Security.Cryptography.CspParameters p = new CspParameters(
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider(1024);
			//Generate public and private key data.
			rsaPrivateParams = rsaCSP.ExportParameters(true);
			rsaPubParams = rsaCSP.ExportParameters(false);
			rsaCSP.Clear();
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of RSA.
		/// </summary>
		/// <param name="PublicParams">Represents the public key for the RSA algorithm.</param>
		/// <param name="PrivatePramas">Represents the private key for the RSA algorithm.</param>
		protected RSA(RSAParameters PublicParams, RSAParameters PrivatePramas)
		{
			this.rsaPubParams = PublicParams;
			this.rsaPrivateParams = PrivateParams;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// DPrivate is known as the secret exponent or decryption exponent.
		/// </summary>
		public byte[] DPrivate
		{
			get
			{
				return this.rsaPrivateParams.D;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// N is known as the modulus.
		/// </summary>
		public byte[] N
		{
			get
			{
				if(this.rsaPrivateParams.Modulus != null)
					return this.rsaPrivateParams.Modulus;
				if(this.rsaPubParams.Modulus != null)
					return this.rsaPubParams.Modulus;
				else return null;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// EPublic is known as the public exponent or encryption exponent.
		/// </summary>
		public byte[] EPublic
		{
			get
			{
				return this.rsaPubParams.Exponent;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Get the public key for the algorithm instance.
		/// </summary>
		private RSAParameters PublicParams
		{
			get
			{
				return this.rsaPubParams;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Get the public key for the algorithm instance.
		/// </summary>
		private RSAParameters PrivateParams
		{
			get
			{
				return this.rsaPrivateParams;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Encrypts the input data using the private key.
		/// </summary>
		/// <param name="rgb">The cipher text to be encrypted.</param>
		/// <returns>The resulting encryption of the rgb parameter in plain text.</returns>
		public byte[] EncryptData(byte[] rgb)
		{
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider(1024);
			rsaCSP.ImportParameters(this.rsaPubParams);
			byte[] encrypted = rsaCSP.Encrypt(rgb, false);
			rsaCSP.Clear();
			return encrypted;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Encrypts the input data using the private key.
		/// </summary>
		/// <param name="rgb">The cipher text to be encrypted.</param>
		/// <param name="length">The maximum number of bytes to encrypt.</param>
		/// <returns>The resulting encryption of the rgb parameter in plain text.</returns>
		public byte[] EncryptData(byte[] rgb, int length)
		{
			if(length <= 0)
				throw new ArgumentOutOfRangeException("length can not be zero or negative");
			byte[] temp = new byte[length];
			Array.Copy(rgb, 0, temp, 0, temp.Length);
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider(1024);
			rsaCSP.ImportParameters(this.rsaPubParams);
			byte[] encrypted = rsaCSP.Encrypt(temp, false);
			temp = null;
			rsaCSP.Clear();
			return encrypted;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Decrypts the input data using the private key.
		/// </summary>
		/// <param name="rgb">The cipher text to be decrypted.</param>
		/// <returns>The resulting decryption of the rgb parameter in plain text.</returns>
		public byte[] DecryptData(byte[] rgb)
		{
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider(1024);
			rsaCSP.ImportParameters(this.PrivateParams);
			byte[] decrypted = rsaCSP.Decrypt(rgb, false);
			rsaCSP.Clear();
			return decrypted;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Manually performs hash and then signs hashed value.
		/// </summary>
		/// <param name="encrypted">Encrypted data.</param>
		/// <returns>Signature data.</returns>
		public byte[] HashAndSign(byte[] encrypted)
		{
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider(1024);
			SHA1Managed hash = new SHA1Managed();
			byte[] hashedData;
			rsaCSP.ImportParameters(rsaPrivateParams);
			hashedData = hash.ComputeHash(encrypted);
			hashedData = rsaCSP.SignHash(hashedData, CryptoConfig.MapNameToOID("SHA1"));
			hash.Clear();
			rsaCSP.Clear();
			return hashedData;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Manually performs hash and then verifies hashed value.
		/// </summary>
		/// <param name="encrypted">Bytes of encrypted data</param>
		/// <param name="signature">Bytes of signature.</param>
		/// <returns>Teue if hash is verified.</returns>
		public bool VerifyHash(byte[] encrypted, byte[] signature)
		{
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider(1024);
			SHA1Managed hash = new SHA1Managed();
			byte[] hashedData;
			rsaCSP.ImportParameters(rsaPubParams);
			hashedData = hash.ComputeHash(encrypted);
			bool ret = rsaCSP.VerifyHash(hashedData, CryptoConfig.MapNameToOID("SHA1"), signature);
			hash.Clear();
			rsaCSP.Clear();
			return ret;
		}
		//**************************************************************************************************************//
	}
}