using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TwilioSmsUtils
{
    public class TwilioSmsWatcher
    {
        public TwilioSmsWatcher(
          string accountSid,
          string accountToken,
          string phoneNumber)
        {
            this.accountSid = accountSid;
            this.accountToken = accountToken;
            this.phoneNumber = phoneNumber;
        }
        public async Task<bool> InitialiseAsync(CancellationToken token)
        {
            var messageListUri = MakeTwilioUri(TWILIO_MESSAGE_LIST);

            var firstPage = await this.ReadSingleTwilioSmsPageAsync(messageListUri,
              token);

            if (firstPage != null)
            {
                if (firstPage.SmsList?.Count > 0)
                {
                    this.latestMessageSeenTime = firstPage.SmsList[0].DateCreated;
                }
                this.initialised = true;
            }
            return (this.initialised);
        }
        public async Task PollForNewMessagesAsync(
          TimeSpan pollInterval,
          Action<TwilioSmsDetails> messageHandler,
          CancellationToken cancellationToken)
        {
            this.CheckInitialised();

            while (true)
            {
                await Task.Delay(pollInterval, cancellationToken);

                var uri = MakeTwilioUri(TWILIO_MESSAGE_LIST);

                var updatedLatestMessageSeenTime = this.latestMessageSeenTime;

                while (uri != null)
                {
                    var page = await ReadSingleTwilioSmsPageAsync(uri, cancellationToken);

                    // clear the URI so that we don't loop unless we find a whole page
                    // of new results and a next page to move to.
                    uri = null;

                    if (page != null)
                    {
                        // filter to the messages that are newer than our current
                        // latest message (if we have one)
                        var newMessageList = page.SmsList.Where(
                          sms => IsNewerMessage(sms, this.latestMessageSeenTime)).ToList();

                        foreach (var newMessage in newMessageList)
                        {
                            messageHandler(newMessage);

                            if (IsNewerMessage(newMessage, updatedLatestMessageSeenTime))
                            {
                                updatedLatestMessageSeenTime = newMessage.DateCreated;
                            }
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        // if everything on this page was new, we might have more to do
                        // so see if there's another page.
                        if (newMessageList.Count == page.SmsList.Count)
                        {
                            uri = new Uri(page.NextPageUri);
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                cancellationToken.ThrowIfCancellationRequested();

                this.latestMessageSeenTime = updatedLatestMessageSeenTime;
            }
        }
        bool IsNewerMessage(TwilioSmsDetails sms, DateTimeOffset? currentLatest)
        {
            return (
              !currentLatest.HasValue ||
              sms.DateCreated > currentLatest);
        }
        HttpClient HttpClient
        {
            get
            {
                if (this.httpClient == null)
                {
                    HttpClientHandler handler = new HttpClientHandler()
                    {
                        Credentials = new NetworkCredential(this.accountSid, this.accountToken)
                    };
                    this.httpClient = new HttpClient(handler);
                }
                return (this.httpClient);
            }
        }
        async Task<TwilioMessageListPage> ReadSingleTwilioSmsPageAsync(Uri uri,
          CancellationToken cancellationToken)
        {
            TwilioMessageListPage page = null;

            var response = await this.HttpClient.GetAsync(uri, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    var xElement = XElement.Load(responseStream);

                    var twilioResponse = xElement.DescendantsAndSelf(XML_TWILIO_RESPONSE_NODE);

                    if (twilioResponse != null)
                    {
                        page = new TwilioMessageListPage();
                        var twilioNextPage =
                          twilioResponse.Attributes(XML_TWILIO_NEXT_PAGE_URI_ATTRIBUTE).FirstOrDefault();

                        page.NextPageUri = twilioNextPage?.Value;

                        page.SmsList =
                          twilioResponse.DescendantsAndSelf(XML_TWILIO_SMS_MESSAGE_NODE).Select(
                            xml => TwilioSmsDetails.FromXElement(xml)).ToList();
                    }
                }
            }
            return (page);
        }
        Uri MakeTwilioUri(string path)
        {
            return (new Uri(
              $"{TWILIO_HOST}{this.accountSid}/{path}?To={this.phoneNumber}"));
        }
        void CheckInitialised()
        {
            if (!this.initialised)
            {
                throw new InvalidOperationException("Not initialised");
            }
        }
        static readonly string TWILIO_HOST = "https://api.twilio.com/2010-04-01/Accounts/";
        static readonly string TWILIO_MESSAGE_LIST = "Messages";
        static readonly string XML_TWILIO_RESPONSE_NODE = "TwilioResponse";
        static readonly string XML_TWILIO_NEXT_PAGE_URI_ATTRIBUTE = "nextpageuri";
        static readonly string XML_TWILIO_SMS_MESSAGE_NODE = "Message";

        HttpClient httpClient;
        DateTimeOffset? latestMessageSeenTime;
        string phoneNumber;
        string accountSid;
        string accountToken;
        bool initialised;
    }
}
