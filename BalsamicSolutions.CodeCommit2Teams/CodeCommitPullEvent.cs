//  -----------------------------------------------------------------------------
//   Copyright  (c) Balsamic Solutions, LLC. All rights reserved.
//   THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF  ANY KIND, EITHER
//   EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR
//  -----------------------------------------------------------------------------
using Newtonsoft.Json;
using System;

/// <summary>
/// typed classes for pull Events 
/// pull events are actuall redirected
/// SNS notifications 
/// </summary>
namespace BalsamicSolutions.CodeCommit2Teams.PullTypes
{
    public partial class CodeCommitPullEvent
    {
        [JsonProperty("account")]
        public string Account { get; set; }

        [JsonProperty("detail")]
        public Detail Detail { get; set; }

        [JsonProperty("detail-type")]
        public string DetailType { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("resources")]
        public string[] Resources { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("time")]
        public DateTimeOffset Time { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public partial class Detail
    {
        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("callerUserArn")]
        public string CallerUserArn { get; set; }

        [JsonProperty("creationDate")]
        public string CreationDate { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("destinationCommit")]
        public string DestinationCommit { get; set; }

        [JsonProperty("destinationReference")]
        public string DestinationReference { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("isMerged")]
        public string IsMerged { get; set; }

        [JsonProperty("lastModifiedDate")]
        public string LastModifiedDate { get; set; }

        [JsonProperty("notificationBody")]
        public string NotificationBody { get; set; }

        [JsonProperty("pullRequestId")]
        public long PullRequestId { get; set; }

        [JsonProperty("pullRequestStatus")]
        public string PullRequestStatus { get; set; }

        [JsonProperty("repositoryNames")]
        public string[] RepositoryNames { get; set; }

        [JsonProperty("sourceCommit")]
        public string SourceCommit { get; set; }

        [JsonProperty("sourceReference")]
        public string SourceReference { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }
}