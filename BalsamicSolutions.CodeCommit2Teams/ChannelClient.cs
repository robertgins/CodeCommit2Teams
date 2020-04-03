//  -----------------------------------------------------------------------------
//   Copyright  (c) Balsamic Solutions, LLC. All rights reserved.
//   THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF  ANY KIND, EITHER
//   EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR
//  -----------------------------------------------------------------------------
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Text;

namespace BalsamicSolutions.CodeCommit2Teams
{
    /// <summary>
    /// A simple C# class to post messages to a Teams Channel
    /// </summary>
    public class ChannelClient
    {
        private readonly Uri _ChannelAndTokenUrl;

        /// <summary>
        /// create a client to a channel with embeded token and channel information
        /// </summary>
        /// <param name="channelAndTokenUrl"></param>
        public ChannelClient(string channelAndTokenUrl)
        {
            _ChannelAndTokenUrl = new Uri(channelAndTokenUrl);
        }

        /// <summary>
        /// create a client to a channel with embeded token and channel information
        /// </summary>
        /// <param name="channelAndTokenUri"></param>
        public ChannelClient(Uri channelAndTokenUri)
        {
            _ChannelAndTokenUrl = channelAndTokenUri;
        }

        /// <summary>
        ///Post a message to the predefined Url
        /// </summary>
        /// <param name="messageText"></param>
        public void PostMessage(string messageText)
        {
            TeamsMessage teamsMessage = new TeamsMessage()
            {
                //Max message length is 1024 characters
                Text = messageText.TrimTo(1024, false)
            };
            PostMessage(teamsMessage);
        }

        /// <summary>
        /// post a message to a channel with embeded token and channel information
        /// </summary>
        /// <param name="teamsMessage"></param>
        public void PostMessage(TeamsMessage teamsMessage)
        {
            string jsonPayload = JsonConvert.SerializeObject(teamsMessage);
            using (HttpClient webClient = new HttpClient())
            {
                try
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, _ChannelAndTokenUrl);
                    requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    HttpResponseMessage httpResponseMessage = webClient.SendAsync(requestMessage).Result;
                    string responseText = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    System.Diagnostics.Trace.WriteLine(responseText);
                    if (responseText.Contains("Microsoft Teams endpoint returned HTTP error 429",StringComparison.OrdinalIgnoreCase))
                    {
                        //TODO retry logic ?
                    }
                }
                catch (Exception postError)
                {
                    Console.WriteLine("Error posting to Teams " + postError.Message);
                }
            }
        }
    }
}