using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Community.VisualStudio.Toolkit
{
    public partial class ToolkitPackage
    {
        private static readonly Dictionary<Assembly, JoinableTaskFactory> _joinableTaskFactoriesByAssembly = new();
        private static readonly Dictionary<string, JoinableTaskFactory> _joinableTaskFactoriesByDirectory = new(StringComparer.OrdinalIgnoreCase);

        private void InitializeJoinableTaskFactory()
        {
            Assembly? assembly = GetType().Assembly;
            if (assembly is not null)
            {
                if (!_joinableTaskFactoriesByAssembly.ContainsKey(assembly))
                {
                    _joinableTaskFactoriesByAssembly[assembly] = JoinableTaskFactory;
                }

                var directory = Path.GetDirectoryName(assembly.Location);
                if (!_joinableTaskFactoriesByDirectory.ContainsKey(directory))
                {
                    _joinableTaskFactoriesByDirectory[directory] = JoinableTaskFactory;
                }
            }
        }

        internal static JoinableTaskFactory GetJoinableTaskFactory()
        {
            // If there's only one registered `JoinableTaskFactory`,
            // then it must be the one we need to use.
            if (_joinableTaskFactoriesByAssembly.Count == 1)
            {
                return _joinableTaskFactoriesByAssembly.First().Value;
            }

            // There should always be at least one registered `JoinableTaskFactory`,
            // but just in case there isn't, we'll safe ourselves from walking the call stack.
            if (_joinableTaskFactoriesByAssembly.Count > 0)
            {
                // Walk up the call stack looking for a method that comes from
                // the same assembly as the `ToolkitPackage` implementation that
                // registered the JoinableTaskFactory when it was initialized.
                var stack = new StackTrace(1, false);

                for (var i = 0; i < stack.FrameCount; i++)
                {
                    StackFrame frame = stack.GetFrame(i);
                    Assembly assembly = frame.GetMethod().DeclaringType.Assembly;

                    // Ignore any method calls from our assembly because our assembly won't have a
                    // `JoinableTaskFactory` registered, and it will most likely be in the same
                    // directory as the assembly that contains the package that registered itself.
                    if (assembly == typeof(ToolkitPackage).Assembly)
                    {
                        continue;
                    }

                    if (_joinableTaskFactoriesByAssembly.TryGetValue(assembly, out JoinableTaskFactory? factory))
                    {
                        return factory;
                    }

                    // There may be multiple assemblies that make up an extension, so also look for a `JoinableTaskFactory`
                    // that was registered from an assembly in the same directory as the current method call.
                    var directory = Path.GetDirectoryName(assembly.Location);
                    if (_joinableTaskFactoriesByDirectory.TryGetValue(directory, out factory))
                    {
                        return factory;
                    }
                }

                Debug.WriteLine($"No JoinableTaskFactory found for method '{stack.GetFrame(0).GetMethod().DeclaringType.FullName}.{stack.GetFrame(0).GetMethod().Name}'.");
            }

            // We couldn't find a JoinableTaskFactory that belongs to a package,
            // so just fall back to using the one from the `ThreadHelper` type.
            return ThreadHelper.JoinableTaskFactory;
        }

    }
}
