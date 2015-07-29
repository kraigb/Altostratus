using Altostratus.Model;
using System;
using System.Diagnostics;
using Xamarin.Forms;

namespace Altostratus
{
    public partial class ConfigurationPage : ContentPage
    {
        ConfigurationViewModel viewModel;
        UserPreferences enterState = null;

        public ConfigurationPage()
        {
            InitializeComponent();
            viewModel = (ConfigurationViewModel)(this.BindingContext);

            CreateCategorySwitches();
                        
            // We have to set Maximum and Minimum in code, in this specific order, because Xamarin.Forms will
            // crash if we attempt to set Minimum first when Maximum has a default of zero. This is why we aren't
            // using data binding for these values.
            limitSlider.Maximum = UserPreferences.ConversationMax;
            limitSlider.Minimum = UserPreferences.ConversationMin;            
            limitSlider.BindingContext = viewModel.CurrentUser;
            limitSlider.SetBinding(Slider.ValueProperty, "ConversationLimit", BindingMode.TwoWay);
            
            InitializePicker();
        }

        private void CreateCategorySwitches()
        {
            StackLayout sl;
            Switch sw;
            Label lb;

            // Build the children of the ScrollView.StackPanel from the Categories list, XAML is as follows
            // except that we want to set the binding for each switch to an individual category:            
            //    <StackLayout Padding="20, 15, 10, 5" Orientation ="Horizontal">              
            //      <Switch IsToggled="{ Binding IsActive, Mode=TwoWay }" />
            //      <Label Text="{ Binding Name }" FontSize="Large" YAlign="Center" />
            //    </StackLayout>
            foreach (Category c in viewModel.Categories)
            {
                sl = new StackLayout();
                sl.Padding = new Thickness(20, 15, 10, 5);
                sl.Orientation = StackOrientation.Horizontal;

                sw = new Switch();
                sw.BindingContext = c;
                sw.SetBinding(Switch.IsToggledProperty, "IsActive", BindingMode.TwoWay);
                sl.Children.Add(sw);

                // NOTE: There seems to be a bug on Android, filed as https://bugzilla.xamarin.com/show_bug.cgi?id=27798.
                // When going back to the home page from here, the Home.OnAppearing event is raised before Configuration.OnDisappearing,
                // and thus a call to viewModel.CheckChanges there happens too late for Home.OnAppearing to make use of it.
                // The workaround is to watch UI interaction and keep updating our change state which is inefficient, but works.
                sw.Toggled += SwitchToggled;

                lb = new Label();
                lb.BindingContext = c;
                lb.FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label));
                lb.YAlign = TextAlignment.Center;

                lb.SetBinding(Label.TextProperty, "Name", BindingMode.OneWay);
                sl.Children.Add(lb);

                optionsStack.Children.Add(sl);
            }
        }

        private void InitializePicker()
        {            
            foreach (AuthProvider p in viewModel.Providers)
            {
                providerPicker.Items.Add(p.Name);
            }
            
            providerPicker.SelectedIndex = 0;
        }

        // To avoid Android navigation timing bugs (https://bugzilla.xamarin.com/show_bug.cgi?id=27798) we check for
        // changes every time something is  affected on the page, rather than just checking in OnDisappearing. In this
        // case we don't update the backend (the false argument).
        async void SwitchToggled(object sender, ToggledEventArgs e)
        {
            await viewModel.CheckChanges(enterState, ChangeCheckType.ConversationLimit, false);
        }

        // Watch the slider to update the number in the header. CurrentUser.ConversationLimit is
        // updated automatically through two-way binding, but due to the Android nav bug we also
        // need to make sure the view model has its change flag set.
        async void SliderChanged(object sender, ValueChangedEventArgs e)
        {
            await viewModel.CheckChanges(enterState, ChangeCheckType.CategorySelection, false);
        }


        async void LoginTapped(object sender, EventArgs e)
        {
            // Debug check: we shouldn't get here on Windows Phone with the login controls not visible.            
            Device.OnPlatform(
                WinPhone: () =>
                {
                    Debug.WriteLine("Should never get here on Windows Phone: login controls should have been disabled.");
                    Debugger.Break();
                    return;
                }
            );

            if (viewModel.IsAuthenticated)
            {
                await viewModel.Logout();
            }
            else
            {
                LoginPage loginPage = new LoginPage(viewModel.Providers[providerPicker.SelectedIndex]);
                await this.Navigation.PushAsync(loginPage);
            }            
        }

        protected override void OnAppearing()
        {            
            Debug.WriteLine("Configuration.Appearing");

            // If we have an enterState already, then we're returning here from the login page and we need
            // to make sure the view model is updated with the database. Otherwise we're coming from the home
            // page and need to save the entry state.

            if (enterState != null)
            {
                viewModel.Refresh();
            }
            else
            {
                enterState = viewModel.GetState();                
            }           

            base.OnAppearing();
        }

        protected override async void OnDisappearing()
        {
            Debug.WriteLine("Configuration.OnDisappearing");                        
            base.OnDisappearing();

            // This updates the HasChanged flag in the view model based on whether anything in the configuration
            // actually changed, and updates the backend (except on Windows Phone where we run unauthenticated
            // until Xamarin.Auth supports that platform).
            await viewModel.CheckChanges(enterState, (ChangeCheckType.CategorySelection | ChangeCheckType.ConversationLimit),
                Device.OnPlatform(true, true, false));            
        }
    }
}
