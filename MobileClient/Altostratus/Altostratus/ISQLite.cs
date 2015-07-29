using System;
using SQLite;

namespace Altostratus.Model
{
	public interface ISQLite
	{        
		SQLiteConnection GetConnection();
        SQLiteAsyncConnection GetAsyncConnection();
        String GetDatabasePath();
	}    
}
