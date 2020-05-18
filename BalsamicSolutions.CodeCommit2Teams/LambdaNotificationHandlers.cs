//  -----------------------------------------------------------------------------
//   Copyright  (c) Balsamic Solutions, LLC. All rights reserved.
//   THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF  ANY KIND, EITHER
//   EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR
//  -----------------------------------------------------------------------------
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SNSEvents;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BalsamicSolutions.CodeCommit2Teams
{
    /// <summary>
    /// handles notifications from CodeCommit triggers and from CodeCommit
    /// notifications.
    ///
    /// Teams configuration for this class relies on an enviornment variable
    /// named TeamsChannelUrl  that contains the url to the Teams Channel.  The
    /// Teams WebHook must be created  as described here 
    /// https://docs.microsoft.com/en-us/microsoftteams/platform/webhooks-and-connectors/how-to/add-incoming-webhook
    /// enabled to post messages to your channel. We do not pass any images
    /// with this app, so it needs it's own logo
    ///
    /// Optionally you can set environment variables for TimeZone and for 
    ///for MaxChangesWarningThreshold to customize the behaviour. The 
    ///TimeZone variable is in Windows format (e.g.  "Eastern Standard Time"
    ///or "UTC-11"). MaxChangesWarningThreshold is an integer value, set
    ///it to 0 to disable max change warnings
    ///
    /// Triggers are configured on CodeCommit individually (because they do not
    /// identify the trigger type (push, new branch, delete branch) with custom
    /// data that defines each one, e.g all triggers point to this Lambda function, but
    /// push with thier custom data.
    ///
    /// Pull request notifications are handled by first creating a Pull request notifcation
    /// on the console, then modifying the subsequent CloudWatch rule to send the
    /// data to this Lambda instead of to SNS.
    /// </summary>
    public class LambdaNotificationHandlers
    {
        private readonly bool _Enabled = false;
        private readonly int _MaxChangesWarningThreshold = 100;
        private readonly Uri _TeamsChannelUrl = null;
        private readonly TimeZoneInfo _TimeZone;
        private AWSCredentials _AwsCredentials = null;

        #region CTORs

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public LambdaNotificationHandlers()
        {
            _AwsCredentials = FallbackCredentialsFactory.GetCredentials();
            //first see if there is a time zone specified as regular timezones id's 
            _TimeZone = TimeZoneInfo.Utc;
            string tempVal = Environment.GetEnvironmentVariable("TimeZone");
            if (!string.IsNullOrWhiteSpace(tempVal))
            {
                tempVal = tempVal.Trim();
                try
                {
                    _TimeZone = tempVal.TimeZoneById();
                }
                catch(TimeZoneNotFoundException)
                {
                    _TimeZone = TimeZoneInfo.Utc;
                    Console.WriteLine("TimeZone setting is invalid " + tempVal); 
                }
            }
            Console.WriteLine("TimeZone set to " + _TimeZone.ToString()); ;
            //now see if we have a different Warning Threshold than the default
            tempVal = Environment.GetEnvironmentVariable("MaxChangesWarningThreshold");
            if (!tempVal.IsNullOrWhiteSpace() && int.TryParse(tempVal, out int maxChanges))
            {
                tempVal = tempVal.Trim();
                _MaxChangesWarningThreshold = maxChanges;
                Console.WriteLine("MaxChangesWarningThreshold set to " + _MaxChangesWarningThreshold.ToString());
            }
            //now collect our TeamsChannelUrl
            tempVal = Environment.GetEnvironmentVariable("TeamsChannelUrl");
            if (!tempVal.IsNullOrWhiteSpace())
            {
                tempVal = tempVal.Trim();
                try
                {
                    _TeamsChannelUrl = new Uri(tempVal);
                    Console.WriteLine("TeamsChannelUrl set to " + _TeamsChannelUrl.ToString());
                    if (!(_AwsCredentials is AnonymousAWSCredentials))
                    {
                        _Enabled = true;
                        Console.WriteLine("CodeCommit2Teams  is configured and enabled");
                    }
                    else
                    {
                        Console.WriteLine("Invalid IAM role assigned to this Lambda");
                    }
                }
                catch (UriFormatException)
                {
                    Console.WriteLine("Invalid url found in environment variable TeamsChannelUrl");
                }
            }
            else
            {
                Console.WriteLine("Missing environment variable TeamsChannelUrl");
            }
        }

        /// <summary>
        /// Constructs an instance with a preconfigured client. 
        /// This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="ccClient"></param>
        public LambdaNotificationHandlers(AWSCredentials awsCreds, string teamsChannelUrl)
        {
            //no checking on this one, as we assume this is a test client
            //so if  its broken its your fault
            _AwsCredentials = awsCreds;
            _TeamsChannelUrl = _TeamsChannelUrl = new Uri(teamsChannelUrl); ;
            _Enabled = true;
            _TimeZone = TimeZoneInfo.Local;
        }

        #endregion CTORs

 

        #region Lambda handlers

        /// <summary>
        /// handle a notification for a branch create,delete or push
        ///
        ///Use this name to register with Lambda
        /// BalsamicSolutions.CodeCommit2Teams::BalsamicSolutions.CodeCommit2Teams.LambdaNotificationHandlers::HandleBranchEvent
        /// </summary>
        /// <param name="jsonPull"></param>
        /// <param name="context"></param>
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public void HandleBranchEvent(BranchTypes.CodeCommitBranchEvent input, ILambdaContext context)
        {
            Console.WriteLine("Starting HandleBranchEvent");
            if (_Enabled)
            {
                string commitId = "Unknown CommitID";
                string refBranch = "Unknown Branch";

                BranchTypes.Record ccRecord = input.Records[0];
                BranchTypes.Codecommit commitRecord = ccRecord.Codecommit;
                //I have only seen multiple references nodes in the test data
                //so we only process record [0]
                if (null != commitRecord.References && commitRecord.References.Length > 0)
                {
                    bool isDelete = false;
                    bool isCreated = false;
                    BranchTypes.Reference ccRef = commitRecord.References[0];
                    commitId = ccRef.Commit;
                    refBranch = ccRef.Ref;
                    if (ccRef.Deleted.HasValue)
                    {
                        isDelete = ccRef.Deleted.Value;
                    }
                    else if (ccRef.Created.HasValue)
                    {
                        isCreated = true;
                    }

                    string[] branchParts = refBranch.Split('/');
                    string branchName = branchParts[branchParts.Length - 1];
                    string eventSourceARN = ccRecord.EventSourceArn;
                    string[] eventSourcARNParts = eventSourceARN.Split(':');
                    string repositoryName = eventSourcARNParts[5];
                    string regionName = eventSourcARNParts[3];
                    Amazon.RegionEndpoint regionEndPoint = Amazon.RegionEndpoint.GetBySystemName(regionName);
                    string userIdentityARN = ccRecord.UserIdentityArn;
                    string eventTimeAsText = ccRecord.EventTime;
                    DateTime eventTime = DateTime.Parse(eventTimeAsText).ToUniversalTime(); 
                    DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(eventTime, _TimeZone);

                    using (AmazonCodeCommitClient codeCommit = new AmazonCodeCommitClient(_AwsCredentials, regionEndPoint))
                    {
                        GetRepositoryResponse repositoryInfo = GetRepositoryInfo(codeCommit, repositoryName);
                        GetCommitResponse commitInfo = GetCommitInfo(codeCommit, repositoryName, commitId);
                        string authorName = userIdentityARN.LastSplitElement('/'); 
                        string notificationMessage = "Unknown Lambda Event";
                        if (isCreated)
                        {
                            notificationMessage = GenerateBranchCreateMessage(authorName, localTime, repositoryName, branchName);
                        }
                        else if (isDelete)
                        {
                            notificationMessage = GenerateBranchDeleteMessage(authorName, localTime, repositoryName, branchName);
                        }
                        else
                        {
                            string commitParentId = "HEAD";
                            if (commitInfo.Commit.Parents.Count > 0)
                            {
                                commitParentId = commitInfo.Commit.Parents[0];
                            }
                            List<Difference> commitDifferences = GetCommitDifferences(codeCommit, repositoryName, commitId, commitParentId, _MaxChangesWarningThreshold);
                            if (_MaxChangesWarningThreshold >0 && commitDifferences.Count >= _MaxChangesWarningThreshold)
                            {
                                notificationMessage = GenerateBranchWarningMessage(authorName, commitId, localTime, commitInfo.Commit.Message, repositoryName, branchName);
                            }
                            else
                            {
                                notificationMessage = GenerateBranchCommitMessage(authorName, commitId, localTime, commitInfo.Commit.Message, repositoryName, branchName, commitDifferences);
                            }
                        }
                        ChannelClient teamsClient = new ChannelClient(_TeamsChannelUrl);
                        teamsClient.PostMessage(notificationMessage);
                    }
                }
                else
                {
                    Console.WriteLine("No References node, nothing to process");
                }
            }
            Console.WriteLine("Completed HandleBranchEvent");
        }

        /// <summary>
        /// this is our callback for all of the AWS triggers and notifications. We do not
        /// use the AWS LambdaSerializer because we are processing more than
        /// one type of JSON data packages on the same endpoint.
        ///
        ///Use this name to register with Lambda
        ///BalsamicSolutions.CodeCommit2Teams::BalsamicSolutions.CodeCommit2Teams.LambdaNotificationHandlers::HandleEvent
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        public void HandleEvent(Stream input, ILambdaContext context)
        {
            string jsonAsText = string.Empty;
            Console.WriteLine("Dispatching CodeCommitEvent");
            if (null != input)
            {
                StreamReader streamReader = new StreamReader(input);
                jsonAsText = streamReader.ReadToEnd();
            }
            Console.WriteLine($"CodeCommitEvent: received the following JSON: {jsonAsText}");
            if (_Enabled)
            {
                //instead of desesralizing the object, look for the "Records" text which indicates we have a
                //branch notification, its faster than parsing the entire jsonData twice for a typed object
                int indexOfRecords = jsonAsText.IndexOf("\"Records\"", 0, StringComparison.OrdinalIgnoreCase);
                if (-1 == indexOfRecords)
                {
                    PullTypes.CodeCommitPullEvent codeCommitEvent = jsonAsText.FromJson<PullTypes.CodeCommitPullEvent>();
                    HandlePullEvent(codeCommitEvent, context);
                }
                else
                {
                    BranchTypes.CodeCommitBranchEvent codeCommitEvent = jsonAsText.FromJson<BranchTypes.CodeCommitBranchEvent>();
                    HandleBranchEvent(codeCommitEvent, context);
                }
            }
            else
            {
                Console.WriteLine("This Lambda handler requires additional configuration");
            }
            Console.WriteLine("Dispatch of CodeCommitEvent complete");
        }

        /// <summary>
        /// handle a notification for a pull event
        ///
        /// Use this name to register with Lambda
        ///  BalsamicSolutions.CodeCommit2Teams::BalsamicSolutions.CodeCommit2Teams.LambdaNotificationHandlers::HandlePullEvent
        /// </summary>
        /// <param name="codeCommitEvent"></param>
        /// <param name="context"></param>
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public void HandlePullEvent(PullTypes.CodeCommitPullEvent input, ILambdaContext context)
        {
            Console.WriteLine("Starting HandlePullEvent");
            if (_Enabled)
            {
                //for this one we are just going to re use the existing notification body
                string notificationMessage = input.Detail.NotificationBody;
                ChannelClient teamsClient = new ChannelClient(_TeamsChannelUrl);
                teamsClient.PostMessage(notificationMessage);
            }
            Console.WriteLine("Completed HandlePullEvent");
        }

        #endregion Lambda handlers

        #region CodeCommit calls

        //= GetBranchInfo(codeCommit, repositoryName, refBranch);
        /// <summary>
        /// gets the commit information
        /// </summary>
        /// <param name="codeCommit"></param>
        /// <param name="repositoryName"></param>
        /// <param name="branchName"></param>
        /// <returns></returns>
        private GetBranchResponse GetBranchInfo(AmazonCodeCommitClient codeCommit, string repositoryName, string branchName)
        {
            GetBranchRequest branchRequest = new GetBranchRequest()
            {
                RepositoryName = repositoryName,
                BranchName = branchName
            };
            GetBranchResponse returnValue = null;
            try
            {
                returnValue = codeCommit.GetBranchAsync(branchRequest).GetAwaiter().GetResult();
            }
            catch (BranchDoesNotExistException)
            {
                returnValue = null;
            }
            return returnValue;
        }

        /// <summary>
        /// get file differences in a particular commit
        /// </summary>
        /// <param name="codeCommit"></param>
        /// <param name="repositoryName"></param>
        /// <param name="afterCommitSpecifier"></param>
        /// <returns></returns>
        private List<Difference> GetCommitDifferences(IAmazonCodeCommit codeCommit, string repositoryName, string afterCommitSpecifier, string beforeCommitSpecifier, int maxResults)
        {
            List<Difference> returnValue = new List<Difference>();
            GetDifferencesRequest diffRequest;
            GetDifferencesResponse diffResponse;
            string nextToken = String.Empty;
            do
            {
                diffRequest = new GetDifferencesRequest()
                {
                    RepositoryName = repositoryName,
                    AfterCommitSpecifier = afterCommitSpecifier,
                    BeforeCommitSpecifier = beforeCommitSpecifier,
                    MaxResults = maxResults
                };
                if (!nextToken.IsNullOrWhiteSpace())
                {
                    diffRequest.NextToken = nextToken;
                }
                diffResponse = codeCommit.GetDifferencesAsync(diffRequest).GetAwaiter().GetResult();

                returnValue.AddRange(diffResponse.Differences);
                nextToken = diffResponse.NextToken;
            } while (!String.IsNullOrEmpty(nextToken));

            return returnValue;
        }

        /// <summary>
        /// gets the commit information
        /// </summary>
        /// <param name="codeCommit"></param>
        /// <param name="repositoryName"></param>
        /// <param name="commitId"></param>
        /// <returns></returns>
        private GetCommitResponse GetCommitInfo(AmazonCodeCommitClient codeCommit, string repositoryName, string commitId)
        {
            GetCommitRequest commitRequest = new GetCommitRequest()
            {
                RepositoryName = repositoryName,
                CommitId = commitId
            };
            GetCommitResponse returnValue = codeCommit.GetCommitAsync(commitRequest).GetAwaiter().GetResult();
            return returnValue;
        }

        /// <summary>
        /// gets the Repository information
        /// </summary>
        /// <param name="codeCommit"></param>
        /// <param name="repositoryName"></param>
        /// <param name="commitId"></param>
        /// <returns></returns>
        private GetRepositoryResponse GetRepositoryInfo(AmazonCodeCommitClient codeCommit, string repositoryName)
        {
            GetRepositoryRequest repoRequest = new GetRepositoryRequest()
            {
                RepositoryName = repositoryName
            };
            GetRepositoryResponse returnValue = codeCommit.GetRepositoryAsync(repoRequest).GetAwaiter().GetResult();
            return returnValue;
        }

        #endregion CodeCommit calls

        #region Message generation

        /// <summary>
        /// generates content for a branch commit notification
        /// </summary>
        /// <param name="authorName"></param>
        /// <param name="commitId"></param>
        /// <param name="commitParentId"></param>
        /// <param name="repositoryInfo"></param>
        /// <param name="commitInfo"></param>
        /// <returns></returns>
        private string GenerateBranchCommitMessage(string authorName, string commitId, DateTime commitTime, string commitMessage, string repoName, string branchName, List<Difference> commitDifferences)
        {
            string returnValue = authorName + " pushed changes to  the " + branchName + " branch of " + repoName;
            returnValue += " at " + commitTime.ToString() + " \r\n";
            returnValue += "*" + commitId + "* : " + commitMessage + " \r\n";
            foreach (Difference commitDiff in commitDifferences)
            {
                string changeType = commitDiff.ChangeType.ToString();
                switch (changeType)
                {
                    case "A":
                        returnValue += "*Added* " + commitDiff.AfterBlob.Path.ToString();
                        break;

                    case "D":
                        returnValue += "*Deleted* " + commitDiff.BeforeBlob.Path.ToString();
                        break;

                    case "M":
                        returnValue += "*Modified* " + commitDiff.AfterBlob.Path.ToString();
                        break;
                }
                returnValue += "\r\n";
            }
            return returnValue;
        }

        /// <summary>
        /// generates content for a new branch notification
        /// </summary>
        /// <returns></returns>
        private string GenerateBranchCreateMessage(string authorName, DateTime commitTime, string repoName, string branchName)
        {
            string returnValue = authorName + " *created* a new branch named " + branchName + " in " + repoName;
            returnValue += " at " + commitTime.ToString("F") + " \r\n";
            return returnValue;
        }

        /// <summary>
        /// generates content for a new branch notification
        /// </summary>
        /// <returns></returns>
        private string GenerateBranchDeleteMessage(string authorName, DateTime commitTime, string repoName, string branchName)
        {
            string returnValue = authorName + " *deleted* the branch " + branchName + " from " + repoName;
            returnValue += " at " + commitTime.ToString("F") + " \r\n";
            return returnValue;
        }

        /// <summary>
        /// generates contentent for a warning message
        /// </summary>
        /// <returns></returns>
        private string GenerateBranchWarningMessage(string authorName, string commitId, DateTime commitTime, string commitMessage, string repoName, string branchName)
        {
            string returnValue = "## Warning \r\n"; ;
            returnValue += authorName + " pushed changes to  the " + branchName + " branch of " + repoName;
            returnValue += " at " + commitTime.ToString("F") + " \r\n";
            returnValue += "*" + commitId + "* : " + commitMessage + " \r\n";
            returnValue += "*This commit had more changes than expected and should be reviewed*";
            return returnValue;
        }
        #endregion Message generation
    }
}