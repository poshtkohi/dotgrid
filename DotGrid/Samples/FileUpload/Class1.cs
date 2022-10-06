//All rights is reserved to Alireza Poshtkohi (C) 2002-2009.
//Email: alireza.poshtkohi@gmail.com
//Website: http://alireza.iranblog.com

using System;
using System.Net;

using DotGrid.DotDfs;

namespace FileUploadTest
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		//**************************************************************************************************************//
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			string dotDfsServerAddress = "127.0.0.1";
			string localFilename = @"C:\WINDOWS\system32\shell32.dll"; // local file name to upload to remote DotDFS server.
			string remoteFilename = @"C:\\test\shell32.dll";
			int parallelTcpConnections = 10; // number of parallel TCP streams especially for large files
			int tcpBufferSize = 256 * 1024; // recommended 256KB for non-secure mode and 128 for secure mode only on LAN
			NetworkCredential nc = new NetworkCredential("alireza", "furnaces2002");
			FileTransferUpload fs = new FileTransferUpload(localFilename, remoteFilename, 
				          parallelTcpConnections, tcpBufferSize, dotDfsServerAddress, nc, false);
			fs.Run();
			fs.Close();
		}
		//**************************************************************************************************************//
	}
}