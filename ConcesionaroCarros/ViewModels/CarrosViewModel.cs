using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class CarrosViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;
        private readonly CarrosDbService _db;

        public ObservableCollection<Carro> Carros { get; }

        public ICommand SeleccionarCarroCommand { get; }
        public ICommand VolverDashboardCommand { get; }

        public ICommand EditarCarroCommand { get; }
        public ICommand VerDetalleCommand { get; }
        public ICommand EliminarCarroCommand { get; }
        public ICommand AgregarCarroCommand { get; }
        public ICommand AgregarAlCarritoCommand { get; }

        private object _modalView;
        public object ModalView
        {
            get => _modalView;
            set { _modalView = value; OnPropertyChanged(); }
        }

        public CarrosViewModel(MainViewModel main)
        {
            _main = main;
            _db = new CarrosDbService();

            Carros = new ObservableCollection<Carro>();
            foreach (var carro in _db.ObtenerTodos())
                Carros.Add(carro);

            // 🛒 SELECCIONAR → AGREGAR AL CARRITO
            SeleccionarCarroCommand = new RelayCommand(c =>
            {
                var carro = c as Carro;
                if (carro == null) return;

                if (!_main.Carrito.Contains(carro))
                {
                    _main.Carrito.Add(carro);
                    _main.ActualizarCarrito();
                }
            });

            VolverDashboardCommand = new RelayCommand(_ =>
            {
                _main.ShowDashboardCommand.Execute(null);
            });

            EditarCarroCommand = new RelayCommand(c =>
            {
                var carro = c as Carro;
                if (carro == null) return;

                ModalView = new EditarCarroView
                {
                    DataContext = new EditarCarroViewModel(carro, false, this)
                };
            });

            VerDetalleCommand = new RelayCommand(c =>
            {
                var carro = c as Carro;
                if (carro == null) return;

                ModalView = new EditarCarroView
                {
                    DataContext = new EditarCarroViewModel(carro, true, this)
                };
            });

            EliminarCarroCommand = new RelayCommand(c =>
            {
                var carro = c as Carro;
                if (carro == null) return;

                _db.Eliminar(carro.Id);
                Carros.Remove(carro);
            });

            AgregarCarroCommand = new RelayCommand(_ =>
            {
                var nuevoCarro = new Carro { Estado = "Disponible" };

                ModalView = new EditarCarroView
                {
                    DataContext = new EditarCarroViewModel(nuevoCarro, false, this)
                };
            });

            AgregarAlCarritoCommand = new RelayCommand(c =>
            {
                var carro = c as Carro;
                if (carro == null) return;

                _main.Carrito.Add(carro);
                _main.ActualizarCarrito();
            });
        }

        public void CerrarModal()
        {
            ModalView = null;
        }
    }
}