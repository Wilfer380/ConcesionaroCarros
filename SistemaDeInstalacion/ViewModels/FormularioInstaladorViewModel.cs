using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class FormularioInstaladorViewModel : BaseViewModel
    {
        public const string CarpetaPuntoLocal = "Punto local de desarrollo planta";
        public const string CarpetaDesarrolloGlobal = "Desarrollo global";

        private readonly InstaladoresViewModel _parent;
        private readonly InstaladorDbService _db;
        private readonly Action _closeAction;

        public Instalador Instalador { get; set; }

        public bool EsSoloLectura { get; private set; }
        public bool EsEdicion { get; private set; }
        public bool PuedeEditar => !EsSoloLectura;
        public ObservableCollection<string> CarpetasDisponibles { get; } =
            new ObservableCollection<string> { CarpetaPuntoLocal, CarpetaDesarrolloGlobal };

        // ?? CONTROL DE BOTONES
        public bool EsNuevo
        {
            get { return !EsSoloLectura && !EsEdicion; }
        }

        public string TituloFormulario
        {
            get
            {
                if (EsSoloLectura) return "Ver Instalador";
                if (EsEdicion) return "Editar Instalador";
                return "Nuevo Instalador";
            }
        }

        public ICommand GuardarCommand { get; private set; }
        public ICommand CerrarCommand { get; private set; }
        public ICommand BuscarArchivoCommand { get; private set; }

        public FormularioInstaladorViewModel(
            InstaladoresViewModel parent,
            Instalador instalador = null,
            bool soloLectura = false,
            bool edicion = false,
            Action closeAction = null)
        {
            _parent = parent;
            _db = new InstaladorDbService();
            _closeAction = closeAction;

            EsSoloLectura = soloLectura;
            EsEdicion = edicion;

            if (instalador == null)
            {
                Instalador = new Instalador
                {
                    Carpeta = NormalizarCarpeta(_parent?.CarpetaSeleccionada)
                };
            }
            else
            {
                Instalador = new Instalador
                {
                    Id = instalador.Id,
                    Ruta = instalador.Ruta,
                    Nombre = instalador.Nombre,
                    Descripcion = instalador.Descripcion,
                    Carpeta = NormalizarCarpeta(instalador.Carpeta)
                };
            }

            BuscarArchivoCommand = new RelayCommand(o =>
            {
                if (EsSoloLectura) return;

                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "Ejecutables (*.exe)|*.exe";

                if (dlg.ShowDialog() == true)
                {
                    Instalador.Ruta = dlg.FileName;

                    if (string.IsNullOrWhiteSpace(Instalador.Nombre))
                        Instalador.Nombre = Path.GetFileNameWithoutExtension(dlg.FileName);

                    LogService.Info("FormularioInstalador", "Archivo ejecutable seleccionado", ConstruirDetalleInstalador());
                }
            });

            GuardarCommand = new RelayCommand(o =>
            {
                if (EsSoloLectura) return;
                Instalador.Carpeta = NormalizarCarpeta(Instalador.Carpeta);

                if (EsEdicion)
                {
                    _db.Actualizar(Instalador);
                    LogService.Info("FormularioInstalador", "Instalador actualizado", ConstruirDetalleInstalador());
                }
                else
                {
                    _db.Guardar(Instalador);
                    LogService.Info("FormularioInstalador", "Instalador creado", ConstruirDetalleInstalador());
                }

                _parent.CerrarModal();
                _closeAction?.Invoke();
            });

            CerrarCommand = new RelayCommand(o =>
            {
                _parent.CerrarModal();
                _closeAction?.Invoke();
            });
        }

        private string NormalizarCarpeta(string carpeta)
        {
            if (string.Equals(carpeta, CarpetaPuntoLocal, System.StringComparison.OrdinalIgnoreCase))
                return CarpetaPuntoLocal;

            if (string.Equals(carpeta, CarpetaDesarrolloGlobal, System.StringComparison.OrdinalIgnoreCase))
                return CarpetaDesarrolloGlobal;

            return CarpetaDesarrolloGlobal;
        }

        private string ConstruirDetalleInstalador()
        {
            return $"Id={Instalador?.Id ?? 0}; Nombre={Instalador?.Nombre}; Carpeta={Instalador?.Carpeta}; Ruta={Instalador?.Ruta}";
        }
    }
}

