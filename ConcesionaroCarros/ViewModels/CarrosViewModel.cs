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


            SeleccionarCarroCommand = new RelayCommand(c =>
            {
                var carro = c as Carro;
                if (carro == null) return;
                _main.CarroSeleccionado = carro;
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
                    DataContext = new EditarCarroViewModel((Carro)c, false, this)
                };
            });

            VerDetalleCommand = new RelayCommand(c =>
            {
                var carro = c as Carro;
                if (carro == null) return;
                ModalView = new EditarCarroView
                {
                    DataContext = new EditarCarroViewModel((Carro)c, true, this)
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
                var nuevoCarro = new Carro
                {
                    Estado = "Disponible"
                };

                ModalView = new EditarCarroView
                {
                    DataContext = new EditarCarroViewModel(nuevoCarro, false, this)
                };
            });
        }

        public void CerrarModal()
        {
            ModalView = null;
        }
    }
}
