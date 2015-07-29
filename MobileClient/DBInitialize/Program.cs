using Altostratus.Model;
using SQLite;
using System;
using System.Timers;

namespace DBInitialize
{
    class Program
    {
        static void Main(string[] args)
        {
            String message = "Database complete; press any key to exit.";

            // The database will be in the DBInitialize\bin\[Debug | Release] folder
            string path = "Altostratus.db3";            
            SQLiteAsyncConnection conn = new SQLiteAsyncConnection(path);

            Console.WriteLine("Database initializing.");

            // This timer creates progress dots in the console window to show progress.
            Timer ticker = new System.Timers.Timer(500);
            ticker.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                Console.Write('.');
            };

            ticker.AutoReset = true;
            ticker.Enabled = true;

            // Because this is a console utility, it doesn't have UI that needs to be responsive.
            // Therefore we can use .Wait() on async tasks instead of await. The timer above will
            // continue to tick and output dots.

            var dbInit = new DataAccessLayer(conn);
            dbInit.InitAsync().Wait();
                       
            DataModel model = new DataModel(dbInit);
            model.InitAsync().Wait();

            try
            {                               
                model.SyncCategories().Wait();
                model.SyncAuthProviders().Wait();
                model.SyncItems().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception during sync: " + ex.Message);                
                message = "So sorry, there was a problem: " + ex.Message + "/nPress any key to exit.";
            }

            ticker.Enabled = false;            
            Console.WriteLine();
            Console.WriteLine(message);

            //Wait until a key is pressed to exit.
            Console.ReadLine();                   
        }
    }
}

