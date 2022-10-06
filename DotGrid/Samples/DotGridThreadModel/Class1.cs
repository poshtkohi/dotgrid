//All rights is reserved to Alireza Poshtkohi (C) 2002-2009.
//Email: alireza.poshtkohi@gmail.com
//Website: http://alireza.iranblog.com

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Reflection;

using DotGrid.DotSec;
using DotGrid.DotDfs;
using DotGrid.DotThreading;

namespace DotGridThreadModel
{
	//---------------------------------------------------------
	[Serializable]
	public class Test
	{
		private int x, y ,z;
		public Test(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
		public void AddProc()
		{
			this.z = this.x + this.y;
		}
		public int Z
		{
			get
			{
				return this.z;
			}
		}
	}
	//---------------------------------------------------------
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		//---------------------------------------------------------
		[STAThread]
		static void Main(string[] args)
		{
			string[] nodes = new string[] {"localhost", "localhost"};
			NetworkCredential nc = new NetworkCredential("alireza", "furnaces2002");
			ThreadCollectionClient[] tcs= new ThreadCollectionClient[nodes.Length];


			Test test = new Test(10, 20);

			for(int i = 0 ; i < tcs.Length ; i++)
			{
				ThreadStart[] starts = new ThreadStart[1];

				for(int j = 0 ; j < starts.Length ; j++)
					starts[j] = new ThreadStart(test.AddProc); // the entrypoint method for remote thread invocation

				Module[] modules = new Module[1];
				modules[0] = typeof(Test).Module;
				tcs[i] = new ThreadCollectionClient(starts, modules, nodes[i], nc, false);
			}

			//starts all remote threads
			for(int i = 0 ; i < tcs.Length ; i++)
				tcs[i].Start();

			for(int j = 0 ; j < tcs.Length ; j++)
				while(tcs[j].IsAlive)
				{
					//Console.WriteLine("im alive still");
					System.Threading.Thread.Sleep(1);
				}



			//gets the computed threads and retrieves the retuened remote objects
			for(int j = 0 ; j < tcs.Length ; j++)
			{
				object[] temp = tcs[j].ReturnedObjects;

				for(int i = 0 ; i < temp.Length ; i++)
				{
					Test _t = (Test)temp[i];
					Console.WriteLine("Z for node of {0} is: {1}", nodes[j], _t.Z);
				}
			}


		}
		//---------------------------------------------------------
	}
}
