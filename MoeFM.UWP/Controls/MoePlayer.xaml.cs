using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using MoeFM.UWP.Common;
using MoeFM.UWP.Entities;
using MoeFM.UWP.ViewModel;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MoeFM.UWP.Controls
{
    public sealed partial class MoePlayer : UserControl
    {
        private DispatcherTimer _processTimer;
        private MusicInfoViewModel Model { get; }
        private bool _canTick = false;
        private bool _suspended = false;
        private MediaPlayer CurrentPlayer
        {
            get
            {
                int retryCount = 2;
                MediaPlayer mp = null;
                while (mp == null && --retryCount >= 0)
                {
                    try
                    {
                        mp = BackgroundMediaPlayer.Current;
                    }
                    catch (Exception ex)
                    {
                        if (ex.HResult == -2147023174)
                        {

                            ResetBackgroundPlayer();
                            InitTimer();
                            InitMediaElementEvents();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                if (mp == null)
                {
                    throw new Exception("Failed to get a MediaPlayer instance.");
                }

                return mp;
            }
        }

        #region 初始化方法

        public MoePlayer()
        {
            this.InitializeComponent();
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                this.InitTimer();
                this.InitMediaElementEvents();
                this.InitPlayerPanelEvents();
                this.InitAppStatus();

                Model = new MusicInfoViewModel();
            }
        }

        private void InitTimer()
        {
            if (_processTimer == null)
            {
                _processTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
            }
            _processTimer.Tick += _processTimer_Tick;
        }

        private void InitMediaElementEvents()
        {
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
            CurrentPlayer.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            CurrentPlayer.Volume = 100;   
        }

        private void InitPlayerPanelEvents()
        {
            PointerEventHandler eventHandler = OnPointerPressed;
            SliderMusic.AddHandler(PointerPressedEvent, eventHandler, true);

            PointerEventHandler pointerreleasedhandler = OnPointerCaptureLost;
            SliderMusic.AddHandler(PointerCaptureLostEvent, pointerreleasedhandler, true);
        }

        private void ResetBackgroundPlayer()
        {
            try
            {
                BackgroundMediaPlayer.Shutdown();
                ApplicationData.Current.LocalSettings.Values[AppConst.AppStatusKey] = "NotRunning";
                ApplicationData.Current.LocalSettings.Values["BackgroundPlayerState"] = false;
                ChangeBarStatus(this.BtnPlay, 1, SIconPlay);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147023174)
                {
                    throw new Exception("Failed to get a MediaPlayer instance.");
                }
                else
                {
                    throw;
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //绑定数据
            this.DataContext = Model;
        }


        #endregion

        #region 应用程序全局事件

        private void InitAppStatus()
        {
            Application.Current.Suspending += Current_Suspending;
            Application.Current.Resuming += Current_Resuming;
            Window.Current.VisibilityChanged += Current_VisibilityChanged;
        }

        private async void Current_Resuming(object sender, object e)
        {
            ApplicationData.Current.LocalSettings.Values[AppConst.AppStatusKey] = "Running";
            this._suspended = false;
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
            CurrentPlayer.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;

            //恢复界面播放器
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _processTimer.Start();
                _processTimer.Tick += _processTimer_Tick;
            });

            //恢复界面播放器
            string state = Convert.ToString(ApplicationData.Current.LocalSettings.Values[AppConst.RunningStatusKey]);
            bool playing = false;
            if (!string.IsNullOrEmpty(state))
            {
                var appState = JsonHelper.Parse<MoeAppState>(state);
                playing = appState?.PlayStatus == MediaPlayerState.Playing.ToString();
            }
            if (playing)
            {
                SendMessageToBackground(EntityHelper.GetTransfer(MessageType.RefreshMusicUI));
            }
        }

        private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            this._suspended = true;
            ApplicationData.Current.LocalSettings.Values[AppConst.AppStatusKey] = "Suspending";
            ApplicationData.Current.LocalSettings.Values[AppConst.RunningStatusKey] = JsonHelper.Stringify(new MoeAppState {PlayStatus = CurrentPlayer.CurrentState.ToString()});
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _processTimer.Stop();
                _processTimer.Tick -= _processTimer_Tick;
            });
            SliderMusic.PointerCaptureLost -= OnPointerCaptureLost;
            SliderMusic.PointerPressed -= OnPointerPressed;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= BackgroundMediaPlayer_MessageReceivedFromBackground;
            CurrentPlayer.CurrentStateChanged -= BackgroundMediaPlayer_CurrentStateChanged;
            deferral.Complete();
        }

        #endregion

        #region 一般方法

        private void PlayItem(bool prev = false)
        {
            MessageType mt = prev ? MessageType.PlayPrev : MessageType.PlayNext;
            SendMessageToBackground(EntityHelper.GetTransfer(mt));
        }

        private void PlayPosition(double postion)
        {
            var message = new ValueSet {{EntityHelper.MediaKey, JsonHelper.Stringify(new MoeMessage {Command = MessageType.SetPosition, Position = postion})}};
            SendMessageToBackground(message);
        }

        /// <summary>
        /// 清空资源
        /// </summary>
        private async void DisposeMedia()
        {
            try
            {
                ApplicationData.Current.LocalSettings.Values.Remove(AppConst.AppStatusKey);
                ApplicationData.Current.LocalSettings.Values.Remove(AppConst.RunningStatusKey);
                ApplicationData.Current.LocalSettings.Values["BackgroundPlayerState"] = false;
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    _processTimer.Stop();
                    _processTimer.Tick -= _processTimer_Tick;
                });
                
                GC.Collect(GC.MaxGeneration);
                CurrentPlayer.CurrentStateChanged -= BackgroundMediaPlayer_CurrentStateChanged;
                BackgroundMediaPlayer.MessageReceivedFromBackground -= BackgroundMediaPlayer_MessageReceivedFromBackground;
            }
            catch (Exception)
            {
                //
            }
        }

        #endregion

        #region 系统播放器事件

        private async void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            switch (sender.CurrentState)
            {
                case MediaPlayerState.Paused:
                case MediaPlayerState.Closed:
                case MediaPlayerState.Stopped:
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (sender.CurrentState != MediaPlayerState.Paused)
                        {
                            SliderMusic.Value = 0;
                            SliderMusic.Maximum = 100;
                        }
                        if (!_processTimer.IsEnabled) _processTimer.Stop();
                    });
                    break;
                case MediaPlayerState.Playing:
                    await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        SliderMusic.Value = CurrentPlayer.Position.TotalSeconds;
                        SliderMusic.Maximum = CurrentPlayer.NaturalDuration.TotalSeconds;
                        _processTimer.Start();
                        ChangeBarStatus(this.BtnPlay, 1, SIconPlay);
                    });
                    break;
            }
        }

        private async void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            var transfer = EntityHelper.ParseMoeMessage(e.Data);
            if (transfer == null) return;
            if (transfer.Command == MessageType.BackgroundInited)
            {
                ApplicationData.Current.LocalSettings.Values["BackgroundPlayerState"] = true;
            }
            else if (transfer.Command == MessageType.FillMusicInfo || transfer.Command == MessageType.RefreshMusicUI)
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Model.UpdateData(transfer.Item);
                    MoeHelper.ShowTileNotify(transfer.Item.SubTitle, transfer.Item.Cover.medium);
                });
            }
            else if (transfer.Command == MessageType.CancelTask)
            {
                this.DisposeMedia();
            }
        }

        private void SendMessageToBackground(ValueSet set)
        {
            if (!MoeOp.BackgroundPlayerInited || set == null) return;
            BackgroundMediaPlayer.SendMessageToBackground(set);
        }

        #endregion

        #region 播放器UI相关事件

        private void PlayerFunc_OnClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            switch (btn.CommandParameter?.ToString())
            {
                case "Pause":
                    SendMessageToBackground(EntityHelper.GetTransfer(MessageType.MediaPause));
                    ChangeBarStatus(btn, 2, SIconPlay);
                    break;
                case "Next":
                    PlayItem();
                    break;
                case "Previous":
                    PlayItem(true);
                    break;
                case "Play":
                    SendMessageToBackground(EntityHelper.GetTransfer(MessageType.MediaPlay));
                    ChangeBarStatus(btn, 1, SIconPlay);
                    break;
                case "Shuffle":
                    ChangeBarStatus(btn, 4, SIconMethod);
                    SendMessageToBackground(EntityHelper.GetTransfer(MessageType.RepeatOne));
                    break;
                case "RepeatOne":
                    ChangeBarStatus(btn, 3, SIconMethod);
                    SendMessageToBackground(EntityHelper.GetTransfer(MessageType.Shuffle));
                    break;
            }
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _processTimer.Stop();
            e.Handled = true;
        }

        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (MoeOp.BackgroundPlayerInited)
            {
                PlayPosition(SliderMusic.Value);
                CurrentPlayer.Position = TimeSpan.FromSeconds(SliderMusic.Value);
                _processTimer.Start();
            }
            e.Handled = true;
        }

        private void SliderVol_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CurrentPlayer.Volume = this.sliderVol.Value/100;
        }

        private void _processTimer_Tick(object sender, object e)
        {
            try
            {
                if (!_canTick && !_suspended) return;
                SliderMusic.Value = CurrentPlayer.Position.TotalSeconds;
                var span = TimeSpan.FromSeconds(CurrentPlayer.NaturalDuration.TotalSeconds - CurrentPlayer.Position.TotalSeconds);
                LblTime.Text = string.Format("{0:D2}:{1:D2}", span.Hours*60 + span.Minutes, span.Seconds);
            }
            catch (Exception)
            {
                ;
            }
        }

        private void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            _canTick = e.Visible;
            try
            {
                if (e.Visible)
                {
                    var currentPlayer = CurrentPlayer; //单纯获取下播放器，重置下后台播放器    
                }
            }
            catch (Exception)
            {
                //
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="btn"></param>
        /// <param name="status"></param>
        /// <param name="icon"></param>
        private void ChangeBarStatus(Button btn, int status, SymbolIcon icon)
        {
            if (status == 1) //暂停
            {
                icon.Symbol = Symbol.Pause;
                btn.CommandParameter = "Pause";
            }
            else if (status == 2) //播放
            {
                icon.Symbol = Symbol.Play;
                btn.CommandParameter = "Play";
            }
            else if (status == 3) //随机
            {
                icon.Symbol = Symbol.Shuffle;
                btn.CommandParameter = "Shuffle";
            }
            else if (status == 4) //单曲循环
            {
                icon.Symbol = Symbol.RepeatOne;
                btn.CommandParameter = "RepeatOne";
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Model.SubFav)
            {
                await MoeOp.UnLike(Model.Id.ToString(), FavType.Song);
            }
            else
            {
                await MoeOp.Like(Model.Id.ToString(), FavType.Song);
            }
            Model.SubFav = Model.SubFav;
        }

        private void sliderVol_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
            if (delta > 0)
            {
                this.sliderVol.Value += 2;
            }
            else
            {
                this.sliderVol.Value -= 2;
            }

        }

        #endregion
    }
}