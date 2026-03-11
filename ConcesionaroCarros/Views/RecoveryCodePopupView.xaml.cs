using System.Windows;
using System.Windows.Threading;

namespace ConcesionaroCarros.Views
{
    public partial class RecoveryCodePopupView : Window
    {
        private readonly DispatcherTimer _timer;
        private int _secondsRemaining = 5;

        public RecoveryCodePopupView(string code)
        {
            InitializeComponent();

            txtCode.Text = code ?? "000000";
            ActualizarContador();

            _timer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            Loaded += (s, e) => _timer.Start();
            Closed += (s, e) => _timer.Stop();
        }

        private void Timer_Tick(object sender, System.EventArgs e)
        {
            _secondsRemaining--;

            if (_secondsRemaining <= 0)
            {
                _timer.Stop();
                Close();
                return;
            }

            ActualizarContador();
        }

        private void ActualizarContador()
        {
            txtCountdown.Text = "Esta ventana se cerrara en " + _secondsRemaining + " segundos.";
        }
    }
}
