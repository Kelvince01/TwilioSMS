using System;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using TwilioSmsUtils;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TwilioSMS
{
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<TwilioSmsDetails> NewSmsMessages
        {
            get;
            private set;
        }
        public MainPage()
        {
            this.InitializeComponent();

            this.NewSmsMessages = new ObservableCollection<TwilioSmsDetails>();
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
        private async void OnStartAsync(object sender, RoutedEventArgs e)
        {
            this.NewSmsMessages.Clear();

            this.tokenSource = new CancellationTokenSource();

            this.twilioWatcher = new TwilioSmsWatcher(
              Constants.ACCOUNT_SID,
              Constants.ACCOUNT_TOKEN,
              Constants.PHONE_NUMBER);

            await this.twilioWatcher.InitialiseAsync(this.tokenSource.Token);

            // We don't await this, we let it go.
            try
            {
                await this.twilioWatcher.PollForNewMessagesAsync(
                  TimeSpan.FromSeconds(30),
                  OnNewTwilioMessage,
                  this.tokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                this.twilioWatcher = null;
                this.tokenSource.Dispose();
                this.tokenSource = null;
            }
        }
        void OnNewTwilioMessage(TwilioSmsDetails message)
        {
            this.Dispatcher.RunAsync(
              CoreDispatcherPriority.Normal,
              () =>
              {
                  this.NewSmsMessages.Add(message);
              }
            );
        }
        void OnStopAsync(object sender, RoutedEventArgs e)
        {
            this.tokenSource.Cancel();
        }
        TwilioSmsWatcher twilioWatcher;
        CancellationTokenSource tokenSource;
    }
}
