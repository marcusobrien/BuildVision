﻿using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

using AlekseyNagovitsyn.BuildVision.Helpers;
using AlekseyNagovitsyn.BuildVision.Tool.DataGrid;
using AlekseyNagovitsyn.BuildVision.Tool.Models;
using AlekseyNagovitsyn.BuildVision.Tool.ViewModels;
using AlekseyNagovitsyn.BuildVision.Tool.Views.Converters;

namespace AlekseyNagovitsyn.BuildVision.Tool.Views
{
    /// <summary>
    /// Interaction logic for ControlView.xaml
    /// </summary>
    public partial class ControlView : UserControl
    {
        private ControlViewModel _viewModel;

        public ControlView()
        {
            InitializeComponent();

            // Ensure the current culture passed into bindings is the OS culture.
            // By default, WPF uses en-US as the culture, regardless of the system settings.
            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);

            Grid.TargetUpdated += GridOnTargetUpdated;
        }

        private void GridOnTargetUpdated(object sender, DataTransferEventArgs e)
        {
            if (e.Property.Name == "ItemsSource")
                RefreshSortDirectionInGrid();
        }

        private void RefreshSortDirectionInGrid()
        {
            DataGridColumn dataGridColumn = Grid.Columns.FirstOrDefault(col => col.GetBindedProperty() == _viewModel.GridSortDescription.SortPropertyName);
            if (dataGridColumn != null)
                dataGridColumn.SortDirection = _viewModel.GridSortDescription.SortOrder.ToSystem();
        }

        private void GridOnSorting(object sender, DataGridSortingEventArgs e)
        {
            if (_viewModel.GridSorting.CanExecute(e))
                _viewModel.GridSorting.Execute(e);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.Assert(DataContext != null);

            _viewModel = (ControlViewModel)DataContext;
            _viewModel.GridColumnsRef = Grid.Columns;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
            _viewModel.Model.SolutionItem.Projects.CollectionChanged += ProjectsOnCollectionChanged;
        }

        private void ProjectsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ProjectItem item in e.NewItems)
                    item.PropertyChanged += SolutionItemOnProjectPropertyChanged;
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (ProjectItem item in e.OldItems)
                    item.PropertyChanged -= SolutionItemOnProjectPropertyChanged;
            }
        }

        private void SolutionItemOnProjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Grid.ItemsSource == null)
                return;

            if (e.PropertyName == _viewModel.GridSortDescription.SortPropertyName 
                || e.PropertyName == _viewModel.GridGroupPropertyName)
            {
                // Refresh grouping and sorting.
                ((ListCollectionView)Grid.ItemsSource).Refresh();
            }
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_viewModel.CurrentProject != null && e.PropertyName == "CurrentProject")
            {
                // TODO: Remove SelectedIndex = -1 and implement Unselect row feature by clicking on selected row.
                Grid.SelectedIndex = -1;

                if (Grid.SelectedIndex == -1)
                    Grid.ScrollIntoView(_viewModel.CurrentProject);
            }
        }

        private void DataGridExpanderOnExpanded(object sender, RoutedEventArgs e)
        {
            ExpanderIsExpandedConverter.SaveState(
                (Expander)sender,
                false,
                _viewModel.ControlSettings.GridSettings.CollapsedGroups);
            e.Handled = true;
        }

        private void DataGridExpanderOnCollapsed(object sender, RoutedEventArgs e)
        {
            ExpanderIsExpandedConverter.SaveState(
                (Expander)sender,
                true,
                _viewModel.ControlSettings.GridSettings.CollapsedGroups);
            e.Handled = true;
        }

        // Send scrolling to the DataGrid.
        private void DataGridExpanderOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                               {
                                    RoutedEvent = MouseWheelEvent, 
                                    Source = sender
                               };

            var parent = (UIElement)VisualTreeHelper.GetParent((DependencyObject)sender);
            parent.RaiseEvent(eventArg);
        }

        private void DataGridRowDetailsOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // http://stackoverflow.com/questions/6279724/mousedoubleclick-events-dont-bubble/6326181#6326181)
            if (e.ClickCount == 2)
                e.Handled = true;
        }

        // Autofocus for RowDetails (without extra mouse click).
        private void DataGridRowOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = (DataGridRow)sender;
            if (!row.IsSelected && e.Source is DataGridDetailsPresenter)
            {
                row.Focusable = true;
                row.Focus();

                // Gets the element with keyboard focus.
                var elementWithFocus = Keyboard.FocusedElement as UIElement;

                // Change keyboard focus.
                if (elementWithFocus != null)
                {
                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    elementWithFocus.MoveFocus(request);
                }
            }
        }
    }
}