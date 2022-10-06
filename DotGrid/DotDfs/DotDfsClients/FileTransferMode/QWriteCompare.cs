/*
	All rights reserved to Alireza Poshtkohi (c) 1999-2021.
	Email: arp@poshtkohi.info
	Website: http://www.poshtkohi.info
*/

using System;
using System.Collections;

namespace DotGrid.DotDfs
{
	/// <summary>
	/// Summary description for QWriteCompare.
	/// </summary>
	internal class QWriteCompare : IComparer  
	{
		//**************************************************************************************************************//
		int IComparer.Compare( Object x, Object y )  
		{
			FileTransferModeHearderInfo X = (FileTransferModeHearderInfo)x;
			FileTransferModeHearderInfo Y = (FileTransferModeHearderInfo)y;
			if(X == null && Y != null)
				return -1;
			if(X != null && Y == null)
				return 1;
			if(X == null && Y == null)
				return 0;
			if(X.SeekValue > Y.SeekValue)
				return 1;
			if(X.SeekValue < Y.SeekValue)
				return -1;
			if(X.SeekValue == Y.SeekValue)
				return 0;
			else return 0;
		}
		//**************************************************************************************************************//
	}
}
