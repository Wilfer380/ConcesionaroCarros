using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;

namespace ConcesionaroCarros.ViewModels
{
    public class PuntoVentaViewModel : BaseViewModel
    {
        private readonly MainViewModel _main;

        private object _pasoActualView;
        public object PasoActualView
        {
            get => _pasoActualView;
            set
            {
                _pasoActualView = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Carro> Carrito => _main.Carrito;

        public ObservableCollection<DetalleOperacionViewModel> DetallesOperacion { get; }

        public ICommand QuitarDelCarritoCommand { get; }
        public ICommand FinalizarVentaCommand { get; }

        public PuntoVentaViewModel(MainViewModel main)
        {
            _main = main;

            PasoActualView = this;

            DetallesOperacion = new ObservableCollection<DetalleOperacionViewModel>();

         
            foreach (var carro in _main.Carrito)
            {
                DetallesOperacion.Add(new DetalleOperacionViewModel(carro));
            }

          
            _main.Carrito.CollectionChanged += (s, e) =>
            {
          
                if (e.NewItems != null)
                {
                    foreach (Carro carro in e.NewItems)
                    {
                        DetallesOperacion.Add(new DetalleOperacionViewModel(carro));
                    }
                }
                
                if (e.OldItems != null)
                {
                    foreach (Carro carro in e.OldItems)
                    {
                        var item = DetallesOperacion
                            .FirstOrDefault(d => d.Vehiculo == carro);

                        if (item != null)
                            DetallesOperacion.Remove(item);
                    }
                }
            };

        
            QuitarDelCarritoCommand = new RelayCommand(c =>
            {
                var carro = c as Carro;
                if (carro == null) return;

                carro.UnidadesDisponibles++;
                _main.Carrito.Remove(carro);
                _main.ActualizarCarrito();
            });

            FinalizarVentaCommand = new RelayCommand(_ =>
            {
                _main.Carrito.Clear();
                DetallesOperacion.Clear();
                _main.ActualizarCarrito();
                _main.VistaActiva = null;
                _main.ShowDashboardCommand.Execute(null);
            });
        }
    }
}
