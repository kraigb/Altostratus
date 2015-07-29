using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Altostratus.DAL;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Configuration;

namespace Altostratus.WebJob
{
    class Program
    {
        static void Main()
        {
            Task task;
            try
            {
                Database.SetInitializer<ApplicationDbContext>(
                   new MigrateDatabaseToLatestVersion<ApplicationDbContext,
                      Altostratus.DAL.Migrations.Configuration>());
                Exception _lastException = null;

                JobHostConfiguration jobConfig = new JobHostConfiguration();
                jobConfig.DashboardConnectionString = GetAppSecret("AzureWebJobsDashboard");
                jobConfig.StorageConnectionString = GetAppSecret("AzureWebJobsStorage");
                var host = new JobHost(jobConfig);

                // Have to wait for each function to complete before the next one starts because they use an EF 
                // context, and the context isn't thread-safe.
                try
                {
                    task = host.CallAsync(typeof(Functions).GetMethod("GetThreadsAsync"), new { providerName = "Twitter" });
                    task.Wait();
                }
                catch (Exception ex)
                {
                    _lastException = ex;
                }
                try
                {
                    task = host.CallAsync(typeof(Functions).GetMethod("GetThreadsAsync"), new { providerName = "StackOverflow" });
                    task.Wait();
                }
                catch (Exception ex)
                {
                    _lastException = ex;
                }

                task = host.CallAsync(typeof(Functions).GetMethod("PurgeOldThreadsAsync"), new { providerName = "" });
                task.Wait();
                if (_lastException != null)
                {
                    throw _lastException;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception: {0}\n{1}", ex.Message, ex.StackTrace);
                throw;
            }
        }

        public static string GetAppSecret(string setting)
        {
            return ConfigurationManager.AppSettings[setting];
        }
    }
}
