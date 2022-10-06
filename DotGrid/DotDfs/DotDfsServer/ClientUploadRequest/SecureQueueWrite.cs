/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.IO;
using System.Threading;
using System.Collections;

using DotGrid.DotDfs;

namespace DotDfs.FileTransferServer
{
	/// <summary>
	/// Summary description for SecureQueueWrite.
	/// </summary>
	public class SecureQueueWrite
	{
		private FileTransferInfo info;
		private FileStream fs;
		//private Thread worker;
		private ArrayList qwrite;
		private int MaxQueueWorkerSize = 20;
		private IComparer comparer; //
		private long lastOffset = 0;
		private long lastLength = 0;
		private long written;
		private int j = 0;  // seek numbers
		private int k = 0;  // total writes
		private bool closed = false;
		private bool locked = false;
		//**************************************************************************************************************//
		public SecureQueueWrite(FileTransferInfo transferInfo)
		{
			info = transferInfo;
			if(info.ParallelSize == 10)
				MaxQueueWorkerSize = 10;
			if(info.ParallelSize < 10)
				MaxQueueWorkerSize /= 2;
			if(info.FileSize <= info.ParallelSize * MaxQueueWorkerSize * info.BufferSize)
				MaxQueueWorkerSize = 1;
			fs = new FileStream(info.WriteFileName, FileMode.Create, FileAccess.Write, FileShare.None);
			qwrite = new ArrayList();
			comparer = new QWriteCompare();
			//worker = new Thread(new ThreadStart(WorkerProc));
			//worker.Start();
		}
		//**************************************************************************************************************//
		public void WriteToFile(FileTransferModeHearderInfo infoRead)
		{
			if(locked)
			{
				while(locked)
				{
					if(closed)
						break;
					Thread.Sleep(1);
				}
			}
			if(info.ParallelSize == 1 || MaxQueueWorkerSize == 1)
			{
				locked = true;
				if(infoRead == null)
					return;
				k++;
				if(infoRead.SeekValue != lastOffset + lastLength)
				{
					fs.Seek(infoRead.SeekValue, SeekOrigin.Begin);
					fs.Write(infoRead.Data, 0, infoRead.Data.Length);
					j++;
				}
				else fs.Write(infoRead.Data, 0, infoRead.Data.Length);
				lastOffset = infoRead.SeekValue;
				lastLength = infoRead.Data.Length;
				written += infoRead.Data.Length;
				locked = false;
			}
			else
			{
				//Console.WriteLine("hel");
				if(infoRead != null)
				{
					qwrite.Add(infoRead);
					if(qwrite.Count == MaxQueueWorkerSize)
					{
						locked = true;
						//Console.WriteLine(qwrite.Count);
						qwrite.Sort(comparer);
						for(int v = 0 ; v < qwrite.Count ; v++)
						{
							k++;
							FileTransferModeHearderInfo info = (FileTransferModeHearderInfo)qwrite[v];
							if(info.SeekValue != lastOffset + lastLength)
							{
								fs.Seek(info.SeekValue, SeekOrigin.Begin);
								fs.Write(info.Data, 0, info.Data.Length);
								j++;
							}
							else fs.Write(info.Data, 0, info.Data.Length);
							lastOffset = info.SeekValue;
							lastLength = info.Data.Length;
							written += info.Data.Length;
						}
						qwrite.Clear();
						locked = false;
					}
					//else qwrite.Add(infoRead);
				}
				else
				{
					if(qwrite.Count != 0)
					{
						locked = true;
						qwrite.Sort(comparer);
						for(int v = 0 ; v < qwrite.Count ; v++)
						{
							k++;
							FileTransferModeHearderInfo info = (FileTransferModeHearderInfo)qwrite[v];
							if(info.SeekValue != lastOffset + lastLength)
							{
								fs.Seek(info.SeekValue, SeekOrigin.Begin);
								fs.Write(info.Data, 0, info.Data.Length);
								j++;
							}
							else fs.Write(info.Data, 0, info.Data.Length);
							lastOffset = info.SeekValue;
							lastLength = info.Data.Length;
							written += info.Data.Length;
						}
						qwrite.Clear();
						locked = false;
					}
				}
			}
		}
		//**************************************************************************************************************//
		public void Close()
		{
			closed = true;
			/*if(!exited)
				while(!exited)
					Thread.Sleep(1);*/
			try { fs.Close(); } catch {}
			qwrite.Clear();
			qwrite = null;
			//worker = null;
		}
		//**************************************************************************************************************//
	}
}
