The Altostratus mobile client is a Xamarin project that builds for iOS, Android, and Windows Phone. It is necessary to have the appropriate Xamarin licenses to build any of the projects.

The Altostratus (Portable) project contains source code for the PCL that's shared across all three platforms, whose individual projects
are Altostratus.Android, Altostratus.iOS, and Altostratus.WinPhone.

The PCL is also used by the DBInitialize utility, which is a Win32 console application that creates a pre-populated SQLite database (Altostratus.db3).
A copy of this database is placed into the resources of each platform-specific project. 

Other notes:

(1) The Altostratus PCL is oriented around the MVVM pattern, but the Views (Home.xaml, Configuration.xaml, and Item.xaml) are not broken out into
a separate PCL with design-time data that would allow a designer using Blend to work on it separately from the rest of the project. Doing this work
is beyond the scope of this sample. For more information, see http://blogs.xamlninja.com/silverlight/mvvm-design-time-data-and-blendability.

(2) The Home.xaml creates an instance of HomeViewModel directly. Some developers prefer to use a view model locator pattern to accomplish this, 
but again doing so is beyond the scope of this sample.


