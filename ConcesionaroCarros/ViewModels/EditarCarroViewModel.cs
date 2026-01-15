using ConcesionaroCarros.Commands;
using ConcesionaroCarros.Db;
using ConcesionaroCarros.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ConcesionaroCarros.ViewModels
{
    public class EditarCarroViewModel : BaseViewModel
    {
        private readonly CarrosViewModel _parent;
        private readonly CarrosDbService _db;

        public Carro Carro { get; }
        public bool SoloLectura { get; }
        public bool EsSoloLectura => SoloLectura;

        public ICommand GuardarCommand { get; }
        public ICommand CerrarCommand { get; }
        public ICommand CargarImagenCommand { get; }

        public EditarCarroViewModel(Carro carro, bool soloLectura, CarrosViewModel parent)
        {
            Carro = carro;
            SoloLectura = soloLectura;
            _parent = parent;
            _db = new CarrosDbService();

            GuardarCommand = new RelayCommand(_ =>
            {
                if (SoloLectura) return;


                if (Carro.Id == 0)
                {
                    _db.Insertar(Carro);
                    _parent.Carros.Add(Carro);
                }

                else
                {
                    _db.Actualizar(Carro);
                }

                _parent.CerrarModal();
            });

            CerrarCommand = new RelayCommand(_ => _parent.CerrarModal());

            CargarImagenCommand = new RelayCommand(_ =>
            {
                if (SoloLectura) return;

                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Imágenes|*.jpg;*.png"
                };
                if (dlg.ShowDialog() == true)
                    Carro.ImagenPath = dlg.FileName;
            });
        }
    }
}
