/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

using DotGrid.Shared.Enums.DotDFS;
using DotGrid.DotSec;

namespace DotGrid.Shared.Headers.DotDFS
{
	/// <summary>
	/// Summary description for RSAPublicHeader.
	/// </summary>
	public class RSAPublicHeader
	{
		private RSA rsa;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the RSAPublicHeader class.
		/// </summary>
		/// <param name="ServerRSA">States RSA public key (e,n) of server.</param>
		public RSAPublicHeader(RSA ServerRSA)
		{
			if(ServerRSA == null)
				throw new ArgumentNullException("ServerRSA is a null reference.");
			this.rsa = ServerRSA;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets bytes of RSAPublicHeader instance.
		/// </summary>
		public byte[] Buffer
		{
			get
			{
				byte[] buffer = new byte[rsa.EPublic.Length + rsa.N.Length];
				Array.Copy(rsa.EPublic, 0, buffer, 0, rsa.EPublic.Length);
				Array.Copy(rsa.N, 0, buffer, rsa.EPublic.Length, rsa.N.Length);
				//Console.WriteLine("buffer length: " + buffer.Length);
				//Console.WriteLine(rsa.EPublic.Length);
				return buffer;
			}
		}
		//**************************************************************************************************************//
	}
}