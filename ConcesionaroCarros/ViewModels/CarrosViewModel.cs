using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class CarrosViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;

        public ObservableCollection<Carro> Carros { get; set; }

        public ICommand SeleccionarCarroCommand { get; }

        public CarrosViewModel(MainViewModel main)
        {
            _main = main;

            Carros = new ObservableCollection<Carro>
            {
                new Carro { Marca="Audi", Modelo="RS5 Coupé", Anio=2024, Color="Negro", PrecioBase=350_000_000 },
                new Carro { Marca="BMW", Modelo="M4 Competition", Anio=2023, Color="Azul", PrecioBase=420_000_000 }
            };

            SeleccionarCarroCommand = new RelayCommand(c =>
            {
                _main.CarroSeleccionado = (Carro)c;
                _main.ShowClientesCommand.Execute(null);
            });
        }
    }
}
