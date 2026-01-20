using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Views;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDashboardVisible));
            }
        }

        public bool IsDashboardVisible => CurrentView == null;

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowCarrosCommand { get; }
        public ICommand ShowClientesCommand { get; }
        public ICommand ShowEmpleadosCommand { get; }

        public MainViewModel()
        {
            ShowDashboardCommand = new RelayCommand(_ =>
            {
                CurrentView = null;
            });

            ShowCarrosCommand = new RelayCommand(_ =>
            {
                CurrentView = new CarrosView
                {
                    DataContext = new CarrosViewModel(this)
                };
            });

            ShowClientesCommand = new RelayCommand(_ =>
            {
                CurrentView = new ClientesView
                {
                    DataContext = new ClientesViewModel()
                };
            });

            ShowEmpleadosCommand = new RelayCommand(_ =>
            {
                CurrentView = new EmpleadosView
                {
                    DataContext = new EmpleadosViewModel()
                };
            });



            CurrentView = null;
        }
        
        public Models.Carro CarroSeleccionado { get; set; }
    }
}
