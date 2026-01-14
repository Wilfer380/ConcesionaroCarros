using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        public ICommand ShowCarrosCommand { get; }
        public ICommand ShowClientesCommand { get; }
        public ICommand ShowEmpleadosCommand { get; }
        public ICommand ShowVentaCommand { get; }
        public ICommand ShowFacturaCommand { get; }

        public MainViewModel()
        {
            ShowCarrosCommand = new RelayCommand(_ => CurrentView = new CarrosView { DataContext = new CarrosViewModel(this) });
            ShowClientesCommand = new RelayCommand(_ => CurrentView = new ClientesView { DataContext = new ClientesViewModel(this) });
            ShowEmpleadosCommand = new RelayCommand(_ => CurrentView = new EmpleadosView { DataContext = new EmpleadosViewModel(this) });
            ShowVentaCommand = new RelayCommand(_ => CurrentView = new VentaView { DataContext = new VentaViewModel(this) });
            ShowFacturaCommand = new RelayCommand(_ => CurrentView = new FacturaView { DataContext = new FacturaViewModel(this) });

            CurrentView = new CarrosView { DataContext = new CarrosViewModel(this) };
        }

        // Estado compartido
        public Models.Carro CarroSeleccionado { get; set; }
        public Models.Cliente ClienteActual { get; set; }
        public Models.Empleado EmpleadoActual { get; set; }
        public Models.Factura FacturaActual { get; set; }
    }
}
