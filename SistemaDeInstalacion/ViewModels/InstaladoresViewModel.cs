using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using ConcesionaroCarros.Services;
using ConcesionaroCarros.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ConcesionaroCarros.ViewModels
{
    public class InstaladoresViewModel : BaseViewModel
    {
        private const string CarpetaPuntoLocal = FormularioInstaladorViewModel.CarpetaPuntoLocal;
        private const string CarpetaDesarrolloGlobal = FormularioInstaladorViewModel.CarpetaDesarrolloGlobal;

        private readonly InstaladorDbService _instaladorDb;
        public bool PuedeGestionarInstaladores => SesionUsuario.EsAdmin;

        public ObservableCollection<Instalador> Instaladores { get; private set; }
        public ObservableCollection<string> Carpetas { get; } =
            new ObservableCollection<string> { CarpetaPuntoLocal, CarpetaDesarrolloGlobal };

        private string _carpetaSeleccionada = CarpetaDesarrolloGlobal;
        public string CarpetaSeleccionada
        {
            get => _carpetaSeleccionada;
            set
            {
                _carpetaSeleccionada = NormalizarCarpeta(value);
                OnPropertyChanged(nameof(CarpetaSeleccionada));
                OnPropertyChanged(nameof(CarpetaLocalSeleccionada));
                OnPropertyChanged(nameof(CarpetaGlobalSeleccionada));
                CargarInstaladores();
            }
        }

        public bool CarpetaLocalSeleccionada =>
            string.Equals(CarpetaSeleccionada, CarpetaPuntoLocal, StringComparison.OrdinalIgnoreCase);

        public bool CarpetaGlobalSeleccionada =>
            string.Equals(CarpetaSeleccionada, CarpetaDesarrolloGlobal, StringComparison.OrdinalIgnoreCase);

        public ICommand BuscarInstaladorCommand { get; private set; }
        public ICommand EjecutarInstaladorCommand { get; private set; }
        public ICommand EliminarInstaladorCommand { get; private set; }
        public ICommand VerInstaladorCommand { get; private set; }
        public ICommand EditarInstaladorCommand { get; private set; }
        public ICommand SeleccionarCarpetaCommand { get; private set; }

        public InstaladoresViewModel(MainViewModel main)
        {
            _instaladorDb = new InstaladorDbService();
            Instaladores = new ObservableCollection<Instalador>();

            CargarInstaladores();

            SeleccionarCarpetaCommand = new RelayCommand(o =>
            {
                var carpetaDestino = o as string;
                CarpetaSeleccionada = carpetaDestino;
                LogService.Info("Instaladores", "Cambio de carpeta de instaladores", NormalizarCarpeta(carpetaDestino));
            });

            BuscarInstaladorCommand = new RelayCommand(o =>
            {
                if (!PuedeGestionarInstaladores)
                    return;

                LogService.Info("Instaladores", "Apertura de formulario de instalador", "Nuevo instalador");
                AbrirFormularioInstalador();
            });

            VerInstaladorCommand = new RelayCommand(o =>
            {
                var inst = o as Instalador;
                if (inst != null)
                {
                    LogService.Info("Instaladores", "Visualizacion de instalador", ConstruirDetalleInstalador(inst));
                    AbrirFormularioInstalador(inst, true, false);
                }
            });

            EditarInstaladorCommand = new RelayCommand(o =>
            {
                if (!PuedeGestionarInstaladores)
                    return;

                var inst = o as Instalador;
                if (inst != null)
                {
                    LogService.Info("Instaladores", "Apertura de formulario de edicion de instalador", ConstruirDetalleInstalador(inst));
                    AbrirFormularioInstalador(inst, false, true);
                }
            });

            EliminarInstaladorCommand = new RelayCommand(o =>
            {
                if (!PuedeGestionarInstaladores)
                    return;

                var inst = o as Instalador;
                if (inst != null)
                {
                    _instaladorDb.EliminarRuta(inst.Ruta);
                    Instaladores.Remove(inst);
                    LogService.Info("Instaladores", "Instalador eliminado", ConstruirDetalleInstalador(inst));
                }
            });

            EjecutarInstaladorCommand = new RelayCommand(o =>
            {
                var inst = o as Instalador;

                if (inst != null && File.Exists(inst.Ruta))
                {
                    try
                    {
                        LogService.Info("Instaladores", "Ejecucion de instalador iniciada", ConstruirDetalleInstalador(inst));
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = inst.Ruta,
                            UseShellExecute = true
                        });
                        LogService.Info("Instaladores", "Ejecucion de instalador lanzada", ConstruirDetalleInstalador(inst));
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        // Codigo 1223 = usuario cancelo UAC.
                        if (ex.NativeErrorCode == 1223)
                        {
                            LogService.Warning("Instaladores", "Ejecucion de instalador cancelada por el usuario", ConstruirDetalleInstalador(inst));
                            return;
                        }

                        LogService.Error("Instaladores", "No se pudo ejecutar el instalador", ex, ConstruirDetalleInstalador(inst));
                        MessageBox.Show(LocalizedText.Get("Installers_RunErrorMessage", "No se pudo ejecutar el instalador."),
                                        LocalizedText.Get("Common_ErrorTitle", "Error"),
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("Instaladores", "Error inesperado al ejecutar instalador", ex, ConstruirDetalleInstalador(inst));
                        MessageBox.Show(LocalizedText.Get("Installers_RunUnexpectedErrorMessage", "Ocurrio un error inesperado al ejecutar el instalador."),
                                        LocalizedText.Get("Common_ErrorTitle", "Error"),
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                    }
                }
                else if (inst != null)
                {
                    LogService.Warning("Instaladores", "No se encontro el archivo del instalador al intentar ejecutarlo", ConstruirDetalleInstalador(inst));
                }
            });
        }

        private void CargarInstaladores()
        {
            Instaladores.Clear();

            var todos = _instaladorDb.ObtenerTodos();

            if (!SesionUsuario.EsAdmin)
            {
                var asignados = SesionUsuario.UsuarioActual?.ObtenerAplicativosAsignados()
                                ?? new List<string>();

                var rutasAsignadas = new HashSet<string>(
                    asignados.Where(r => !string.IsNullOrWhiteSpace(r)),
                    StringComparer.OrdinalIgnoreCase);

                todos = todos
                    .Where(x => !string.IsNullOrWhiteSpace(x.Ruta) && rutasAsignadas.Contains(x.Ruta))
                    .ToList();
            }

            var carpetaActual = NormalizarCarpeta(CarpetaSeleccionada);
            todos = todos
                .Where(x => string.Equals(
                    NormalizarCarpeta(x.Carpeta),
                    carpetaActual,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var item in todos)
                Instaladores.Add(item);
        }

        public void CerrarModal()
        {
            CargarInstaladores();
        }

        private void AbrirFormularioInstalador(
            Instalador instalador = null,
            bool soloLectura = false,
            bool edicion = false)
        {
            Window dialog = null;
            var owner = Application.Current?.MainWindow;

            var view = new FormularioInstaladorView();
            var vm = new FormularioInstaladorViewModel(
                this,
                instalador,
                soloLectura,
                edicion,
                () => dialog?.Close());

            view.DataContext = vm;

            dialog = new Window
            {
                Content = view,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            using (var overlay = new ModalOverlayScope(owner))
            {
                dialog.Owner = overlay.OverlayWindow ?? owner;
                dialog.ShowDialog();
            }
        }

        private static string NormalizarCarpeta(string carpeta)
        {
            if (string.Equals(carpeta, CarpetaPuntoLocal, StringComparison.OrdinalIgnoreCase))
                return CarpetaPuntoLocal;

            if (string.Equals(carpeta, CarpetaDesarrolloGlobal, StringComparison.OrdinalIgnoreCase))
                return CarpetaDesarrolloGlobal;

            return CarpetaDesarrolloGlobal;
        }

        private static string ConstruirDetalleInstalador(Instalador instalador)
        {
            if (instalador == null)
                return "Sin instalador";

            return $"Id={instalador.Id}; Nombre={instalador.Nombre}; Carpeta={instalador.Carpeta}; Ruta={instalador.Ruta}";
        }
    }
}

