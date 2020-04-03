//  -----------------------------------------------------------------------------
//   Copyright  (c) Balsamic Solutions, LLC. All rights reserved.
//   THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF  ANY KIND, EITHER
//   EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR
//  -----------------------------------------------------------------------------
using Newtonsoft.Json;
using System;

/// <summary>
/// typed classes for Branch Events
/// </summary>
namespace BalsamicSolutions.CodeCommit2Teams.BranchTypes
{
    public partial class Codecommit
    {
        [JsonProperty("references")]
        public Reference[] References { get; set; }
    }

    public partial class CodeCommitBranchEvent
    {
        [JsonProperty("Records")]
        public Record[] Records { get; set; }
    }

    public partial class Record
    {
        [JsonProperty("awsRegion")]
        public string AwsRegion { get; set; }

        [JsonProperty("codecommit")]
        public Codecommit Codecommit { get; set; }

        [JsonProperty("customData")]
        public string CustomData { get; set; }

        [JsonProperty("eventId")]
        public Guid EventId { get; set; }

        [JsonProperty("eventName")]
        public string EventName { get; set; }

        [JsonProperty("eventPartNumber")]
        public long EventPartNumber { get; set; }

        [JsonProperty("eventSource")]
        public string EventSource { get; set; }

        [JsonProperty("eventSourceARN")]
        public string EventSourceArn { get; set; }

        [JsonProperty("eventTime")]
        public string EventTime { get; set; }

        [JsonProperty("eventTotalParts")]
        public long EventTotalParts { get; set; }

        [JsonProperty("eventTriggerConfigId")]
        public Guid EventTriggerConfigId { get; set; }

        [JsonProperty("eventTriggerName")]
        public string EventTriggerName { get; set; }

        [JsonProperty("eventVersion")]
        public string EventVersion { get; set; }

        [JsonProperty("userIdentityARN")]
        public string UserIdentityArn { get; set; }
    }

    public partial class Reference
    {
        [JsonProperty("commit")]
        public string Commit { get; set; }

        [JsonProperty("created")]
        public bool? Created { get; set; }

        [JsonProperty("deleted")]
        public bool? Deleted { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }
    }
}