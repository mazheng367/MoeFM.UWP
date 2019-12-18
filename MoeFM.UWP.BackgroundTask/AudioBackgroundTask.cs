using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using MoeFM.UWP.Common;
using MoeFM.UWP.Entities;
using MoeFM.UWP.Entities.MoeFMEntity;
using MoeFM.UWP.Entities.Response;
using MoeFM.UWP.OAuth;
using Newtonsoft.Json;
// ReSharper disable RedundantCatchClause

namespace MoeFM.UWP.BackgroundTask
{
    public sealed class AudioBackgroundTask : IBackgroundTask
    {
        private readonly ManualResetEventSlim _manualReset = new ManualResetEventSlim(true);
        private readonly Queue<MusicInfo> _musicInfos = new Queue<MusicInfo>();
        private readonly List<MusicInfo> _playlistHistory = new List<MusicInfo>(20);
        private BackgroundTaskDeferral _deferral;
        private SystemMediaTransportControls _transportControls;

        private bool _isWiki, _isFavSub, _shuffle = true, _suspend;
        private string _wikiId, _wikiType;

        private int _hisPosition = 1, _wikiPageIndex = 1, _errorCount;

        private MusicInfo CurrentMusicInfo { get; set; }

        private bool IsForegroundRunning => Convert.ToString(ApplicationData.Current.LocalSettings.Values[AppConst.AppStatusKey]) != "Suspending";

        #region 后台播放器相关设置

        private void InitSystemMedia()
        {
            _transportControls = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
            _transportControls.IsEnabled = true;
            _transportControls.IsPlayEnabled = true;
            _transportControls.IsPauseEnabled = true;
            _transportControls.IsPreviousEnabled = true;
            _transportControls.IsNextEnabled = true;
            _transportControls.ButtonPressed += SystemMediaControls_ButtonPressed;
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }
        
        private void NetworkInformation_NetworkStatusChanged(object sender)
        {
            ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile != null && (profile.IsWwanConnectionProfile || profile.IsWlanConnectionProfile))
            {
                _suspend = false;
                _manualReset.Set();
            }
        }

