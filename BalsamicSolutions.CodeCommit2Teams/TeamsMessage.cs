//  -----------------------------------------------------------------------------
//   Copyright  (c) Balsamic Solutions, LLC. All rights reserved.
//   THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF  ANY KIND, EITHER
//   EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR
//  -----------------------------------------------------------------------------
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BalsamicSolutions.CodeCommit2Teams
{    /// <summary>
     /// Minimal message formatting needed to post to a teams channel
     /// We could send a full teams card but we are not going to do 
     /// that now
     /// Invoke-RestMethod -Method post -ContentType 'Application/Json' -Body '{"text":"Hello World!"}' -Uri <YOUR WEBHOOK URL>
     /// </summary>
    public class TeamsMessage
    {

        [JsonProperty("text")]
        public string Text { get; set; }

    }
}