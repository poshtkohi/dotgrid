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

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for QueueRead.
	/// </summary>
	internal class QueueRead
	{
		private FileStream fs;
		//private Queue queue;
		//private Thread worker;
		private int tcpBufferSize;
		//private int MaxQueueWorkerSize = 20;
		private bool closed = false;
		private bool workerExited = false;
		private int parallel = 1;
		private bool locked = false;
		private bool memmoryToMemoryTests;
		private int n;
		//private BufferedStream stream;
		//**************************************************************************************************************//
		public QueueRead(ref FileStream fs, int tcpBufferSize, int parallel/*, int MaxQueueWorkerSize*/, bool memmoryToMemoryTests)
		{
			/*if(MaxQueueWorkerSize > 0)
				this.MaxQueueWorkerSize = MaxQueueWorkerSize;*/
			this.tcpBufferSize = tcpBufferSize;
			this.parallel = parallel;
			this.fs = fs;
			//this.stream = new BufferedStream(fs, tcpBufferSize);
			if(this.parallel <= 0)
				this.parallel = 1;
			this.memmoryToMemoryTests = memmoryToMemoryTests;
			/*if(!memmoryToMemoryTests)
			{
				queue = new Queue();
				worker = new Thread(new ThreadStart(ThreadProc));
				worker.Start();
			}*/
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Manages execution of the worker thread.
		/// </summary>
		private void ThreadProc()
		{
			//int j = 0;
			/*while(true)
			{
				if(closed)
					break;
				if(queue.Count == 0)
				{
					if(closed)
						goto End;
					byte[] buffer = new byte[MaxQueueWorkerSize* tcpBufferSize];
					int n = fs.Read(buffer, 0, buffer.Length);
					if(n <= 0)
						goto End;
					//j++;
					//Console.WriteLine("buffer size: {0}", buffer.Length/(1024*1024));
					//Console.WriteLine("j={0}", j);
					long position = fs.Position - n;
					//Console.WriteLine(position);
					ReadInfo info = null;
					if(n < buffer.Length)
					{
						int a = n / tcpBufferSize;
						int b = n % tcpBufferSize;
						//Console.WriteLine("a={0}, b={1}", a, b);
						if(a == 0)
						{
							byte[] temp = new byte[n];
							Array.Copy(buffer, 0, temp, 0, temp.Length);
							info = new ReadInfo(temp, position);
							position += temp.Length;
							queue.Enqueue(info);
							//Console.WriteLine("aa== 0");
						}
						if(a != 0)
						{
							for(int i = 0 ; i < a ; i++)
							{
								byte[] temp = new byte[tcpBufferSize];
								Array.Copy(buffer, i*tcpBufferSize, temp, 0, temp.Length);
								info = new ReadInfo(temp, position/);
								position +=temp.Length;
								queue.Enqueue(info);
								//Console.WriteLine("aa");
							}
						}
						if(b != 0)
						{
							byte[] temp = new byte[b];
							Array.Copy(buffer, a*tcpBufferSize, temp, 0, temp.Length);
							info = new ReadInfo(temp, position);
							position += temp.Length;
							queue.Enqueue(info);
							//Console.WriteLine("bb");
						}
						//break;
					}
					else
					{
						for(int i = 0 ; i < MaxQueueWorkerSize ; i++)
						{
							byte[] temp = new byte[tcpBufferSize];
							Array.Copy(buffer, i*tcpBufferSize, temp, 0, temp.Length);
							info = new ReadInfo(temp, position);
							position +=temp.Length;
							queue.Enqueue(info);
						}
					}
				}
				else Thread.Sleep(1);
			}*/
			/*while(true)
			{
				if(closed)
					break;
				if(queue.Count == 0)
				{
					for(int i = 0 ; i < MaxQueueWorkerSize ; i++)
					{
						if(closed)
							goto End;
						//Console.WriteLine(i);
						byte[] buffer = new byte[tcpBufferSize];
						int n = fs.Read(buffer, 0, buffer.Length);
						if(n <= 0)
							goto End;
						ReadInfo info = null;
						if(n < buffer.Length)
						{
							byte[] temp = new byte[n];
							Array.Copy(buffer, 0, temp, 0, n);
							info = new ReadInfo(ref temp, fs.Position - n);
						}
						else info = new ReadInfo(ref buffer, fs.Position - n);
						queue.Enqueue(info);
					}
				}
				else Thread.Sleep(1);
			}
		End:
			workerExited = true;*/
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Read a file block from the queue read.
		/// </summary>
		/// <returns></returns>
		public void Read(ref byte[] buffer, ref long offsetSeek)
		{
			/*byte[] buffer = new byte[tcpBufferSize];
			offset += buffer.Length;
			//Thread.Sleep(1);
			return new ReadInfo(buffer, offset - buffer.Length);*/


			/*if(memmoryToMemoryTests)
			{*/
			if(parallel > 1)
			{
				if(locked)
				{
					while(true)
					{
						if(workerExited || closed)
						{
							offsetSeek = -1;
							return ;
						}
						if(!locked)
							break;
						Thread.Sleep(1);
					}
				}
				locked = true;
			}
			n = fs.Read(buffer, 0, buffer.Length);
			if(n <= 0)
			{
				offsetSeek = -1;
				return ;
			}
			if(n < buffer.Length)
			{
				byte[] temp = new byte[n];
				Array.Copy(buffer, 0, temp, 0, n);
				buffer = temp;
				temp = null;
				locked = false;
			    offsetSeek = fs.Position - n;
				return ;
			}
			else 
			{
				locked = false;
				offsetSeek = fs.Position - n;
				return ;
			}
			/*}


			if(parallel != 1)
			{
				if(locked)
				{
					while(true)
					{
						if(workerExited || closed)
							return null;
						if(!locked)
						{
							locked = true;
							break;
						}
						Thread.Sleep(1);
					}
				}
			}
			if(queue.Count == 0) 
			{
				while(true)
				{
					if(workerExited || closed)
						return null;
					if(queue.Count != 0)
					{
						ReadInfo read = (ReadInfo)queue.Dequeue();
						locked = false;
						return read;
					}
					Thread.Sleep(1);
				}
			}
			else
			{
				ReadInfo read = (ReadInfo)queue.Dequeue();
				locked = false;
				return read;
			}
			//Console.WriteLine("read");
			/*byte[] buffer = new byte[tcpBufferSize];
			int n = stream.Read(buffer, 0, buffer.Length);
			if(n <= 0)
				return null;
			if(n < buffer.Length)
			{
				byte[] temp = new byte[n];
				Array.Copy(buffer, 0, temp, 0, n);
				return new ReadInfo(temp, fs.Position - n);
			}
			else return new ReadInfo(buffer, fs.Position - n);*/
			/*byte[] buffer = new byte[tcpBufferSize];
			int n = fs.Read(buffer, 0, buffer.Length);
			if(n <= 0)
				return null;
			if(n < buffer.Length)
			{
				byte[] temp = new byte[n];
				Array.Copy(buffer, 0, temp, 0, n);
				return new ReadInfo(temp, fs.Position - n);
			}
			else return new ReadInfo(buffer, fs.Position - n);
			/*if(parallel == 1)
			{
				byte[] buffer = new byte[tcpBufferSize];
				int n = fs.Read(buffer, 0, buffer.Length);
				if(n <= 0)
					return null;
				if(n < buffer.Length)
				{
					byte[] temp = new byte[n];
					Array.Copy(buffer, 0, temp, 0, temp.Length);
					return new ReadInfo(temp, fs.Position - n);
				}
				else return new ReadInfo(buffer, fs.Position - n);
			}
			else
			{*/
			/*if(workerExited || closed)
					return null;*/
			/*if(queue.Count == 0) 
				{
					while(true)
					{
						if(workerExited || closed)
							return null;
						if(queue.Count != 0)
							return (ReadInfo)queue.Dequeue();
						Thread.Sleep(1);
					}
				}
				else return (ReadInfo)queue.Dequeue();*/
			/*}*/
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Gets a long value representing the length of the stream in bytes.
		/// </summary>
		public long Length
		{
			get
			{
				if(memmoryToMemoryTests)
					return long.MaxValue;
				else 
					return fs.Length;
			}
		}
		//**************************************************************************************************************//
		/// <summary>
		/// Closes this instance.
		/// </summary>
		public void Close()
		{
			closed = true;
			/*if(parallel > 1)
			{
				while(!workerExited)
					Thread.Sleep(1);
			}*/
			/*if(queue != null)
				queue.Clear();
			worker = null;*/
			if(fs != null)
				fs.Close();
			//buffer = null;
			GC.Collect();
		}
		//**************************************************************************************************************//
	}
}
