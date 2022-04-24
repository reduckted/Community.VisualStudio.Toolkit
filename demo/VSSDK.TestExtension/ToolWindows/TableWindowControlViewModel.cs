using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using Community.VisualStudio.Toolkit;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace TestExtension
{
    public class TableWindowControlViewModel : ITableDataSource
    {
        private readonly List<ITableDataSink> _tableSinks = new();
        private readonly IWpfTableControlProvider _controlProvider;
        private readonly ITableManager _tableManager;
        private readonly IEnumerable<Project> _data;
        private ITableEntriesSnapshot _lastPublishedSnapshot;

        public TableWindowControlViewModel(IWpfTableControlProvider controlProvider, ITableManagerProvider tableMangerProvider)
        {
            _controlProvider = controlProvider;
            _tableManager = tableMangerProvider.GetTableManager(Identifier);
            _data = ThreadHelper.JoinableTaskFactory.Run(() => VS.Solutions.GetAllProjectsAsync());
            _tableManager.AddSource(this);
        }


        public string SourceTypeIdentifier => "DemoTableControl";

        public string Identifier => "DemoTable";

        public string DisplayName => "Demo Table";

        public void NotifyOfUpdate()
        {
            DemoTableEntriesSnapshot snapshot = CreateSnapshot();

            foreach (ITableDataSink tableSink in _tableSinks)
            {
                tableSink.ReplaceSnapshot(snapshot, _lastPublishedSnapshot);
            }

            _lastPublishedSnapshot = snapshot;
        }

        private DemoTableEntriesSnapshot CreateSnapshot()
        {
            return new DemoTableEntriesSnapshot(ImmutableArray.CreateRange(_data), 0);
        }

        public IDisposable Subscribe(ITableDataSink sink)
        {
            _tableSinks.Add(sink);
            _lastPublishedSnapshot = CreateSnapshot();
            sink.AddSnapshot(_lastPublishedSnapshot);
            return new RemoveSinkWhenDisposed(_tableSinks, sink);
        }

        public IWpfTableControl4 GetTableControl()
        {
            IEnumerable<ColumnState2> initialColumnStates = GetInitialColumnStates();
            string[] fixedColumns = GetFixedColumns();
            return (IWpfTableControl4)_controlProvider.CreateControl(
                    _tableManager,
                    true,
                    initialColumnStates,
                    fixedColumns);
        }

        protected string[] GetFixedColumns()
        {
            return new[]
            {
                ColumnDefinitions.ProjectName,
                ColumnDefinitions.Kind
            };
        }

        private IEnumerable<ColumnState2> GetInitialColumnStates()
        {
            return new[] {
                new ColumnState2(ColumnDefinitions.ProjectName, isVisible: true, width: 100),
                new ColumnState2(ColumnDefinitions.Kind, isVisible: true, width: 100)
            };
        }

        public void ShutDown()
        {
            _tableManager.RemoveSource(this);
        }


        private class RemoveSinkWhenDisposed : IDisposable
        {
            private readonly List<ITableDataSink> _tableSinks;
            private readonly ITableDataSink _sink;

            public RemoveSinkWhenDisposed(List<ITableDataSink> tableSinks, ITableDataSink sink)
            {
                _tableSinks = tableSinks;
                _sink = sink;
            }

            public void Dispose()
            {
                _tableSinks.Remove(_sink);
            }
        }
    }

    internal class ColumnDefinitions
    {
        public const string ProjectName = nameof(ProjectName);
        public const string Kind = nameof(Kind);
    }

    namespace Columns
    {
        [Export(typeof(IDefaultColumnGroup))]
        [Name(nameof(ProjectNameGroupingSet))]
        [GroupColumns(ColumnDefinitions.ProjectName)]
        internal class ProjectNameGroupingSet : IDefaultColumnGroup
        {
            [ImportingConstructor]
            public ProjectNameGroupingSet()
            {
            }
        }

        [Export(typeof(ITableColumnDefinition))]
        [Name(ColumnDefinitions.ProjectName)]
        internal class WhitespaceCategoryColumnDefinition : TableColumnDefinitionBase
        {
            [ImportingConstructor]
            public WhitespaceCategoryColumnDefinition()
            {
            }

            public override string Name => ColumnDefinitions.ProjectName;
            public override string DisplayName => "Project";
            public override double MinWidth => 80;
            public override bool DefaultVisible => false;
            public override bool IsFilterable => true;
            public override bool IsSortable => true;
            public override TextWrapping TextWrapping => TextWrapping.NoWrap;

            private static string GetCategoryName(ITableEntryHandle entry)
            {
                if (entry.TryGetValue(ColumnDefinitions.ProjectName, out string categoryName))
                {
                    return categoryName;
                }
                else
                {
                    return null;
                }
            }

            public override IEntryBucket CreateBucketForEntry(ITableEntryHandle entry)
            {
                string categoryName = GetCategoryName(entry);
                return categoryName is not null ? new StringEntryBucket(GetCategoryName(entry)) : null;
            }
        }

        [Export(typeof(ITableColumnDefinition))]
        [Name(ColumnDefinitions.Kind)]
        internal class KindColumnDefinition : TableColumnDefinitionBase
        {
            [ImportingConstructor]
            public KindColumnDefinition()
            {
            }

            public override string Name => ColumnDefinitions.Kind;
            public override string DisplayName => "Kind";
            public override double MinWidth => 350;
            public override bool DefaultVisible => true;
            public override bool IsFilterable => true;
            public override bool IsSortable => true;
        }

    }

    internal class DemoTableEntriesSnapshot : WpfTableEntriesSnapshotBase
    {
        private readonly ImmutableArray<Project> _data;
        private readonly int _currentVersionNumber;

        public DemoTableEntriesSnapshot(ImmutableArray<Project> data, int currentVersionNumber)
        {
            _data = data;
            _currentVersionNumber = currentVersionNumber;
        }

        public override int VersionNumber => _currentVersionNumber;

        public override int Count => _data.Length;

        public override bool TryGetValue(int index, string keyName, out object content)
        {
            Project result;
            try
            {
                if (index < 0 || index > _data.Length)
                {
                    content = null;
                    return false;
                }

                result = _data[index];

                if (result == null)
                {
                    content = null;
                    return false;
                }
            }
            catch (Exception)
            {
                content = null;
                return false;
            }

            return TryGetValue(result, keyName, out content);
        }

        private bool TryGetValue(Project result, string keyName, out object content)
        {
            content = keyName switch
            {
                ColumnDefinitions.ProjectName => result.Name,
                ColumnDefinitions.Kind => Path.GetExtension(result.FullPath) switch
                {
                    ".csproj" => "C#",
                    ".vbproj" => "VB",
                    ".fsproj" => "F#",
                    _ => "?"
                },
                _ => null,
            };

            return content is not null;
        }
    }

}
