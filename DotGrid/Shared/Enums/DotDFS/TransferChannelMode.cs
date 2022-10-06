/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;

namespace DotGrid.Shared.Enums.DotDFS
{
	/// <summary>
	/// Enum for DotDFS.
	/// </summary>
	public enum TransferChannelMode
	{
		/// <summary>
		/// Meaning transferring a single file on the DotDFS session from client to server.
		/// </summary>
		SingleFileTransferUploadFromClient = 1,
		/// <summary>
		/// Meaning transferring a single file on the DotDFS session from server to client.
		/// </summary>
		SingleFileTransferDownloadFromClient = 2,
		/// <summary>
		/// Meaning moving a directory tree on the DotDFS session from client to server.
		/// </summary>
		DirectoryMovementUploadFromClient = 3,
		/// <summary>
		/// Meaning moving a FileStream request on the DotDFS session from client to server.
		/// </summary>
		FileStreamFromClient = 4,
	}
}