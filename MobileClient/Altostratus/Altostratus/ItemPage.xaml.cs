using Altostratus.Model;
using System;
using Xamarin.Forms;

namespace Altostratus
{
    public partial class ItemPage : ContentPage
    {
        // The OnNavigating event handler later on forces links in the webview to open in a browser to
        // avoid navigating inside the webview. On iOS and Windows Phone 8.1, this event is also raised
        // when initializing the webview with local content, in which case we want to ignore the first
        // navigating event by setting this flag to false initially.
        private Boolean navigateToBrowser = Device.OnPlatform<Boolean>(false, true, false);

        public ItemPage(Item selectedItem)
        {
            InitializeComponent();        

            // We set the BindingContext in code because it's parameterized with the tapped item.            
            // Note: it's possible, of course, to skip the view model and just set the binding context
            // to the item. However, we're leaving the viewmodel here to allow for later extension.             
            ItemViewModel viewModel = new ItemViewModel(selectedItem);
            this.BindingContext = viewModel;

            // Create and initialize the webview that displays the HTML body. Note that the 
            // selectedItem object will not have the body; we have to use the item in the viewModel
            // because that's what contains the full data.
            WebView wv = new WebView();

            // Force links in the webview to open in a browser; alternately, we could get the back
            // button command and navigate back in the webview, but this seems simpler and avoids
            // complications with rendering random web pages in the webview.
            wv.Navigating += (Object sender, WebNavigatingEventArgs e) =>
                {
                    if (navigateToBrowser)
                    {
                        Device.OpenUri(new System.Uri(e.Url));
                        e.Cancel = true;
                    }

                    navigateToBrowser = true;
                };

            HtmlWebViewSource source = new HtmlWebViewSource();            
            source.Html = viewModel.Body;
            wv.Source = source;
            
            this.WebviewFrame.Content = wv;

            // Make the provider label tappable to open the original post
            this.ProviderLabel.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command (() =>
                {
                    Device.OpenUri(new System.Uri(viewModel.Url));
                })
            });
        }
    }
}
