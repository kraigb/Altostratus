using Altostratus.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Altostratus
{
    public class App : Application
    {
        static DataModel dataModel = null;
        DateTime sleepTime = DateTime.Now;      
        HomePage homePage = null;        
              
        public App()
        {
            // At the time of writing, it's always necessary to set MainPage in the app's constructor.
            // Without this, the use of async APIs causes the app's UI to not appear. Fortunately, setting
            // MainPage here and then changing it within OnStart is allowable and, in this app, necessary, 
            // because we need to asynchronously initialize the data model before the UI is created.
            MainPage = new ContentPage();
        }

        public static DataModel DataModel
        {
            get 
            {
                // This check is here for debugging; the dataModel should never be null in normal operation.                
                if (dataModel == null)
                {
                    Debug.WriteLine("App.DataModel is null in property request.");
                    Debugger.Break();
                }
                return dataModel;
            }
        }

        protected override async void OnStart()
        {
            dataModel = new DataModel();
            await dataModel.InitAsync();

            // Replace the earlier empty MainPage with our real one now that the data model is set up.
            homePage = new HomePage();            
            MainPage = new NavigationPage(homePage);
        }

        protected override void OnSleep()
        {
            //Remember when we slept
            sleepTime = DateTime.Now;
        }

        protected override async void OnResume()
        {            
            TimeSpan elapsed = DateTime.Now.Subtract(sleepTime);
            await homePage.Resume(elapsed);
        }
    }
}
