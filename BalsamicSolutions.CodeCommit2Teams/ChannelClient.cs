//  -----------------------------------------------------------------------------
//   Copyright  (c) Balsamic Solutions, LLC. All rights reserved.
//   THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF  ANY KIND, EITHER
//   EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR
//  -----------------------------------------------------------------------------
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        private List<Uri> _ChannelsAndTokens = new List<Uri>();

        /// <summary>
        /// create a client to a channel with embeded token and channel information
        /// </summary>
        /// <param name="channelAndTokenUri"></param>
        public ChannelClient(List<Uri> channelAndTokenUrls)
        {
            _ChannelsAndTokens = new List<Uri>(channelAndTokenUrls);
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
                foreach (Uri channelAndToken in _ChannelsAndTokens)
                {
                    try
                    {
                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, channelAndToken);
                        requestMessage.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        HttpResponseMessage httpResponseMessage = webClient.SendAsync(requestMessage).Result;
                        string responseText = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        System.Diagnostics.Trace.WriteLine(responseText);
                        if (responseText.Contains("Microsoft Teams endpoint returned HTTP error 429", StringComparison.OrdinalIgnoreCase))
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
}