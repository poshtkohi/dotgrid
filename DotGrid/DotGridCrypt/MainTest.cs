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
		//public double delay = 0;
		private string _remoteSourceFilename;
		private string _remoteDirectory;
		private string _sourceDotDfsAddress;
		private object _enc = null;
		private Enc _encryptionName;
		private string _collectorAddress;
		private NetworkCredential _nc;
		private int BlockSize = 1024 * 1024; // 1 Meg BlockSize
		public CryptTest(object encryption, Enc encryptionName, string sourceDotDfsAddress, string collectorAddress, string remoteSourceFilename, 
			string remoteDirectory, int id, int offset, int length, NetworkCredential nc)
		{
			_enc = encryption;
			_encryptionName = encryptionName;
			_sourceDotDfsAddress = sourceDotDfsAddress;
			_remoteSourceFilename = remoteSourceFilename;
			_remoteDirectory = remoteDirectory;
			_collectorAddress = collectorAddress;
			_id = id;
			_offset = offset;
			_length = length;
			_nc = nc;
			//Console.WriteLine("offset: {0}, length: {1}", _offset, _length);
		}
		public void Enrypt()
		{
			//DateTime t1 = DateTime.Now;
			DotDfsFileStream remoteReader = new DotDfsFileStream(_remoteSourceFilename, FileMode.Open, FileAccess.Read, 
				FileShare.Read, PathEncoding.UTF8, _sourceDotDfsAddress, _nc, false);
			//FileStream remoteReader = new FileStream(_remoteSourceFilename, FileMode.Open, FileAccess.Read);
			string remoteTempPath = String.Format(@"{0}\{1}.temp", _remoteDirectory, _id); //
			DotDfsFileStream remoteWriter = new DotDfsFileStream(remoteTempPath, FileMode.Create, FileAccess.Write, //
				FileShare.None, PathEncoding.UTF8, _collectorAddress, _nc, false);
			//FileStream remoteWriter = new FileStream(remoteTempPath, FileMode.Create, FileAccess.Write, FileShare.None);
			remoteReader.Seek(_offset, SeekOrigin.Begin);
			//DateTime t2 = DateTime.Now;
			//delay = (t2 - t1).TotalMilliseconds;
			byte[] buffer = new byte[BlockSize];
			int blocks = _length/BlockSize;
			int remindedBlock = _length%BlockSize;
			for(int i = 0 ; i < blocks ; i++)
			{
				int read = remoteReader.Read(buffer, 0, BlockSize);
				if(read <= 0)
					break;
				byte[] temp = new BlockI(_enc, _encryptionName, _id * _length + i, buffer, read).Buffer;
				remoteWriter.Write(temp, 0, temp.Length);
			}
			if(remindedBlock != 0)
			{
				int read = remoteReader.Read(buffer, 0, remindedBlock);
				if(read > 0)
				{
					byte[] temp = new BlockI(_enc, _encryptionName, _id * _length + blocks, buffer, read).Buffer;
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
		private static double test(string filename, Enc encryptionName, string resultDirectory, string[] nodes, string DistributorIP, string CollectorIP, NetworkCredential nc)
		{
			DateTime t1;
			DateTime t2;
			object enc = null;
			switch(encryptionName)
			{
				case Enc.Rijndael:
					enc  = new RijndaelEncryption();
					break;
				case Enc.TripleDes:
					enc =  new TripleDESEncryption();
					break;
				case Enc.RC2:
					enc = new RC2Encryption();
					break;
				default:
					throw new ArgumentException("encryptionName parameter only can be rijndael, 3des and rsa");
			}
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
			CryptTest[] tests = new CryptTest[n];
			for(int i = 0 ; i < tests.Length ; i++)
			{
				if(remindedThreadsNum != 0 && i == tests.Length - 1)
					tests[i] = new CryptTest(enc, encryptionName, DistributorIP, CollectorIP, filename, resultDirectory, i, i*anyThread, remindedThreadsNum, nc);
				else
					tests[i] = new CryptTest(enc, encryptionName, DistributorIP, CollectorIP, filename, resultDirectory, i, i*anyThread, anyThread, nc);
			}
			int a = tests.Length/nodes.Length;
			int b = tests.Length%nodes.Length;
			//Console.WriteLine("threrads num: {0}", tests.Length);
			//Console.WriteLine("a:{0}, b:{1}", a, b);
			int m = nodes.Length;
			if(b != 0)
				m++;
			ThreadCollectionClient[] tcs= new ThreadCollectionClient[m];
			//Console.WriteLine("tcs:{0}", tcs.Length);
			for(int i = 0 ; i < tcs.Length ; i++)
			{
				if(b != 0 && i == tcs.Length - 1)
				{
					ThreadStart[] starts = new ThreadStart[b];
					for(int j = 0 ; j < b ; j++)
						starts[j] = new ThreadStart(tests[i*a + j].Enrypt);
					Module[] modules = new Module[1];
					modules[0] = typeof(CryptTest).Module;
					tcs[i] = new ThreadCollectionClient(starts, modules, nodes[new Random().Next(nodes.Length)], nc, false);
				}
				else
				{
					ThreadStart[] starts = new ThreadStart[a];
					for(int j = 0 ; j < a ; j++)
						//Console.WriteLine(i*a + j);
						starts[j] = new ThreadStart(tests[i*a + j].Enrypt);
					Module[] modules = new Module[1];
					modules[0] = typeof(CryptTest).Module;
					tcs[i] = new ThreadCollectionClient(starts, modules, nodes[i], nc, false);
				}
			}
			for(int i = 0 ; i < tcs.Length ; i++)
				tcs[i].Start();
			t1 = DateTime.Now;
			for(int j = 0 ; j < tcs.Length ; j++)
				while(tcs[j].IsAlive)
					System.Threading.Thread.Sleep(1);
			//double delay = 0;
			/*for(int i = 0 ; i < tcs.Length ; i++)
				for(int j = 0 ; j < tcs[i].ReturnedObjects.Length ; j++)
					delay += ((CryptTest)tcs[i].ReturnedObjects[j]).delay;
			Console.WriteLine("delay time is {0}.{1} seconds.\n", (int)(delay/1000), (int)(delay%1000));*/
			t2 = DateTime.Now;
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
			Enc encryptonName = Enc.TripleDes;
			string filename = @"c:\\test\50meg.dat"; // 50, 100, 200, 300, 400, 500, 600, 700, 800, 900,1000
			string resultDirectory = @"c:\\test";
			string[] nodes = new string[] {"169.254.149.164"};
			string DistributorIP = "169.254.149.165";
			string CollectorIP = "169.254.149.163";
			int repeats = 1;
			double elapsed = 0;
			NetworkCredential nc = new NetworkCredential("alireza", "furnaces2002");
			for(int i = 0 ; i < repeats ; i++)
				elapsed += test(filename, encryptonName, resultDirectory, nodes, DistributorIP, CollectorIP, nc);
			elapsed /= repeats;
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