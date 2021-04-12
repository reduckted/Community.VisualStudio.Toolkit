using Microsoft.VisualStudio.Shell;

namespace Community.VisualStudio.Toolkit
{
    /// <summary>
    /// An <see cref="AsyncPackage"/> that provides additional functionality.
    /// </summary>
    public abstract partial class ToolkitPackage : AsyncPackage
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ToolkitPackage"/>.
        /// </summary>
        protected ToolkitPackage()
        {
            InitializeJoinableTaskFactory();
        }
    }
}
