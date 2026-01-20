using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class ClientesViewModel : BaseViewModel
    {
        private readonly ClientesDbService _db;
        private List<Cliente> _todos;

        public ObservableCollection<Cliente> Clientes { get; }

        private object _modalView;
        public object ModalView
        {
            get => _modalView;
            set { _modalView = value; OnPropertyChanged(); }
        }

        private string _textoBusqueda;
        public string TextoBusqueda
        {
            get => _textoBusqueda;
            set
            {
                _textoBusqueda = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        private string _filtroTipo = "Todos";
        public string FiltroTipo
        {
            get => _filtroTipo;
            set
            {
                _filtroTipo = value;
                OnPropertyChanged();
                AplicarFiltros();
            }
        }

        public ICommand AgregarClienteCommand { get; }
        public ICommand EditarClienteCommand { get; }
        public ICommand EliminarClienteCommand { get; }

        public ClientesViewModel()
        {
            _db = new ClientesDbService();
            Clientes = new ObservableCollection<Cliente>();

          
            _todos = _db.ObtenerTodos();

      
            ActualizarLista(_todos);

            AgregarClienteCommand = new RelayCommand(_ =>
            {
                ModalView = new EditarClienteView
                {
                    DataContext = new EditarClienteViewModel(this, new Cliente())
                };
            });

            EditarClienteCommand = new RelayCommand(c =>
            {
                ModalView = new EditarClienteView
                {
                    DataContext = new EditarClienteViewModel(this, (Cliente)c)
                };
            });

            EliminarClienteCommand = new RelayCommand(c =>
            {
                var cliente = (Cliente)c;

                if (MessageBox.Show(
                    $"¿Eliminar a {cliente.Nombres} {cliente.Apellidos}?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

                _db.Eliminar(cliente.Id);
                _todos.Remove(cliente);
                AplicarFiltros();
            });
        }

        private void AplicarFiltros()
        {
            IEnumerable<Cliente> lista = _todos;

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var t = TextoBusqueda.ToLower();
                lista = lista.Where(c =>
                    c.Nombres.ToLower().Contains(t) ||
                    c.Apellidos.ToLower().Contains(t) ||
                    c.Cedula.ToLower().Contains(t) ||
                    c.Telefono.ToLower().Contains(t));
            }

            if (FiltroTipo != "Todos")
                lista = lista.Where(c => c.TipoCliente == FiltroTipo);

            ActualizarLista(lista);
        }

        private void ActualizarLista(IEnumerable<Cliente> lista)
        {
            Clientes.Clear();
            foreach (var c in lista)
                Clientes.Add(c);
        }

        public void GuardarCliente(Cliente cliente)
        {
            if (cliente.Id == 0)
            {
                _db.Insertar(cliente);
                _todos.Insert(0, cliente);
            }
            else
            {
                _db.Actualizar(cliente);
            }

            AplicarFiltros();
            ModalView = null;
        }

        public void CerrarModal()
        {
            ModalView = null;
        }
    }
}