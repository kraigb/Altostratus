using Altostratus.Android;
using Altostratus.Model;
using SQLite;
using System;
using System.Diagnostics;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency(typeof (SQLite_Android))]

namespace Altostratus.Android
{
    public class SQLite_Android : ISQLite
    {
        public SQLite_Android()
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
            string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal); // Documents folder
            var path = Path.Combine(documentsPath, sqliteFilename);
            Debug.WriteLine(path);

            if (!File.Exists(path))
            {
                // We have the the prepopulated database in the Resources\Raw folder in the project. This file *must* be set to a Build Action
                // of Android Resource. Upon doing that, the Resource.Designer.cs file will regenerate, which makes the Resource.Raw namespace
                // below plus an identifier for the file (without the extension). If you're copying this code to another project and see
                // an error on "Raw", it means you don't have a folder by that name or you don't have the build action set properly.
                var s = Forms.Context.Resources.OpenRawResource(Resource.Raw.Altostratus);                
                FileStream writeStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);                
                TransferStream(s, writeStream);
            }

            return path;
        }
        #endregion

        void TransferStream(Stream source, Stream destination)
        {
            int Length = 256;
            Byte[] buffer = new Byte[Length];
            int bytesRead = source.Read(buffer, 0, Length);
            // write the required bytes
            while (bytesRead > 0)
            {
                destination.Write(buffer, 0, bytesRead);
                bytesRead = source.Read(buffer, 0, Length);
            }
            source.Close();
            destination.Close();
        }
    }
}