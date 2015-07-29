using Altostratus.Model;
using Altostratus.WinPhone;
using System;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel;
using Windows.Storage;
using Xamarin.Forms;
using SQLite;

[assembly: Dependency (typeof (SQLite_WinPhone))]

namespace Altostratus.WinPhone
{
	public class SQLite_WinPhone : ISQLite
	{
		public SQLite_WinPhone ()
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
            var localFolder = ApplicationData.Current.LocalFolder;
            string path = Path.Combine(localFolder.Path, sqliteFilename);
            Debug.WriteLine(path);

            if (!File.Exists(path))
            {
                // The prepopulated database is in the package's resources\raw folder. The Build Action on
                // Windows Phone must be set to Content.
                StorageFolder packageFolder = Package.Current.InstalledLocation;
                string source = Path.Combine(packageFolder.Path, @"resources\raw\" + sqliteFilename);

                var fileExists = File.Exists(source).ToString();
                Debug.WriteLine("Source file exists = " + fileExists + "; " + source);

                // This is a synchronous API, which we're using so we don't have to make an async constructor
                // for the database class. This work happens when we launch the app, so a little extra wait on the
                // splash screen is no problem.
                try
                {
                    File.Copy(source, path);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception on File.Copy, message = " + ex.Message);
                }
                
            }
            
            return path;
        }		
       #endregion
	}
}
