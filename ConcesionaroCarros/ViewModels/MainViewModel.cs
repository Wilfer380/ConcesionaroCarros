using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Views;
using System.Collections.ObjectModel;
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
        
        private string _vistaActiva;
        public string VistaActiva
        {
            get => _vistaActiva;
            set
            {
                _vistaActiva = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Carro> Carrito { get; } =
            new ObservableCollection<Carro>();

        public int CarritoCount => Carrito.Count;

        public void ActualizarCarrito()
        {
            OnPropertyChanged(nameof(CarritoCount));
        }

        public Carro CarroSeleccionado { get; set; }

   
        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowCarrosCommand { get; }
        public ICommand ShowClientesCommand { get; }
        public ICommand ShowEmpleadosCommand { get; }
        public ICommand ShowGestionUsuariosCommand { get; }
        public ICommand ShowVentaCommand { get; }

        public MainViewModel()
        {
            ShowDashboardCommand = new RelayCommand(_ =>
            {
                VistaActiva = null;
                CurrentView = null;
            });

            ShowCarrosCommand = new RelayCommand(_ =>
            {
                VistaActiva = "Carros";
                CurrentView = new CarrosView
                {
                    DataContext = new CarrosViewModel(this)
                };
            });

            ShowClientesCommand = new RelayCommand(_ =>
            {
                VistaActiva = "Clientes";
                CurrentView = new ClientesView
                {
                    DataContext = new ClientesViewModel()
                };
            });

            ShowEmpleadosCommand = new RelayCommand(_ =>
            {
                VistaActiva = "Empleados";
                CurrentView = new EmpleadosView
                {
                    DataContext = new EmpleadosViewModel()
                };
            });

            ShowGestionUsuariosCommand = new RelayCommand(_ =>
            {
                CurrentView = new GestionUsuarioView();
            });



            ShowVentaCommand = new RelayCommand(_ =>
            {
                if (Carrito.Count > 0)
                {
                    VistaActiva = "Venta";
                    CurrentView = new PuntoVentaView
                    {
                        DataContext = new PuntoVentaViewModel(this)
                    };
                }
            });

            CurrentView = null;
        }
    }
}
