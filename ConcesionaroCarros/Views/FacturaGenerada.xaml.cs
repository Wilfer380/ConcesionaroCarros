using ConcesionaroCarros.ViewModels;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ConcesionaroCarros.Views
{
    public partial class FacturaGenerada : UserControl
    {
        public FacturaGenerada()
        {
            if (GlobalFontSettings.FontResolver == null)
                GlobalFontSettings.FontResolver = new FontResolver();

            InitializeComponent();
        }

        private void GenerarFacturaPdf_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as FacturaGeneradaViewModel;

            if (vm == null || vm.CarrosSeleccionados.Count == 0)
            {
                MessageBox.Show("No hay vehículos para facturar.");
                return;
            }

            try
            {
                BtnPdf.Visibility = Visibility.Collapsed;
                Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                string carpeta =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                foreach (var carro in vm.CarrosSeleccionados)
                {
                    // 🔁 cambiar carro actual
                    vm.CarroActual = carro;
                    Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                    this.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    this.Arrange(new Rect(this.DesiredSize));

                    RenderTargetBitmap bitmap = new RenderTargetBitmap(
                        (int)this.ActualWidth,
                        (int)this.ActualHeight,
                        96,
                        96,
                        PixelFormats.Pbgra32);

                    bitmap.Render(this);

                    string tempImage = Path.Combine(Path.GetTempPath(), "factura.png");

                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));

                    using (FileStream fs = new FileStream(tempImage, FileMode.Create))
                        encoder.Save(fs);

                    string nombre = $"Factura_{carro.Placa}_{DateTime.Now:HHmmss}.pdf";
                    string archivo = Path.Combine(carpeta, nombre);

                    PdfDocument doc = new PdfDocument();
                    PdfPage page = doc.AddPage();

                    XGraphics gfx = XGraphics.FromPdfPage(page);
                    XImage img = XImage.FromFile(tempImage);

                    page.Width = img.PixelWidth * 72 / img.HorizontalResolution;
                    page.Height = img.PixelHeight * 72 / img.VerticalResolution;

                    gfx.DrawImage(img, 0, 0, page.Width, page.Height);

                    doc.Save(archivo);

                    img.Dispose();
                    File.Delete(tempImage);

                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = archivo,
                        UseShellExecute = true
                    });
                }

                MessageBox.Show($"Se generaron {vm.CarrosSeleccionados.Count} facturas correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDFs:\n" + ex.Message);
            }
            finally
            {
                BtnPdf.Visibility = Visibility.Visible;
            }
        }
    }
}
