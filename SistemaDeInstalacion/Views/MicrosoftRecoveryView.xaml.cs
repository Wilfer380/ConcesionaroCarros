using ConcesionaroCarros.ViewModels;
using System;
using System.ComponentModel;
using System.Windows;

namespace ConcesionaroCarros.Views
{
    public partial class MicrosoftRecoveryView : Window
    {
        public event Action<string, string> RecoveryCompleted;

        public MicrosoftRecoveryView()
        {
            InitializeComponent();

            var vm = new MicrosoftRecoveryViewModel();
            vm.RequestClose += () => Close();
            vm.RecoveryCompleted += (usuario, password) => RecoveryCompleted?.Invoke(usuario, password);
            vm.ShowCodeRequested += MostrarCodigoTemporal;
            vm.PropertyChanged += Vm_PropertyChanged;
            DataContext = vm;
        }

        private void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MicrosoftRecoveryViewModel.IsValidationStep))
            {
                var vm = DataContext as MicrosoftRecoveryViewModel;
                if (vm != null && vm.IsValidationStep)
                {
                    txtNuevaPassword.Password = string.Empty;
                    txtConfirmarPassword.Password = string.Empty;
                }
            }
        }

        private void NuevaPasswordChanged(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MicrosoftRecoveryViewModel;
            if (vm != null)
                vm.NuevaPassword = txtNuevaPassword.Password ?? string.Empty;
        }

        private void ConfirmarPasswordChanged(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MicrosoftRecoveryViewModel;
            if (vm != null)
                vm.ConfirmarPassword = txtConfirmarPassword.Password ?? string.Empty;
        }

        private void MostrarCodigoTemporal(string code)
        {
            var vm = DataContext as MicrosoftRecoveryViewModel;
            var popup = new RecoveryCodePopupView(code)
            {
                Owner = this
            };

            popup.ShowDialog();
            vm?.ActivarPasoCambioPassword();
        }
    }
}
