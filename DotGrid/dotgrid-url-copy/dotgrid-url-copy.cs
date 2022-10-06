using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

using DotGrid.DotDfs;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		private static long sum = 0;
		private static double seconds = 0;
		private static long lastWritten;
		private static FileTransferUpload fs;
		private static FileTransferDownload download;
		private static UploadDirectoryClient uploadDirectory;
		private static DownloadDirectoryClient downloadDirectory;
		//private static int BufferSize = 256 * 1024; // 256 for non-secure mode and 128 for secure mode on LAN
		private static	NetworkCredential nc = new NetworkCredential("alireza", "furnaces2002");
		private static TimerCallback timerDelegate = new TimerCallback(ShowSpeedUpload);
		private static Timer timer;
		//**************************************************************************************************************//
		public static void print(string path, DirectoryMovementClient client)
		{
			string[] dirs = client.GetDirectories(path);
			foreach(string dir in dirs)
			{
				Console.WriteLine(dir);
				print(dir, client);
			}
		}
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			/*UploadDirectoryClient client = new UploadDirectoryClient(@"C:\Program Files\ahead", @"d:\\test\ahead", "127.0.0.1", 10, BufferSize, 
				nc, false, false, true);
			client.Run();
			Exception[] es = client.ThrownExceptions;
			if(es != null)
				foreach(Exception ex in es)
					Console.WriteLine(ex.Message);*/
			if(args == null)
			{
				Console.WriteLine(Copyright(false));
				Console.WriteLine("\nError: You must specify input argument(s).");
				return ;
			}
			if(args.Length == 0)
			{
				Console.WriteLine(Copyright(false));
				Console.WriteLine("\nError: You must specify input argument(s).");
				return ;
			}
			string arg = "";
			for(int i = 0 ; i < args.Length ; i++)
				arg += " " + args[i];
			if(arg.Trim() == "")
			{
				Console.WriteLine(Copyright(false));
				Console.WriteLine("\nError: You must specify input argument(s).");
				return ;
			}
			if(arg.ToLower().IndexOf("-cmd") >= 0) // only for Windows platforms.
			{
				string path = Assembly.GetExecutingAssembly().Location;
				//Process.Start("cmd.exe", String.Format("/k \"SET root={0}&&echo %root%\"", path));
				Process.Start("cmd.exe", String.Format("/k \"{0}&&cd /D\"{1}\"\"", Copyright(true), path.Substring(0, path.LastIndexOf(@"\"))));
				return ;
			}
			if(arg.ToLower().IndexOf("-help") >= 0)
			{
				Help();
				return ;
			}
			Run(arg);
		}
		//**************************************************************************************************************//
		private static string Copyright(bool echo)
		{
			if(!echo)
				return "This DotGrid.DotDFS client program transfers files and directory trees between DotDFS clients and servers on Grid infrastructures. All rights reserved to DotGrid team (c) 2006.\n\nUsage:   dotgrid-url-copy -hostname remoteHost -[upload|download] localPath,remotePath -[optional command line switches]\n\nFor more help, please use -help input argument.\n";
			else
				return "title DotGrid dotgrid-url-copy client utility&&echo This DotGrid.DotDFS client program transfers files and directory trees between DotDFS clients and servers on Grid infrastructures. All rights reserved to DotGrid team (c) 2006.&&echo.&&echo Usage:  dotgrid-url-copy -hostname remoteHost -[upload^|download] localPath,remotePath -[optional command line switches]&&echo.&&echo For more help, please use -help input argument.&&echo.";
		}
		//**************************************************************************************************************//
		private static void TransferUpload(int parallel, string localPath, string remotePath, string hostname, /*TransferMode mode,*/ bool secure, bool mem_to_mem, int BufferSize)
		{
			//bool secure = false;
			//TransferMode mode = TransferMode.DotDFS;  // DotDFS   or  GridFTP
			//String dotDfsServerAddress = "localhost";
			//int MaxQueueWorkerSize = 50;
			FileInfo info = null ;
			if(!mem_to_mem)
				info = new FileInfo(localPath);
			if(!mem_to_mem)
				Console.WriteLine("\n\nTransferring  \n\n\tfile:{0} ({1} Bytes)\n\nto \n\tdotdfs:{2}:{3}\n\n\n\tParallel Streams: {4}, Secure(DotSec): {5}\n\n", localPath, info.Length,
					hostname, remotePath/*, mode*/, parallel, secure);
			else
			{
				Console.WriteLine("Memory-to-Memory Test.");
				Console.WriteLine("\nTransferring  \n\n\tfile:{0} ({1} Bytes)\n\nto \n\tdotdfs:{2}:{3}\n\n\n\tParallel Streams: {4}, Secure(DotSec): {5}\n\n", localPath, long.MaxValue,
					hostname, remotePath/*, mode*/, parallel, secure);
			}
			fs = new FileTransferUpload(localPath, remotePath, parallel, BufferSize/*, MaxQueueWorkerSize*/, hostname, nc, secure/*, mode*/);
			/*if(mem_to_mem)
				timer = new Timer(timerDelegate, fs, 5*1000, 1000);
			else
				timer = new Timer(timerDelegate, fs, 0, 1000);*/
			timer = new Timer(timerDelegate, fs, 0, 1000);
			fs.Run();
			/*if(!mem_to_mem)
				while(fs.CurrentTransferredBytes != info.Length)
					Thread.Sleep(1);
			else
				while(fs.CurrentTransferredBytes != long.MaxValue)
					Thread.Sleep(1);*/
			timer.Dispose();
			timer = null;
			//if(parallel == 1)
			Thread.Sleep(1000);
			fs.Close();
			double speed1 = 0;
			double speed2 = 0;
			if(seconds != 0)
				speed1 = (fs.CurrentTransferredBytes/(1024*1024))/seconds ;
			speed2 = (fs.CurrentTransferredBytes/(1024*1024))/(fs.TotalElapsedTime / 1000);
			Console.WriteLine("\n\nFinal Real Speed(MBytes/s): {0} MBytes/s\n", Math.Max(speed1, speed2));
			/*Console.WriteLine("\n\nFinal Average TransferUpload Speed(MBytes/s): {0} MBytes/s", speed1);
			Console.WriteLine("Final Real TransferUpload Speed(MBytes/s): {0} MBytes/s\n", speed2);*/
			lastWritten = 0;
			sum = 0;
			seconds = 0;
			fs = null;
			/*if(seconds > 0)
				Console.WriteLine("\n\nAverage Speed in Globus Format(MBytes/s): {0}.{1} MBytes/s", (sum/seconds)/(1024*1024), (sum/seconds)%(1024 * 1024));
			Console.WriteLine("Real Average Speed(MBytes/s): {0} MBytes/s\n", (info.Length/(1024*1024))/(fs.TotalElapsedTime / 1000));*/
		}
		//**************************************************************************************************************//
		private static void TransferDownload(int parallel, string localPath, string remotePath, string hostname, bool secure, bool mem_to_mem, int BufferSize)
		{
			if(mem_to_mem)
				Console.WriteLine("Memory-to-Memory Test.");
			Console.WriteLine("\n\nTransferring  \n\n\tdotdfs:{0}:{1}\n\nto\n\n\tfile:{2}\n\t\n\n\n\tParallel Streams: {3}, Secure(DotSec): {4}\n\n", hostname, remotePath, 
				localPath, parallel, secure);
			download = new FileTransferDownload(localPath, remotePath, parallel, BufferSize/*, MaxQueueWorkerSize*/, hostname, nc, secure/*, mode*/);
			timer = new Timer(timerDelegate, download, 0, 1000);
			download.Run();
			timer.Dispose();
			timer = null;
			//if(parallel == 1)
			Thread.Sleep(1000);
			download.Close();
			double speed1 = 0;
			double speed2 = 0;
			if(seconds != 0)
				speed1 = (download.CurrentTransferredBytes/(1024*1024))/seconds ;
			speed2 = (download.CurrentTransferredBytes/(1024*1024))/(download.TotalElapsedTime / 1000);
			Console.WriteLine("\n\nFinal Real Speed(MBytes/s): {0} MBytes/s\n", Math.Max(speed1, speed2));
			/*Console.WriteLine("\n\nFinal Average TransferUpload Speed(MBytes/s): {0} MBytes/s", speed1);
			Console.WriteLine("Final Real TransferUpload Speed(MBytes/s): {0} MBytes/s\n", speed2);*/
			lastWritten = 0;
			sum = 0;
			seconds = 0;
			download = null;
		}
		//**************************************************************************************************************//
		private static void Help()
		{
			Console.WriteLine("Attention: For memory-to-memory tests, use /dev/zero for all read paths and /dev/null for all write paths only in Linux and Unix operating systems, testing DotGrid.DotDFS with installed MONO. NET Framework.");
			Console.WriteLine("\nCommands:\n");
			//Console.WriteLine("-quit: exit from this program.");
			Console.WriteLine("-tbs: specifies the TCP Window Size in bytes. Default TCP windows size is 256KB (-tbs bytes number).");
			Console.WriteLine("-secure: with this parameter all data connections will be forced to being secure based on DotSec security layer protocol (Default is non-secure).");
			Console.WriteLine("-hostname: specifies IP or domain name of the destination DotDFS server for transferring files to it.");
			//Console.WriteLine("-mode: specifies the file transfer mode, the mode can be DotDFS or GridFTP.");
			Console.WriteLine("-p: specifies parallel connections (Default is 1).");
			Console.WriteLine("-upload: uploads the specified source file or directory tree to destination DotDFS server.\nUsage:");
			Console.WriteLine("\t-upload localPath,remotePath\n");
			Console.WriteLine("-download: downloads the specified remote source file or directory tree from destination DotDFS server.\nUsage:");
			Console.WriteLine("\t-download localPath,remotePath\n");
			Console.WriteLine("----------------------------------------------------------------");
			Console.WriteLine("Examples:\n");
			//Console.WriteLine("Non-secure file transfer:\n\n\t" + @"-hostname 127.0.0.1 -upload g:\\test\100meg.dat,g:\\test\temp.dat -p 20 -mode dotdfs" + "\n");
			Console.WriteLine("Non-secure file transfer:\n\n\t" + @"-hostname 127.0.0.1 -upload g:\\test\100meg.dat,g:\\test\temp.dat -p 20" + "\n");
			//Console.WriteLine("Secure file transfer:\n\n\t" + @"-hostname 127.0.0.1 -upload g:\\test\100meg.dat,g:\\test\temp.dat -p 20 -mode dotdfs -secure" + "\n");
			Console.WriteLine("Secure file transfer:\n\n\t" + @"-hostname 127.0.0.1 -upload g:\\test\100meg.dat,g:\\test\temp.dat -p 20 -secure" + "\n");
			Console.WriteLine("----------------------------------------------------------------");
		}
		//**************************************************************************************************************//
		private static void Run(string command)
		{
			bool upload = true;
			string comm = command.ToLower();
			if(comm.IndexOf("-download") >= 0)
				upload = false;
			int p1 = -1, p2 = -1;
			if((p1 = comm.IndexOf("-upload ")) >= 0 || (p1 = comm.IndexOf("-download ")) >= 0)
			{
				//********************************************************************
				string hostname = "localhost";
				if((p2 = comm.IndexOf("-hostname ")) >= 0)
				{
					p2 += "-hostname ".Length;
					int p3 = comm.IndexOf("-", p2);
					if(p3 >= 0)
						hostname = command.Substring(p2, p3 -  p2).Trim();
					else hostname = command.Substring(p2).Trim();
					//Console.WriteLine(hostname);
				}
				//********************************************************************
				int tcpBufferSize = 256 * 1024;
				if((p2 = comm.IndexOf("-tbs ")) >= 0)
				{
					p2 += "-tbs ".Length;
					int p3 = comm.IndexOf("-", p2);
					try
					{
						if(p3 >= 0)
							tcpBufferSize = Convert.ToInt32(command.Substring(p2, p3 -  p2).Trim());
						else tcpBufferSize = Convert.ToInt32(command.Substring(p2).Trim());
					}
					catch
					{
						Console.WriteLine("Error: Bad format.(Usage: -tbs bytes number)\n");
						return ;
					}
					if(tcpBufferSize <= 0)
					{
						Console.WriteLine("Error in -tbs parameter: TCP Window size can not be zero or negative.\n");
						return; 
					}
					//Console.WriteLine(hostname);
				}
				//********************************************************************
				/*TransferMode mode = TransferMode.DotDFS;
				string m = null;
				if((p2 = comm.IndexOf("-mode ")) >= 0)
				{
					p2 += "-mode ".Length;
					int p3 = comm.IndexOf("-", p2);
					if(p3 >= 0)
						m = command.Substring(p2, p3 -  p2).Trim().ToLower();
					else m = command.Substring(p2).Trim().ToLower();
					if(m == "gridftp")
						mode =  TransferMode.GridFTP;
					if(m == "dotdfs")
						mode =  TransferMode.DotDFS;
					if(m != "dotdfs" && m != "gridftp")
						mode =  TransferMode.DotDFS;
					//Console.WriteLine(mode);
				}*/
				//********************************************************************
				bool secure = false;
				if((p2 = comm.IndexOf("-secure")) >= 0)
					secure = true;
				//********************************************************************
				p2 = comm.IndexOf(",");
				if(p2 <= 0)
				{
					Console.WriteLine("Error: Bad format.(Usage: -upload readPath,remotePath)\n");
					return ;
				}
				if(upload)
					p1 += "-upload ".Length;
				else 
					p1 += "-download ".Length;
				string localPath = command.Substring(p1, p2 - p1).Trim();
				int pp = comm.IndexOf("-p ", p1);
				string remotePath = null;
				if(pp >= 0 && pp > p2)
					remotePath = command.Substring(p2 + 1, pp - p2 - 1).Trim();
				else
					remotePath = command.Substring(p2 + 1).Trim();
				int parallel = 1;
				string ppp = null;
				if((p1 = comm.IndexOf("-p ")) >= 0)
				{
					p1 += "-p ".Length;
					p2 = comm.IndexOf("-", p1);
					if(p2 >= 0)
						ppp = command.Substring(p1, p2 -  p1).Trim();
					else ppp = command.Substring(p1).Trim();
					//string ppp = command.Substring(p1 + 2, p2 - p1).Trim();
				}
				if(ppp != null)
				{
					try 
					{
						parallel = Convert.ToInt32(ppp);
					}
					catch
					{
						Console.WriteLine("Error: Bad format.(Usage: -p number)\n");
						return ;
					}
				}
				if(parallel <= 0)
				{
					parallel = 1;
					Console.WriteLine("Error: Parallel connections number can not be zero or negative.\n");
					return; 
				}
				//Console.WriteLine("{0}/{1}/{2}", parallel, localPath, remotePath);
				/*try
				{*/
					if(upload) //upload section
					{
						if(localPath.ToLower().Trim().IndexOf("/dev/zero", 0) >= 0 && remotePath.ToLower().Trim().IndexOf("/dev/null", 0) >= 0) // for memroy-to-memory tests
							TransferUpload(parallel, localPath, remotePath, hostname, secure, true, tcpBufferSize);
						if(File.Exists(localPath)) //meaning the local source path is a single file.
							TransferUpload(parallel, localPath, remotePath, hostname, secure, false, tcpBufferSize);
						else //meaning the local source path is a directory tree.
						{
							uploadDirectory = new UploadDirectoryClient(localPath, remotePath, hostname, parallel, tcpBufferSize, nc, secure, false, true);
							timer = new Timer(timerDelegate, uploadDirectory, 0, 1);
							Console.WriteLine("\n\nTransferring  \n\n\tdirectory:{0} \n\nto \n\tdotdfs:{1}:{2}\n\n\n\tParallel Streams: {3}, Secure(DotSec): {4}\n\n", localPath, hostname, remotePath/*, mode*/, parallel, secure);
							uploadDirectory.Run();
							timer.Dispose();
							Console.WriteLine("\n\n\tTotal Elapsed Time: {0} seconds", uploadDirectory.TotalElapsedTime/1000);
							Exception[] es = uploadDirectory.ThrownExceptions;
							if(es != null)
								foreach(Exception ex in es)
									Console.WriteLine(ex.Message);
							Console.WriteLine("");
						}
					}
					else //download section
					{
						if(remotePath.ToLower().Trim().IndexOf("/dev/zero", 0) >= 0 && localPath.ToLower().Trim().IndexOf("/dev/null", 0) >= 0) // for memroy-to-memory tests
							TransferDownload(parallel, localPath, remotePath, hostname, secure, true, tcpBufferSize);
						if(remotePath[remotePath.Length - 1] == '\\' || remotePath[remotePath.Length - 1] == '/') //meaning the remote source path is a directory tree.
						{
							downloadDirectory = new DownloadDirectoryClient(localPath, remotePath, hostname, parallel, tcpBufferSize, nc, secure, false, true);
							timer = new Timer(timerDelegate, downloadDirectory, 0, 1);
							Console.WriteLine("\n\nTransferring  \n\n\tdotdfs:{0}:{1} \n\nto \n\tdirectory:{2}\n\n\n\tParallel Streams: {3}, Secure(DotSec): {4}\n\n", hostname, remotePath, localPath/*, mode*/, parallel, secure);
							downloadDirectory.Run();
							timer.Dispose();
							Console.WriteLine("\n\n\tTotal Elapsed Time: {0} seconds", downloadDirectory.TotalElapsedTime/1000);
							Exception[] es = downloadDirectory.ThrownExceptions;
							if(es != null)
								foreach(Exception ex in es)
									Console.WriteLine(ex.Message);
							Console.WriteLine("");
						}
						else //meaning the local source path is a single file.
							TransferDownload(parallel, localPath, remotePath, hostname, secure, false, tcpBufferSize);
					}
				/*}
				catch(Exception e)
				{
					Console.WriteLine("\n\nExecution Error. Thrown Exception Message: {0}\n", e.Message);
					if(fs != null)
						fs.Close();
					if(download != null)
						download.Close();
					if(uploadDirectory != null)
						uploadDirectory.Close();
					if(downloadDirectory != null)
						downloadDirectory.Close();
					return ;
				}*/
			}
			else
			{
				Console.WriteLine("\nError: Unrecognized command(s). You must use -upload or -download input argument correctly. For help, use -help input argument.\n");
				return ;
			}
		}
		//**************************************************************************************************************//
		private static void ShowSpeedUpload(object f)
		{
			if(f != null)
			{
				if(f.GetType() == typeof(FileTransferUpload))
				{
					FileTransferUpload fs = (FileTransferUpload)f;
					if(fs.CurrentTransferredBytes > 0)
					{
						long written = fs.CurrentTransferredBytes - lastWritten;
						if(written >= 0)
						{
							seconds++;
							sum += written;
							string ave = ((sum/seconds)/(1024 * 1024)).ToString();
							if(ave.Length > 5)
								ave = ave.Substring(0, 5);
							//string ins = String.Format("{0}.{1}", written/(1024 * 1024), written%(1024 * 1024));
							string ins = String.Format("{0}", (double)(written/(1024 * 1024)));
							if(ins.Length > 5)
								ins = ins.Substring(0, 5);
							Console.WriteLine("({0} Bytes)   Speed(MB/s): ave:{1}   ins:{2}", fs.CurrentTransferredBytes, ave, ins);
							lastWritten = fs.CurrentTransferredBytes;
						}
					}
				}
				if(f.GetType() == typeof(FileTransferDownload))
				{
					FileTransferDownload fs = (FileTransferDownload)f;
					if(fs.CurrentTransferredBytes > 0)
					{
						long written = fs.CurrentTransferredBytes - lastWritten;
						if(written >= 0)
						{
							//Console.Write("\r\t({0} Bytes) Current Speed(MBytes/s): {1}.{2}\r", fs.CurrentTransferredBytes, written/(1024 * 1024), written%(1024 * 1024));
							seconds++;
							sum += written;
							string ave = ((sum/seconds)/(1024 * 1024)).ToString();
							if(ave.Length > 5)
								ave = ave.Substring(0, 5);
							//string ins = String.Format("{0}.{1}", written/(1024 * 1024), written%(1024 * 1024));
							string ins = String.Format("{0}", (double)(written/(1024 * 1024)));
							if(ins.Length > 5)
								ins = ins.Substring(0, 5);
							Console.WriteLine("{0} Speed(MB/s): ave:{1}   ins:{2}", fs.CurrentTransferredBytes, ave, ins);
							lastWritten = fs.CurrentTransferredBytes;
						}
					}
				}
				if(f.GetType() == typeof(UploadDirectoryClient))
				{
					UploadDirectoryClient uploadDirectory = (UploadDirectoryClient)f;
					Console.Write("\r\tCompleted Percentage: {0}%", uploadDirectory.CompletedPercentage);
				}
				if(f.GetType() == typeof(DownloadDirectoryClient))
				{
					DownloadDirectoryClient downloadDirectory = (DownloadDirectoryClient)f;
					Console.Write("\r\tCompleted Percentage: {0}%", downloadDirectory.CompletedPercentage);
				}
			}
		}
		//**************************************************************************************************************//
	}
}