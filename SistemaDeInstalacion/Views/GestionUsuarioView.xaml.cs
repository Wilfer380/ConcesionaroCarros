using ConcesionaroCarros.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ConcesionaroCarros.Views
{
    public partial class GestionUsuarioView : UserControl
    {
        public GestionUsuarioView()
        {
            InitializeComponent();
            DataContext = new GestionUsuarioViewModel();
        }

        private void RootGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as GestionUsuarioViewModel;
            if (vm == null || !vm.IsPanelAsignacionVisible)
                return;

            var source = e.OriginalSource as DependencyObject;
            if (source == null)
                return;

            // Mantiene el panel visible mientras se usa cualquier control de scroll.
            if (FindAncestor<ScrollBar>(source) != null ||
                FindAncestor<Thumb>(source) != null ||
                FindAncestor<Track>(source) != null ||
                FindAncestor<RepeatButton>(source) != null)
                return;

            if (IsDescendantOf(source, AsignacionPanel) || FindAncestor<DataGridRow>(source) != null)
                return;

            vm.OcultarPanelAsignacion();
        }

        private void AccionButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as GestionUsuarioViewModel;
            vm?.MarcarSiguienteSeleccionComoAccion();
        }

        private static bool IsDescendantOf(DependencyObject child, DependencyObject parent)
        {
            var current = child;
            while (current != null)
            {
                if (ReferenceEquals(current, parent))
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static T FindAncestor<T>(DependencyObject child) where T : DependencyObject
        {
            var current = child;
            while (current != null)
            {
                if (current is T match)
                    return match;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