        private void InitBackgroudMedia()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
            BackgroundMediaPlayer.Current.MediaEnded += Current_MediaEnded;
            BackgroundMediaPlayer.Current.MediaFailed += Current_MediaEnded;
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;
        }

        private void Current_MediaEnded(MediaPlayer sender, object args)
        {
            if (_hisPosition > 0)
            {
                PlayNext();
                return;
            }
            PlayItem();
        }

        private void SystemMediaControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Next:
                    if (_hisPosition > 1) PlayNext();
                    else PlayItem();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    PlayPrev();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Pause();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    Resume();
                    break;
            }
        }

        private void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
            else if (sender.CurrentState == MediaPlayerState.Paused)
            {
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
            else if (sender.CurrentState == MediaPlayerState.Closed)
            {
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
            }
        }

        private async void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            var transfer = EntityHelper.ParseMoeMessage(e.Data);
            if (transfer == null) return;

            _suspend = false;
            _manualReset.Set();
            if (transfer.Command == MessageType.SetPosition)
            {
                PlayAtPosition(transfer.Position);
            }
            else if (transfer.Command == MessageType.PlayNext)
            {
                if (_hisPosition > 1) PlayNext();
                else PlayItem();
            }
            else if (transfer.Command == MessageType.PlayPrev)
            {
                PlayPrev();
            }
            else if (transfer.Command == MessageType.MediaPlay)
            {
                BackgroundMediaPlayer.Current.Play();
            }
            else if (transfer.Command == MessageType.MediaPause)
            {
                BackgroundMediaPlayer.Current.Pause();
            }
            else if (transfer.Command == MessageType.RefreshMusicUI)
            {
                SendMusicInfoToForeground(CurrentMusicInfo);
            }
            else if (transfer.Command == MessageType.Shuffle)
            {
                _shuffle = true;
            }
            else if (transfer.Command == MessageType.RepeatOne)
            {
                _shuffle = false;
            }
            else if (transfer.Command == MessageType.PlayWiki)
            {
                PlayWiki(transfer.DataId, transfer.WikiType);
            }
            else if (transfer.Command == MessageType.PlayWikiItem)
            {
                PlayWikiItem(transfer.DataId);
            }
            else if (transfer.Command == MessageType.PlayFavSub)
            {
                await PlayFavSub();
            }
            else if (transfer.Command == MessageType.CancelTask)
            {
                _deferral?.Complete();
            }
        }

        #endregion

        #region 任务启动方法

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            InitSystemMedia();
            InitBackgroudMedia();
            taskInstance.Task.Completed += Task_Completed;
            taskInstance.Canceled += TaskInstance_Canceled;
            //发送消息，后台播放器已经生成完成
            SendMessageToForeground(EntityHelper.GetTransfer(MessageType.BackgroundInited));
            Task.Run(async () =>
            {
                await Task.Delay(200);
                await LoadPlayList();
            });
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            try
            {
                BackgroundMediaPlayer.SendMessageToForeground(EntityHelper.GetTransfer(MessageType.CancelTask));
                _musicInfos.Clear();
                _playlistHistory.Clear();

                BackgroundMediaPlayer.MessageReceivedFromForeground -= BackgroundMediaPlayer_MessageReceivedFromForeground;
                BackgroundMediaPlayer.Current.CurrentStateChanged -= Current_CurrentStateChanged;
                BackgroundMediaPlayer.Current.MediaEnded -= Current_MediaEnded;
                BackgroundMediaPlayer.Current.Position = TimeSpan.MaxValue;
                BackgroundMediaPlayer.Current.Pause();

                if (_transportControls != null)
                {
                    UpdateMediaCotnrols();
                    _transportControls.ButtonPressed -= SystemMediaControls_ButtonPressed;
                    _transportControls.IsEnabled = false;
                    _transportControls.IsPlayEnabled = false;
                    _transportControls.IsPauseEnabled = false;
                    _transportControls.IsPreviousEnabled = false;
                    _transportControls.IsNextEnabled = false;
                }
                ApplicationData.Current.LocalSettings.Values.Remove(AppConst.AppStatusKey);
                ApplicationData.Current.LocalSettings.Values.Remove(AppConst.RunningStatusKey);
                NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;

                _deferral.Complete();
            }
            catch (Exception)
            {
                //
            }
            throw new Exception("");
        }

        private void Task_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            try
            {
                BackgroundMediaPlayer.Shutdown();
            }
            catch (Exception)
            {
                //
            }
            finally
            {
                _deferral.Complete();
            }
        }

        private void SendMessageToForeground(ValueSet set)
        {
            if (!IsForegroundRunning) return;
            BackgroundMediaPlayer.SendMessageToForeground(set);
        }

        private void UpdateMediaCotnrols()
        {

            if (CurrentMusicInfo == null)
            {
                _transportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                _transportControls.DisplayUpdater.MusicProperties.Title = string.Empty;
                _transportControls.DisplayUpdater.Update();
                return;
            }
            
            _transportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
            _transportControls.DisplayUpdater.Type = MediaPlaybackType.Music;
            _transportControls.DisplayUpdater.MusicProperties.Title = CurrentMusicInfo.SubTitle;
            _transportControls.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Images/noimage2.jpg"));
            _transportControls.DisplayUpdater.Update();
        }

        #endregion

        #region 播放的一些公共方法

        private async void PlayItem()
        {
            if (_musicInfos.Count == 0)
            {
                await LoadPlayList();
                return;
            }
            if (!_shuffle) //不是随机播放
            {
                CurrentMusicInfo = CurrentMusicInfo ?? _musicInfos.Dequeue();
                PlayMusic();
                return;
            }
            CurrentMusicInfo = _musicInfos.Dequeue();
            PlayMusic();
            SavePlayHistory(); //保存历史纪录
        }

        private void PlayMusic()
        {
            BackgroundMediaPlayer.Current.Source = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri(CurrentMusicInfo.Url)));
            BackgroundMediaPlayer.Current.Play();
            SendMusicInfoToForeground(CurrentMusicInfo);
            UpdateMediaCotnrols();
        }

        private void PlayAtPosition(double position)
        {
            var current = BackgroundMediaPlayer.Current;
            if (current.CurrentState == MediaPlayerState.Closed || current.CurrentState == MediaPlayerState.Stopped)
            {
                return;
            }
            if (position < 0) return;
            current.Pause();
            current.Position = TimeSpan.FromSeconds(position);
            current.Play();
        }

        private void PlayNext()
        {
            if (!_shuffle)
            {
                BackgroundMediaPlayer.Current.Position = TimeSpan.Zero;
                PlayItem();
                return;
            }
            if (_hisPosition == 1)
            {
                PlayItem();
            }
            else
            {
                var position = _playlistHistory.Count - (--_hisPosition);
                if (position >= _playlistHistory.Count)
                {
                    PlayItem();
                }
                else
                {
                    CurrentMusicInfo = _playlistHistory[position];
                    PlayMusic();
                }
            }
            SendMessageToForeground(EntityHelper.GetTransfer(MessageType.PlayNext));
        }

        private void PlayPrev()
        {
            if (!_shuffle)
            {
                BackgroundMediaPlayer.Current.Position = TimeSpan.Zero;
                return;
            }
            if (_playlistHistory.Count == 0)
            {
                BackgroundMediaPlayer.Current.Position = TimeSpan.Zero;
            }
            else
            {
                var position = _playlistHistory.Count - (++_hisPosition);
                if (_hisPosition >= _playlistHistory.Count) _hisPosition = _playlistHistory.Count; //如果超出栈历史，则指向最后一个
                if (position < 0) position = 0;
                CurrentMusicInfo = _playlistHistory[position];
                PlayMusic();
            }
            SendMessageToForeground(EntityHelper.GetTransfer(MessageType.PlayPrev));
        }

        private void Pause()
        {
            BackgroundMediaPlayer.Current.Pause();
            UpdateMediaCotnrols();
        }

        private void Resume()
        {
            BackgroundMediaPlayer.Current.Play();
            UpdateMediaCotnrols();
        }

        private void PlayWikiItem(string subId)
        {
            GetWikiItem(subId);
        }

        private async void PlayWiki(string wikiId, string wikiType)
        {
            _wikiPageIndex = 1;
            _hisPosition = 1;
            _isWiki = true;
            _wikiId = wikiId;
            _wikiType = wikiType;
            await LoadPlayList();
        }

        private async Task PlayFavSub()
        {
            _isFavSub = true;
            _wikiPageIndex = 1;
            _hisPosition = 1;
            await LoadPlayList();
        }

        private void SendMusicInfoToForeground(MusicInfo info)
        {
            SendMessageToForeground(EntityHelper.GetTransfer(MessageType.FillMusicInfo, info));
        }

        #endregion

        #region 播放器填充方法

        private async Task LoadPlayList()
        {
            try
            {
                List<MusicInfo> entities;
                if (_isWiki) //播放专辑
                {
                    entities = await GetWikiData();
                }
                else if (_isFavSub)
                {
                    entities = await GetFavSub();
                }
                else
                {
                    entities = await GetPlaylistEntities("http://moe.fm/listen/playlist?api=json&perpage=30");
                }
                if (entities == null) return;
                entities.ForEach(_musicInfos.Enqueue);
                CurrentMusicInfo = null;
                PlayItem();
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        private async void GetWikiItem(string subId)
        {
            try
            {
                var entities = await GetPlaylistEntities($"http://moe.fm/listen/playlist?api=json&song={subId}");
                if (entities == null || entities.Count == 0)
                {
                    PlayItem();
                    return;
                }
                var tmp = _musicInfos.ToArray();
                _musicInfos.Clear();
                entities.ForEach(_musicInfos.Enqueue);
                foreach (var musicInfo in tmp)
                {
                    _musicInfos.Enqueue(musicInfo);
                }
                CurrentMusicInfo = null;
                PlayItem();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<List<MusicInfo>> GetWikiData()
        {
            _musicInfos.Clear(); //清空现有队列数据，加载专辑数据
            var entities = await GetPlaylistEntities($"http://moe.fm/listen/playlist?api=json&perpage=30&page={_wikiPageIndex}&{_wikiType}={_wikiId}");
            _wikiPageIndex = _wikiPageIndex + 1;
            if (entities == null || entities.Count == 0)
            {
                _isWiki = false;
                return null;
            }
            if (entities.Count < 30)
            {
                _isWiki = false;
            }
            return entities;
        }

        private async Task<List<MusicInfo>> GetFavSub()
        {
            _musicInfos.Clear();
            var musicInfos = await GetPlaylistEntities($"http://moe.fm/listen/playlist?api=json&page={_wikiPageIndex}&perpage=30&fav=song");
            _wikiPageIndex += 1;
            if (musicInfos == null || musicInfos.Count == 0)
            {
                _isFavSub = false;
                return null;
            }
            if (musicInfos.Count < 30) _isFavSub = false;
            return musicInfos;
        }

        private async Task<List<MusicInfo>> GetPlaylistEntities(string uri)
        {
            try
            {
                if (_suspend)
                {
                    _manualReset.Wait(TimeSpan.FromSeconds(30));
                    return new List<MusicInfo>(0);
                }
                var profile = NetworkInformation.GetInternetConnectionProfile();
                if (profile == null || (profile.IsWwanConnectionProfile == false && profile.IsWlanConnectionProfile == false)) //检查网络连接--表示当前网络未连接
                {
                    throw new MoeSuspendException();
                }
                var postUri = new Uri(OAuthHelper.GenerateRequestUrl(uri, true));
                var client = WebRequest.Create(postUri);
                string response;
                using (var sr = new StreamReader((await client.GetResponseAsync()).GetResponseStream()))
                {
                    response = await sr.ReadToEndAsync();
                }
                var moeResponse = JsonHelper.Parse<MoeResponse<MoePlayListEntity>>(response);
                if (moeResponse?.response.playlist == null || moeResponse.response.playlist.Count == 0) return null;
                CheckWikiPlay(moeResponse); //检查是否为专辑播放，设置状态
                var musics = new List<MusicInfo>(moeResponse.response.playlist.Count);
                musics.AddRange(moeResponse.response.playlist.Select(item => new MusicInfo
                {
                    Artist = item.artist,
                    Cover = item.cover,
                    FavSub = item.fav_sub != null,
                    Id = (int) item.sub_id,
                    SubTitle = item.sub_title,
                    Url = item.url,
                    WikiTitle = item.wiki_title
                }));
                _errorCount = 0;
                return musics;
            }
            catch (JsonException)
            {
                if (!CanRetryLoad()) return new List<MusicInfo>(0);
                return await GetPlaylistEntities(uri);
            }
            catch (WebException ex)
            {
                if (!CanRetryLoad()) return new List<MusicInfo>(0);
                if (((HttpWebResponse) ex.Response)?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await OAuthHelper.Logout();
                    return await GetPlaylistEntities(uri);
                }
                return new List<MusicInfo>();
            }
            catch (HttpRequestException)
            {
                return new List<MusicInfo>(0);
            }
            catch (MoeSuspendException)
            {
                _suspend = true;
                _manualReset.Reset();
                return new List<MusicInfo>(0);
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
                return new List<MusicInfo>(0);
            }
        }

        private void CheckWikiPlay(MoeResponse<MoePlayListEntity> moeResponse)
        {
            if (_isWiki || _isFavSub)
            {
                if (moeResponse.response.information.is_target == false)
                {
                    _isWiki = _isFavSub = false;
                }
            }
        }

        private bool CanRetryLoad()
        {
            if (_errorCount > 10)
            {
                _errorCount = 0;
                throw new MoeSuspendException();
            }
            _errorCount++;
            return true;
        }

        #endregion

        #region 播放历史相关方法

        private const string HisFileName = "MusicHistories.his";
        private const int MaxHisCount = 100;

        private async void SavePlayHistory()
        {
            if (CurrentMusicInfo == null) return;

            _playlistHistory.Add(CurrentMusicInfo);
            //如果超过最大限制，则删除前面一半的历史数据
            if (_playlistHistory.Count > MaxHisCount)
            {
                _playlistHistory.RemoveRange(0, MaxHisCount/2);
            }

            //将播放历史保存到文件中
            var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(HisFileName) ?? await ApplicationData.Current.LocalFolder.CreateFileAsync(HisFileName);
            using (var sw = new StreamWriter(await ((StorageFile) file).OpenStreamForWriteAsync()))
            {
                await sw.WriteLineAsync(JsonHelper.Stringify(_playlistHistory));
                await sw.FlushAsync();
            }
        }

        #endregion
    }
}