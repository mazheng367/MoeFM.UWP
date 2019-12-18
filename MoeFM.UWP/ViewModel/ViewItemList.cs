using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;
using MoeFM.UWP.Common;

namespace MoeFM.UWP.ViewModel
{
    internal class ViewItemList<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; } = 30;

        private int _totalCount;

        public int TotalCount
        {
            get { return _totalCount; }
            set
            {
                _totalCount = value;
                base.OnPropertyChanged(new PropertyChangedEventArgs("TotalCount"));
            }
        }

        private bool _busy = false, manualCancel;

        public Func<int, int, Task<List<T>>> LoadItemsFunc { get; private set; }

        public bool HasMoreItems { get; private set; }

        public ViewItemList(Func<int, int, Task<List<T>>> func)
        {
            this.PageIndex = 1;
            this.HasMoreItems = true;
            this.LoadItemsFunc = func;
        }

        public event EventHandler LoadStart;
        public event EventHandler LoadCompleted;

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            if (_busy)
            {
                return Task.Run(() => new LoadMoreItemsResult() {Count = 0}).AsAsyncOperation();
            }
            _busy = true;
            return LoadMoreItems().AsAsyncOperation();
        }

        public void Reset()
        {
            this.ClearItems();
            this.PageIndex = 1;
            this.HasMoreItems = true;
            this.manualCancel = false;
        }

        private async Task<LoadMoreItemsResult> LoadMoreItems()
        {
            List<T> items;
            try
            {
                LoadStart?.Invoke(null, EventArgs.Empty);
                Task<List<T>> task = LoadItemsFunc?.Invoke(PageIndex, PageSize);
                if (task == null)
                {
                    HasMoreItems = false;
                    OnCompleted();
                    return new LoadMoreItemsResult() {Count = 0};
                }
                items = await task;
            }
            catch (ErrorCanContinueException)
            {
                PageIndex = PageIndex + 1;
                HasMoreItems = true;
                OnCompleted();
                return new LoadMoreItemsResult() {Count = 0};
            }

            if (items == null || items.Count == 0)
            {
                HasMoreItems = false;
                OnCompleted();
                return new LoadMoreItemsResult() {Count = 0};
            }

            PageIndex = PageIndex + 1; //页索引+1
            AddItems(items); //添加项到当前集合
            HasMoreItems = true;
            this.TotalCount += items.Count;
            OnCompleted();
            if (manualCancel)
            {
                this.HasMoreItems = false;
            }
            return new LoadMoreItemsResult() {Count = (uint) items.Count};
        }

        public void AddItems(List<T> items)
        {
            items?.ForEach(this.Add);
        }

        public void CompleteLoad()
        {
            manualCancel = true;
        }

        private void OnCompleted()
        {
            _busy = false;
            LoadCompleted?.Invoke(null, EventArgs.Empty);
        }
    }
}
