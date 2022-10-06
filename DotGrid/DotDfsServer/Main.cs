using System;

using DotGrid.DotDfs;

namespace DotDfs_Server
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
			DotDfsServer server = new DotDfsServer();//
			server.Start();//
		}
		//**************************************************************************************************************//
	}
}