using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.TableControl;

namespace TestExtension
{
    /// <summary>
    /// Interaction logic for TableWindowControl.xaml
    /// </summary>
    public partial class TableWindowControl : UserControl
    {
        private readonly TableWindowControlViewModel _viewModel;

        public TableWindowControl(TableWindowControlViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            TableControl = _viewModel.GetTableControl();
            TableHost.Content = TableControl.Control;
        }

        public UserControl SettingControl => this;

        public IWpfTableControl TableControl { get; }

        public void OnClose() => _viewModel.ShutDown();
    }
}
