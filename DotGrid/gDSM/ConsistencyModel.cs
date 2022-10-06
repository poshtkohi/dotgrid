using System;

namespace DotGrid.gDSM
{
	/// <summary>
	/// An interface for gDsm Consistency Model.
	/// </summary>
	public interface ConsistencyModel
	{
		/// <summary>
		/// Reads an object of gDsm stream.
		/// </summary>
		/// <returns>The read object. If null is returned, there might be an error.</returns>
		object Read();


		/// <summary>
		/// Writes wr object to gDsm stream.
		/// </summary>
		/// <param name="wr">The object to write.</param>
		/// <returns></returns>
		bool Write(object wr);


		/// <summary>
		/// Locks the the object on gDsm based on the used Consistency Model..
		/// </summary>
		/// <returns>True if the lock operation has been succeed.</returns>
		bool Lock();


		/// <summary>
		/// Unlocks the the object on gDsm based on the used Consistency Model.
		/// </summary>
		/// <returns>True if the Unlock operation has been succeed.</returns>
		bool Unlock();

		/// <summary>
		/// Gets unique ID of the shared object on gDsm.
		/// </summary>
		string Guid
		{
			get;
		}


		/// <summary>
		/// Gets a value indicating whether the current shared object on gDsm supports writing.
		/// </summary>
		bool Writable
		{
			get;
		}


		/// <summary>
		/// Gets a value indicating whether the current shared object on gDsm supports reading.
		/// </summary>
		bool Readable
		{
			get;
		}

		/// <summary>
		/// Gets a value indicating whether the current shared object on gDsm locked.
		/// </summary>
		bool Locked
		{
			get;
		}

	}
}
