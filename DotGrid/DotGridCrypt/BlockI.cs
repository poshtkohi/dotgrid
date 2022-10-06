using System;
using DotGrid.DotSec;

namespace DotGridCrypt
{
	/// <summary>
	/// Summary description for BlockI.
	/// </summary>
	public class BlockI
	{
		private byte[] _pureData;
		private int _length;
		private int _PartNum;
		private object _enc = null;
		private Enc _encryptionName;
		public BlockI(object encryption, Enc encryptionName, int PartNum, byte[] pureData, int length)
		{
			if(encryption == null)
				throw new ArgumentNullException("encryption can not be null");
			if(pureData == null)
				throw new ArgumentNullException("pureData can not be null");
			if(length <= 0)
				throw new ArgumentNullException("length can not be zero or negative number");
			if(PartNum < 0)
				throw new ArgumentNullException("PartNum can not be a negative number");
			_pureData = pureData;
			_enc = encryption;
			_encryptionName = encryptionName;
			_length = length;
			_PartNum = PartNum;
		}
		public byte[] Buffer
		{
			get
			{
				byte[] hash = new MD5().MD5hash(_pureData);
				byte[] buffer = new byte[_pureData.Length + hash.Length];
				Array.Copy(_pureData, 0, buffer, 0, _pureData.Length);
				Array.Copy(hash, 0, buffer, _pureData.Length, hash.Length);
				hash = null;
				switch(_encryptionName)
				{
					case Enc.Rijndael:
						buffer = ((RijndaelEncryption)(_enc)).encrypt(buffer, _length);
						break;
					case Enc.TripleDes:
						buffer = ((TripleDESEncryption)(_enc)).encrypt(buffer, _length);
						break;
					case Enc.RC2:
						buffer = ((RC2Encryption)(_enc)).encrypt(buffer, _length);
						break;
					default:
						throw new ArgumentException("encryptionName parameter only can be rijndael, 3des and rsa");
				}
				byte[] temp = new byte[4 + 4 + buffer.Length]; // PartNum + BlockSize + buffer.Length
				temp[0] = (byte)((_PartNum & 0xFF000000) >> 24);
				temp[1] = (byte)((_PartNum & 0x00FF0000) >> 16);
				temp[2] = (byte)((_PartNum & 0x0000FF00) >> 8);
				temp[3] = (byte) (_PartNum & 0x000000FF);
				temp[4] = (byte)((buffer.Length & 0xFF000000) >> 24);
				temp[5] = (byte)((buffer.Length & 0x00FF0000) >> 16);
				temp[6] = (byte)((buffer.Length & 0x0000FF00) >> 8);
				temp[7] = (byte) (buffer.Length & 0x000000FF);
				Array.Copy(buffer, 0, temp, 8, buffer.Length);
				buffer = null;
				return temp;
			}
		}
		/*public static byte[] GetPureData(RijndaelEncryption rijndael, byte[] encryptedData, ref int PartNum)
		{
			if(rijndael == null)
				throw new ArgumentNullException("rijndael can not be null");
			if(encryptedData == null)
				throw new ArgumentNullException("encryptedData can not be null");
			if(encryptedData.Length <= 8)
				throw new ArgumentOutOfRangeException("Bad length for encryptedData");
			PartNum = (encryptedData[0] << 24) | (encryptedData[1]  << 16) | (encryptedData[2]  << 8) | encryptedData[3];
			int BlockSize = (encryptedData[4] << 24) | (encryptedData[5]  << 16) | (encryptedData[6]  << 8) | encryptedData[7];
			if(BlockSize <= 0)
				throw new ArgumentOutOfRangeException("BlockSize for BlockI header is invalid");
			byte[] buffer = new byte[BlockSize];
			Array.Copy(encryptedData, 8, buffer, 0, BlockSize); 
			byte[] temp = rijndael.decrypt(buffer);
			buffer = new byte[temp.Length - 16];
			byte[] hash = new byte[16];
			Array.Copy(temp, 0, buffer, 0, buffer.Length);
			Array.Copy(temp, buffer.Length, hash, 0, hash.Length);
			temp = null;
			byte[] newHash = new MD5().MD5hash(buffer);
			for(int i = 0 ; i < hash.Length ; i++)
				if(hash[i] != newHash[i])
					throw new System.Security.Cryptography.CryptographicException("The hash with the data is wrong.");
			hash = newHash = null;
			return buffer;
		}*/
	}
}