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
		private int BlockSize = 1024 * 1024; // 1 Meg BlockSize
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
			//Console.WriteLine("offset: {0}, length: {1}", _offset, _length);
		}
		public void Enrypt()
		{
			DotDfsFileStream remoteReader = new DotDfsFileStream(_remoteSourceFilename, FileMode.Open, FileAccess.Read, 
				FileShare.Read, PathEncoding.UTF8, _sourceDotDfsAddress, new NetworkCredential("alireza", "furnaces2002"), false);
			//FileStream remoteReader = new FileStream(_remoteSourceFilename, FileMode.Open, FileAccess.Read);
			string remoteTempPath = String.Format(@"{0}\{1}.temp", _remoteDirectory, _id); //
			DotDfsFileStream remoteWriter = new DotDfsFileStream(remoteTempPath, FileMode.Create, FileAccess.Write, //
				FileShare.None, PathEncoding.UTF8, _sourceDotDfsAddress, new NetworkCredential("alireza", "furnaces2002"), false);
			//FileStream remoteWriter = new FileStream(remoteTempPath, FileMode.Create, FileAccess.Write, FileShare.None);
			remoteReader.Seek(_offset, SeekOrigin.Begin);
			byte[] buffer = new byte[BlockSize];
			int blocks = _length/BlockSize;
			int remindedBlock = _length%BlockSize;
			for(int i = 0 ; i < blocks ; i++)
			{
				int read = remoteReader.Read(buffer, 0, BlockSize);
				if(read <= 0)
					break;
				byte[] temp = new BlockI(_rijndael, _id * _length + i, buffer, read).Buffer;
				remoteWriter.Write(temp, 0, temp.Length);
			}
			if(remindedBlock != 0)
			{
				int read = remoteReader.Read(buffer, 0, remindedBlock);
				if(read > 0)
				{
					byte[] temp = new BlockI(_rijndael, _id * _length + blocks, buffer, read).Buffer;
					remoteWriter.Write(temp, 0, temp.Length);
				}
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
		private static double test(string filename, string resultDirectory, string[] nodes, string DistributorIP)
		{
			DateTime t1;
			DateTime t2;
			RijndaelEncryption rijndael = new RijndaelEncryption();
			if(!Directory.Exists(resultDirectory))
				Directory.CreateDirectory(resultDirectory);
			int size = (int)new FileInfo(filename).Length;
			int anyThread = 10*1024*1024; // 10Meg
			if(size <= anyThread)
				anyThread = 1024*1024; // 1Meg
			int threadsNum = size/anyThread;
			int remindedThreadsNum = size%anyThread;
			int n = threadsNum;
			if(threadsNum == 0)
				n++;
			if(remindedThreadsNum != 0)
				n++;
			CryptTest[] tests = new CryptTest[n];//new CryptTest(rijndael, DistributorIP, filename, resultDirectory, 0, 0, size);
			for(int i = 0 ; i < tests.Length ; i++)
			{
				if(remindedThreadsNum != 0 && i == tests.Length - 1)
					tests[i] = new CryptTest(rijndael, DistributorIP, filename, resultDirectory, i, i*anyThread, remindedThreadsNum);
				else
					tests[i] = new CryptTest(rijndael, DistributorIP, filename, resultDirectory, i, i*anyThread, anyThread);
			}
			int threadCollectionNumForAnyMachine = tests.Length/nodes.Length;
			int remindedThreadCollectionNumForAnyMachine = tests.Length%nodes.Length;
			int m = threadCollectionNumForAnyMachine;
			if(remindedThreadCollectionNumForAnyMachine != 0)
				m++;
			ThreadCollectionClient[] tcs = new ThreadCollectionClient[m];
			for(int i = 0 , machine = 0 ; i < tcs.Length ; i+=threadCollectionNumForAnyMachine, machine++)
			{
				if(remindedThreadCollectionNumForAnyMachine != 0 && i == tcs.Length - 1)
				{
					break;
					ThreadStart[] starts = new ThreadStart[remindedThreadCollectionNumForAnyMachine];
					for(int j = 0 ; j < starts.Length ; j++)
						starts[j] = new ThreadStart(tests[i + j].Enrypt);
					Module[] modules = new Module[1];
					modules[0] = typeof(CryptTest).Module;
					tcs[tcs.Length - 1] = new ThreadCollectionClient(starts, modules, nodes[new Random().Next(nodes.Length)], 
						new NetworkCredential("alireza", "furnaces2002"), false);
					/*(ThreadStart[] starts = new ThreadStart[1];
					starts[0] = new ThreadStart(tests[tests.Length - 1].Enrypt);
					Module[] modules = new Module[1];
					modules[0] = typeof(CryptTest).Module;
					tcs[tcs.Length - 1] = new ThreadCollectionClient(starts, modules, nodes[new Random().Next(nodes.Length)], 
						new NetworkCredential("alireza", "furnaces2002"), false);*/
				}
				else
				{
					ThreadStart[] starts = new ThreadStart[threadCollectionNumForAnyMachine];
					for(int j = 0 ; j < starts.Length ; j++)
						starts[j] = new ThreadStart(tests[i + j].Enrypt);
					Module[] modules = new Module[1];
					modules[0] = typeof(CryptTest).Module;
					tcs[i] = new ThreadCollectionClient(starts, modules, nodes[machine], 
						new NetworkCredential("alireza", "furnaces2002"), false);
					Console.WriteLine(i);
				}
			}
			for(int i = 0 ; i < tcs.Length - 1 ; i++)
				tcs[i].Start();
			t1 = DateTime.Now;
			for(int j = 0 ; j < tcs.Length ; j++)
				while(tcs[j].IsAlive)
					System.Threading.Thread.Sleep(1);
			t2 = DateTime.Now;
			/*Console.WriteLine(threadCollectionNumForAnyMachine);
			Console.WriteLine(remindedThreadCollectionNumForAnyMachine);
			//threadC.Start();
			while(threadC.IsAlive)
				System.Threading.Thread.Sleep(1);*/
			//t2 = DateTime.Now;
			/*ThreadStart[] starts = new ThreadStart[1];
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
			}*/
			/*FileStream fsMain = new FileStream(resultDirectory + @"\encrypted.dat", FileMode.Create, FileAccess.Write, FileShare.None);
			FileStream[] fss = new FileStream[tests.Length];
			for(int i = 0 ; i < fss.Length ; i++)
				fss[i] = new FileStream(String.Format(@"{0}\{1}.temp", resultDirectory, i), FileMode.Open, FileAccess.Read, FileShare.None);
			int read = 0;
			byte[] buffer = new byte[10 * 1024*1024]; // 1Meg Buffer
			for(int i = 0 ; i < fss.Length ; i++)
			{
				while((read = fss[i].Read(buffer, 0, buffer.Length)) > 0)
					fsMain.Write(buffer, 0, read);
				fss[i].Close();
				File.Delete(fss[i].Name);
			}
			buffer = null;*/
			return (t2 - t1).TotalMilliseconds;
		}
		//----------------------------------------------------------------------------------
		[STAThread]
		static void Main(string[] args)
		{
			/*RijndaelEncryption rijndael = new RijndaelEncryption();
			string filename = @"c:\\php\php5ts.dll";
			CryptTest test = new CryptTest(rijndael, "127.0.0.1", filename, @"c:\\test", 0, 0, (int)new FileInfo(filename).Length);
			test.Enrypt();*/
			string filename = @"c:\\test.exe";//@"c:\\php\php5ts.dll";//@"C:\Program Files\Macromedia\Flash MX 2004\Flash.exe";//
			string resultDirectory = @"c:\\test";
			string[] nodes = new string[] {"localhost"};
			string DistributorIP = "localhost";
			double elapsed = test(filename, resultDirectory, nodes, DistributorIP);
			Console.WriteLine("Elapsed time is {0}.{1} seconds.\n", (int)(elapsed/1000), (int)(elapsed%1000));
			/*FileStream fsMain = new FileStream( @"C:\Program Files\Macromedia\Flash MX 2004\Flash.exe", FileMode.Open, FileAccess.Read, FileShare.None);
			FileStream fs = new FileStream( @"c:\\test.exe", FileMode.Create, FileAccess.Write, FileShare.None);
			byte[] buffer = new byte[10*1024*1024];
			int read = fsMain.Read(buffer, 0, buffer.Length);
			for(int i = 0 ; i < 10 ; i++)
				fs.Write(buffer, 0, read);
			fsMain.Close();
			fs.Close();*/
		}
		//----------------------------------------------------------------------------------
	}
}