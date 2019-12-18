using System;
using System.Diagnostics;
using System.Text;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Microsoft.ApplicationInsights;
using MoeFM.UWP.Common;
using MoeFM.UWP.Entities;
using MoeFM.UWP.OAuth;
using MoeFM.UWP.Pages;

namespace MoeFM.UWP
{
    /// <summary>
    ///     提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        ///     初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        ///     已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            WindowsAppInitializer.InitializeAsync(
                WindowsCollectors.Metadata |
                WindowsCollectors.Session);
            InitializeComponent();
            UnhandledException += App_UnhandledException;
        }

        private async void WriteLog(string info)
        {
            try
            {
                var file = await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("ERROR.log") ?? await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("ERROR.log", CreationCollisionOption.OpenIfExists);
                var builder = new StringBuilder();
                builder.AppendLine($"----------------------START LOG Date:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}----------------------");
                builder.AppendLine(info);
                builder.AppendLine("-----------------------END LOG----------------------");
                await FileIO.AppendTextAsync((StorageFile) file, builder.ToString());
            }
            catch
            {
                // ignored
            }
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
#if DEBUG
                var sb = new StringBuilder();
                sb.AppendLine(e.Message);
                var exception = e.Exception;
                while (exception != null)
                {
                    sb.AppendLine(exception.ToString());
                    exception = exception.InnerException;
                }

                WriteLog(sb.ToString());
#endif
                var set = new ValueSet {{EntityHelper.MediaKey, JsonHelper.Stringify(new MoeMessage {Command = MessageType.CancelTask})}};
                MoeOp.SendMessageToBackground(set);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        ///     在应用程序由最终用户正常启动时进行调用。
        ///     将在启动应用程序以打开特定文件等情况下使用。
        /// </summary>
        /// <param name="e">有关启动请求和过程的详细信息。</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            ApplicationData.Current.LocalSettings.Values[AppConst.AppStatusKey] = "Running"; //设置程序运行状态
            await OAuthHelper.GetTokenAsyncFromFileAsync(); //读取登录文件

            var rootFrame = Window.Current.Content as MainPage;

            // 不要在窗口已包含内容时重复应用程序初始化，
            // 只需确保窗口处于活动状态
            if (rootFrame == null)
            {
                // 创建要充当导航上下文的框架，并导航到第一页
                rootFrame = new MainPage();

                rootFrame.AppFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: 从之前挂起的应用程序加载状态
                }

                // 将框架放在当前窗口中
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.AppFrame.Content == null)
            {
                // 当导航堆栈尚未还原时，导航到第一页，
                // 并通过将所需信息作为导航参数传入来配置
                // 参数
                if (!string.IsNullOrEmpty(e.Arguments)) //从辅助磁贴点击进入
                {
                    var info = JsonHelper.Parse<ClickInfo>(e.Arguments);
                    rootFrame.AppFrame.Navigate(typeof (WikiDetailPage), info);
                }
                else
                {
                    rootFrame.AppFrame.Navigate(typeof (HomePage), e.Arguments);
                }
            }
            // 确保当前窗口处于活动状态
            Window.Current.Activate();
            Window.Current.Closed += Current_Closed;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.BackgroundColor = Color.FromArgb(255, 12, 142, 246);
            titleBar.ButtonBackgroundColor = Color.FromArgb(255, 12, 142, 246);
        }

        private void Current_Closed(object sender, CoreWindowEventArgs e)
        {
            try
            {
                //清理一些临时数据
                ApplicationData.Current.LocalSettings.Values.Remove(AppConst.AppStatusKey);
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        ///     导航到特定页失败时调用
        /// </summary>
        /// <param name="sender">导航失败的框架</param>
        /// <param name="e">有关导航失败的详细信息</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        ///     注册后台播放任务
        /// </summary>
        public void RegistAudioTask()
        {
        }
    }
}