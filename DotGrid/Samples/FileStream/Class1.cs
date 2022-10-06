//All rights is reserved to Alireza Poshtkohi (C) 2002-2009.
//Email: alireza.poshtkohi@gmail.com
//Website: http://alireza.iranblog.com

using System;
using System.IO;
using System.Net;

using DotGrid.DotDfs;
using DotGrid.Shared.Enums.DotDFS;

namespace FileStreamTest
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
			string remoteFilename= @"C:\WINDOWS\system32\shell32.dll";
			string localFilename = @"C:\\test\shell32.dll";
			NetworkCredential nc = new NetworkCredential("alireza", "furnaces2002");
			DotDfsFileStream fsRead = new DotDfsFileStream(remoteFilename, FileMode.Open, FileAccess.Read, 
				                      FileShare.ReadWrite, PathEncoding.UTF8, dotDfsServerAddress, nc, false);
			FileStream fsWrite = new FileStream(localFilename, FileMode.Create, FileAccess.Write, FileShare.None);
			byte[] buffer = new byte[64*1024];
			int n = 0;
			while((n = fsRead.Read(buffer, 0, buffer.Length)) > 0)
				fsWrite.Write(buffer, 0, n);
			fsRead.Close();
			fsWrite.Close();
		}
		//**************************************************************************************************************//
	}
}