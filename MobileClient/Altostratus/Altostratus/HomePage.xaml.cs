using Altostratus.Model;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Altostratus
{
    public partial class HomePage : ContentPage
    {
        HomeViewModel viewModel;        

        public HomePage()
        {
            InitializeComponent();
            viewModel = (HomeViewModel)(this.BindingContext);
            viewModel.Navigation = this.Navigation;
        }

        public async Task Resume(TimeSpan elapsed)
        {
            await viewModel.CheckTimeAndSync(elapsed, false);
        }

        protected override async void OnAppearing()
        {
            Debug.WriteLine("Home.OnAppearing");
            base.OnAppearing();

            // Whenever we navigate to this page, we check if the configuration has changed in the
            // meantime, which happens specifically if we navigate to the configuration page and return
            // here after making changes. This is handled in the viewmodel, so all we need to do is            
            // to check for a refresh.
            await viewModel.CheckRefresh();                       
        }

        protected override void OnDisappearing()
        {
            // This is here just for debug output, which was necessary to reveal a bug in Xamarin.Forms
            // with navigation events.
            Debug.WriteLine("Home.OnDisappearing");            
            base.OnDisappearing();
        }

        async void OnItemTapped(Object sender, ItemTappedEventArgs e)
        {
            // Get the full item from the database and send it to the Item page. 
            var item = (Model.Item)(e.Item);
            Item dbItem = await DataAccessLayer.Current.GetItemAsync(item.Url);                

            // If we got a null from the database, then the background cleanup might have
            // removed it. Signal this by setting a null body, which the item page checks.
            if (dbItem == null)
            {                
                dbItem = item;
                dbItem.Body = null;
            }

            await this.Navigation.PushAsync(new ItemPage(dbItem));
        }
    }
}
