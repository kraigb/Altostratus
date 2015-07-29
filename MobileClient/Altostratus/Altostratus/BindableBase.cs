using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Altostratus
{
    // BindableBase is a generic INotifyPropertyChanged implementation taken
    // from Charles Petzold's book, Creating Mobile Apps with Xamarin.Forms.
    // It is used as a base class for anything that would otherwise implement
    // INotifyPropertyChanged, and allows properties with simple setters (no
    // added processing) to be implemented with one line of code.

    public class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Object.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
        }
    }
}

