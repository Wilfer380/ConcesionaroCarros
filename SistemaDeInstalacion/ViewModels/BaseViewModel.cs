using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ConcesionaroCarros.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged, ILocalizableViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        protected void RaisePropertyChanges(params string[] propertyNames)
        {
            if (propertyNames == null)
                return;

            foreach (var propertyName in propertyNames)
                OnPropertyChanged(propertyName);
        }

        public virtual void RefreshLocalization()
        {
            OnPropertyChanged(string.Empty);
        }
    }
}
