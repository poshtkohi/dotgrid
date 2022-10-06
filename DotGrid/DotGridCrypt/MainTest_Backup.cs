using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Reflection;

using DotGrid.DotSec;
using DotGrid.DotDfsClient;
using DotGrid.DotThreading;

namespace DotGridCrypt
{
	[Serializable]
	class CryptTest
	{
		private int _id;
		private int _offset;
		private int _length;
		private string _remoteSourceFilename;
		private string _remoteDirectory;
		private string _sourceDotDfsAddress;
		private RijndaelEncryption _rijndael;
		private int BlockSize = 1024 * 1024; // 1 Meg Block Size
		public CryptTest(RijndaelEncryption rijndael, string sourceDotDfsAddress, string remoteSourceFilename, 
			string remoteDirectory, int id, int offset, int length)
		{
			_rijndael = rijndael;
			_sourceDotDfsAddress = sourceDotDfsAddress;
			_remoteSourceFilename = remoteSourceFilename;
			_remoteDirectory = remoteDirectory;
			_id = id;
			_offset = offset;
			_length = length;
		}
		public void Enrypt()
		{
			DotDfsFileStream remoteReader = new DotDfsFileStream(_remoteSourceFilename, FileMode.Open, FileAccess.Read, 
				FileShare.Read, PathEncoding.UTF8, _sourceDotDfsAddress, new NetworkCredential("alireza", "furnaces2002"), false);
			//FileStream remoteReader = new FileStream(_remoteSourceFilename, FileMode.Open, FileAccess.Read);
			string remoteTempPath = String.Format(@"{0}\{1}.temp", _remoteDirectory, _id); 
			DotDfsFileStream remoteWriter = new DotDfsFileStream(remoteTempPath, FileMode.Create, FileAccess.Write, 
				FileShare.None, PathEncoding.UTF8, _sourceDotDfsAddress, new NetworkCredential("alireza", "furnaces2002"), false);
			//FileStream remoteWriter = new FileStream(remoteTempPath, FileMode.Create, FileAccess.Write, FileShare.None);
			remoteReader.Seek(_offset, SeekOrigin.Begin);
			byte[] buffer = new byte[BlockSize];
			int blocks = _length/BlockSize;
			int remindedBlock = _length%BlockSize;
			for(int i = 0 ; i < blocks ; i++)
			{
				int read = remoteReader.Read(buffer, 0, BlockSize);
				byte[] temp = new BlockI(_rijndael, buffer, read).Buffer;
				remoteWriter.Write(temp, 0, temp.Length);
			}
			if(remindedBlock != 0)
			{
				int read = remoteReader.Read(buffer, 0, remindedBlock);
				byte[] temp = new BlockI(_rijndael, buffer, read).Buffer;
				remoteWriter.Write(temp, 0, temp.Length);
			}
			remoteReader.Close();
			remoteWriter.Close();
		}
	}
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		private static void test(string filename, string resultDirectory, string[] nodes, string DistributorIP)
		{
			RijndaelEncryption rijndael = new RijndaelEncryption();
			if(!Directory.Exists(resultDirectory))
				Directory.CreateDirectory(resultDirectory);
			int size = (int)new FileInfo(filename).Length;
			int BlockSize = 10*1024*1024; // 10Meg BlockSize
			CryptTest test = new CryptTest(rijndael, DistributorIP, filename, resultDirectory, 0, 0, size);
			ThreadStart[] starts = new ThreadStart[1];
			starts[0] = new ThreadStart(test.Enrypt);
			Module[] modules = new Module[1];
			modules[0] = typeof(CryptTest).Module;
			ThreadCollectionClient threadC = new ThreadCollectionClient(starts, modules, nodes[0], 
				new NetworkCredential("alireza", "furnaces2002"), false);
			threadC.Start();
			while(true)
			{
				if(!threadC.IsAlive)
					break;
				System.Threading.Thread.Sleep(1);
			}
		}
		//----------------------------------------------------------------------------------
		[STAThread]
		static void Main(string[] args)
		{
			/*RijndaelEncryption rijndael = new RijndaelEncryption();
			string filename = @"c:\\php\php5ts.dll";
			CryptTest test = new CryptTest(rijndael, "127.0.0.1", filename, @"c:\\test", 0, 0, (int)new FileInfo(filename).Length);
			test.Enrypt();*/
			string filename = @"c:\\php\php5ts.dll";
			string resultDirectory = @"c:\\test";
			string[] nodes = new string[] {"localhost"};
			string DistributorIP = "localhost";
			test(filename, resultDirectory, nodes, DistributorIP);
		}
		//----------------------------------------------------------------------------------
	}
}