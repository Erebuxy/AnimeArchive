﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace BangumiArchive.UIModule
{
    /// <summary>
    /// The main Series grid view
    /// </summary>
    public sealed partial class MainView : Page
    {
        public static bool IsFiltered = false;
        public static string CompanyFilter = "";
        public static Review? ReviewFilter;
        public static ObservableCollection<SeriesIndex> StaticIndices;

        private ObservableCollection<SeriesIndex> Indices => StaticIndices;
        private ObservableCollection<ReviewBoolPair> ReviewChecked;

        public MainView()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();

            ReviewChecked = new ObservableCollection<ReviewBoolPair>
                (ReviewHelper.All.Select(cur => new ReviewBoolPair(cur, true)).ToList());

            ResetIndices();
        }

        /// <summary>
        /// Refresh binding on navigated to
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (CompanyFilter.Length > 0 || ReviewFilter != null)
            {
                IsFiltered = true;
                ResetFilter();

                if (CompanyFilter.Length > 0)
                {
                    CompanyTB.Text = CompanyFilter;
                    CompanyFilter = "";
                }

                if (ReviewFilter != null)
                {
                    foreach (ReviewBoolPair r in ReviewChecked) r.Second = false;
                    ReviewChecked[(int)(Review)ReviewFilter].Second = true;
                    ReviewFilter = null;
                }
                FilterSeries();
            }
        }

        /// <summary>
        /// Update the main series list
        /// </summary>
        public void Refresh() => ResetFilter();

        /// <summary>
        /// Reset the index list to default
        /// </summary>
        public void ResetIndices()
        {
            StaticIndices = new ObservableCollection<SeriesIndex>(DataManager.WatchedIdx);
            Bindings.Update();
        }

        /// <summary>
        /// Reset the filter UI to default
        /// </summary>
        private void ResetFilter()
        {
            IsFiltered = false;

            WatchingCB.IsChecked = NWatchedCB.IsChecked = false;
            RankAllCB.IsChecked = true;
            foreach (ReviewBoolPair r in ReviewChecked) r.Second = true;
            CompanyTB.Text = "";

            ResetIndices();
        }

        /// <summary>
        /// Filter Series according to UI setting
        /// </summary>
        private void FilterSeries()
        {
            IsFiltered = true;

            StaticIndices = new ObservableCollection<SeriesIndex>(
                DataManager.WatchedIdx.Where(i => CheckSeries(i.Series)));
            Bindings.Update();
        }

        /// <summary>
        /// Check whether a single Series should be filtered
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        private bool CheckSeries(Series series)
        {
            // Company
            if (CompanyTB.Text != "" &&
                !series.Seasons.Any(cur => cur.Company == CompanyTB.Text))
                return false;

            // Rank
            if (!ReviewChecked[(int)series.Review].Second)
                return false;

            // Is Watching
            if ((bool)WatchingCB.IsChecked &&
                !(bool)series.IsWatchingNullable)
                return false;

            // Never watched
            if ((bool)NWatchedCB.IsChecked &&
                series.Seasons.Any(cur => cur.Time == 0))
                return false;

            return true;
        }

        /// <summary>
        /// Add new series
        /// </summary>
        public static async void AddSeriesClickAsync()
        {
            // Show input dialog to get the new Series name
            var dialog = new InputDialog("Add Series");
            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary) return;

            // Create new Series
            SeriesIndex si = DataManager.Watched.Add(dialog.Text);
            if (!IsFiltered)
            {
                StaticIndices.Add(si);
            }

            MainPage.NavigateDetailView(si);
        }

        /// <summary>
        /// Scroll to the top of the page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollToTopClick(object sender, RoutedEventArgs e)
        {
            if (Indices.Any())
                SeriesGrid.ScrollIntoView(Indices[0]);
        }

        /// <summary>
        /// Toggle the filter split view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeFilterSplitViewClick(object sender, RoutedEventArgs e)
        {
            FilterSplitView.IsPaneOpen = !FilterSplitView.IsPaneOpen;
        }

        /// <summary>
        /// Actions when rank all checkbox is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RankAllClick(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if ((bool)cb.IsChecked)
            {
                foreach (ReviewBoolPair r in ReviewChecked) r.Second = true;
            }
            else
            {
                foreach (ReviewBoolPair r in ReviewChecked) r.Second = false;
            }
        }

        /// <summary>
        /// Actions when rank checkbox is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RankClick(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (!(bool)cb.IsChecked)
            {
                RankAllCB.IsChecked = false;
                return;
            }

            if (ReviewChecked.ToList().All(cur => (bool)cur.Second))
                RankAllCB.IsChecked = true;
        }

        private void SeriesItemClick(object sender, ItemClickEventArgs e) =>
            MainPage.NavigateDetailView((SeriesIndex)e.ClickedItem);
    }

    public class ReviewBoolPair : Pair<Review, bool>
    {
        public ReviewBoolPair(Review f, bool s) : base(f, s)
        {
        }

        public string ReviewStr => ReviewHelper.ToString(First);
    }
}
