# CodeCommit2Slack
This project is an AWS Lambda handler that forwards Code Commit events to a Teams Channel. It provides more 
detail than the standard email subscriber and of course sinces you have the source code
you cna modify it to do anything you want. Compiling and publshing this project requires
that you have the AWS Lambda toolkit for .NET installed. Assuming you are using the 
project code "as is" then the following steps will install it.

*  Compile and package the prjoect with the command dotnet lambda package --configuration "Lambda-Debug" 
*  Create a Teams channel for your account and enable web hooks https://docs.microsoft.com/en-us/microsoftteams/platform/webhooks-and-connectors/how-to/add-incoming-webhook
*  Use the AWS wizard to create a Lambda https://docs.aws.amazon.com/codecommit/latest/userguide/how-to-notify-lambda.html and upload the code package
*  Make sure it triggers for new branch, branch delete and push.
*  On the Lambda, modify the envionrment variable TeamsChannelUrl so that it is set to the url with the embeded token
*  Optionally set the Lambda environment variable MaxChangesWarningThreshold to represent the maximum changes before the notification becomes a warning
*  Make sure your Lambda role has CodeCommit readonly access. 
*  Setup pull notifications for the CodeCommit repository, then edit the notification to remove the SNS and change it to notify the same Lambda

Coming soon: step by step setup guide

