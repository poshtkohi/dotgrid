/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

using DotGrid.Shared.Enums.DotDFS;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Specifies necessary information for FileTransfer server mode.
	/// </summary>
	[Serializable]
	internal class FileTransferInfo
	{
		private string guid;
		private string writeFilename;
		private int parallel;
		private int _BufferSize;
		private long fileSize;
		//**************************************************************************************************************//
		/// <summary>
		/// Initializes a new instance of the FileTransferInfo class.
		/// </summary>
		/// <param name="guid">Unique ID of this instance.</param>
		/// <param name="writeFilename">Filename to write it.</param>
		/// <param name="fileSize">Length of this file.</param>
		/// <param name="parallel">Specifies numbers of parallel TCP/IP connections for FileTransfer server mode.</param>
		/// <param name="tcpBufferSize">Specifies buffer length to write.</param>
		public FileTransferInfo(string guid, string writeFilename, long fileSize, int  parallel, int tcpBufferSize)
		{
			this.guid = guid;
			this.writeFilename = writeFilename;
			this.parallel = parallel;
			this._BufferSize = tcpBufferSize;
			this.fileSize = fileSize;
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets unique ID of this instance.
		/// </summary>
		public string GUID
		{
			get
			{
				return guid;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the filename of this instance to write it.
		/// </summary>
		public string WriteFileName
		{
			get
			{
				return writeFilename;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the tcpBufferSize of this instance.
		/// </summary>
		public int tcpBufferSize
		{
			get
			{
				return _BufferSize;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets the numbers of parallel TCP/IP connections of this instance.
		/// </summary>
		public int ParallelSize
		{
			get
			{
				return parallel;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets file size of this instance.
		/// </summary>
		public long FileSize
		{
			get
			{
				return fileSize;
			}
		}
		//**************************************************************************************************************//
	}
}
