using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Community.VisualStudio.Toolkit;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableManager;

namespace TestExtension
{
    public class TableWindow : BaseToolWindow<TableWindow>
    {
        public override string GetTitle(int toolWindowId) => "Table Demo Window";

        public override Type PaneType => typeof(Pane);

        public async override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            IWpfTableControlProvider controlProvider = await VS.GetMefServiceAsync<IWpfTableControlProvider>();
            ITableManagerProvider tableManagerProvider = await VS.GetMefServiceAsync<ITableManagerProvider>();
            TableWindowControlViewModel vm = new(controlProvider, tableManagerProvider);
            return new TableWindowControl(vm);
        }

        [Guid("c9581b59-1031-469b-a0f9-feb8a93f73e3")]
        internal class Pane : ToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.Table;
            }
        }
    }
}
