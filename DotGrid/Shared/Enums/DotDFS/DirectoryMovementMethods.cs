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
	public enum DirectoryMovementMethods
	{
		/// <summary>
		/// Meaning closing channel by client in current DotDFS session.
		/// </summary>
		CloseTransferChannel = 0,
		/// <summary>
		/// Meaning downloading a single file in DotDFS session.
		/// </summary>
		DownloadFile = 1,
		/// <summary>
		/// Meaning uploading a single file in DotDFS session.
		/// </summary>
		UploadFile = 2,
		/// <summary>
		/// Meaning creation of all directories and subdirectories as specified by path on the DotDFS server.
		/// </summary>
		CreateDirectory = 3,
		/// <summary>
		/// Meaning deletion a directory and its contents on the DotDFS server.
		/// </summary>
		DeleteDirectory = 4,
		/// <summary>
		/// Meaning deletion a file on the DotDFS server.
		/// </summary>
		DeleteFile = 5,
		/// <summary>
		/// Meaning whether the given path refers to an existing directory on DotDFS server disk.
		/// </summary>
		ExistsDirectory = 6,
		/// <summary>
		/// Meaning whether the given path refers to an existing file on DotDFS server disk.
		/// </summary>
		ExistsFile = 7,
		/// <summary>
		/// Meaning getting the current working directory of the application on DotDFS server.
		/// </summary>
		GetCurrentDirectory = 8,
		/// <summary>
		/// Meaning getting the names of subdirectories in the specified directory on DotDFS server.
		/// </summary>
		GetDirectories = 9,
		/// <summary>
		/// Meaning moving a directory tree on the DotDFS session from client to server.
		/// </summary>
		MoveDirectory  = 10,
		/// <summary>
		/// Meaning moving a file on the DotDFS session from client to server.
		/// </summary>
		MoveFile  = 11,
		/// <summary>
		/// Meaning getting the names of files in the specified directory on DotDFS server.
		/// </summary>
		GetFiles = 12,
		/// <summary>
		/// Meaning getting the size of a specified file on DotDFS server.
		/// </summary>
		GetFileSize = 13,
	}
}