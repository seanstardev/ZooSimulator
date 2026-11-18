using System.ComponentModel;

namespace ZooSimulator.App.WPF
{
    /// <summary>Native presentation status only; owns no simulation state or actor lookups.</summary>
    internal sealed class MainViewModel : INotifyPropertyChanged
    {
        private string status = "Preparing the zoo…";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Status
        {
            get => status;
            set
            {
                if (status == value)
                    return;

                status = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
        }
    }
}
