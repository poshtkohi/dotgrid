/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Specifies necessary information for downloading a requested file from client to DotDFS server.
	/// </summary>
	[Serializable]
	internal class DownloadFileInfo
	{
		private string remoteFilename;
		private int tcpBufferSize;
		private long offset;
		private long length;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes an DownloadFileInfo instance. 
		/// </summary>
		/// <param name="remoteFilename">The remote file name.</param>
		/// <param name="tcpBufferSize">TCP Window size.</param>
		/// <param name="offset">The offset in which downloading remote file will be started.</param>
		/// <param name="length">Value in which downloading remote file will be finished.With 0 or -1 length, the length parameter will be set with the real length of the remote file.</param>
		public DownloadFileInfo(string remoteFilename, int tcpBufferSize, long offset, long length)
		{
			if(remoteFilename == null)
				throw new ArgumentNullException("remoteFilename is a null reference.");
			if(tcpBufferSize <= 0)
				tcpBufferSize = 256 * 1024;
			if(offset < 0)
				throw new ArgumentOutOfRangeException("offset can not be negative.");
			/*if(length <= 0)
				throw new ArgumentOutOfRangeException("length can not be negative or zero.");*/
			this.remoteFilename = remoteFilename;
			this.tcpBufferSize = tcpBufferSize;
			this.offset = offset;
			this.length = length;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the remote file name.
		/// </summary>
		public string RemoteFilename
		{
			get
			{
				return this.remoteFilename;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the TCP Windows size.
		/// </summary>
		public int TcpBufferSize
		{
			get
			{
				return this.tcpBufferSize;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the offset seek.
		/// </summary>
		public long Offset
		{
			get
			{
				return this.offset;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets or sets the specified file length.
		/// </summary>
		public long Length
		{
			get
			{
				return this.length;
			}
			set
			{
				this.length = value;
			}
		}
		//**************************************************************************************************************//
	}
}