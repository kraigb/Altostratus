using Altostratus.iOS;
using Altostratus.Model;
using SQLite;
using System;
using System.Diagnostics;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency (typeof (SQLite_iOS))]

namespace Altostratus.iOS
{
	public class SQLite_iOS : ISQLite
	{
		public SQLite_iOS ()
		{
		}

		#region ISQLite implementation
        public SQLiteConnection GetConnection()
        {
            // Not used in the app, but here for reference. This would be a place to do
            // any other platform-specific work for making a connection, as is sometimes needed
            // with other implementations of SQLite.
            return new SQLiteConnection(GetDatabasePath());
        }

        public SQLiteAsyncConnection GetAsyncConnection()
        {
            // Not used in the app, but here for reference. This would be a place to do
            // any other platform-specific work for making a connection, as is sometimes needed
            // with other implementations of SQLite.
            return new SQLiteAsyncConnection(GetDatabasePath());
        }

        public String GetDatabasePath()
        {
            var sqliteFilename = "Altostratus.db3";

            //Assemble the path to the the app's local storage, where we'll host the active database
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // Documents folder
            string libraryPath = Path.Combine(documentsPath, "..", "Library"); // Library folder
            var path = Path.Combine(libraryPath, sqliteFilename);
            Debug.WriteLine(path);

            if (!File.Exists(path))
            {
                // Copy the prepopulated database. On iOS, the default folder is the app's Resources, and we
                // have the file stored in a raw folder within that, hence the simple relative path. The Build Action
                // for this file is set to BundleResource.
                File.Copy("raw/" + sqliteFilename, path);
            }

            return path;
        }
		#endregion
	}
}
