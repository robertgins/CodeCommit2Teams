This project is an AWS Lambda handler that forwards Code Commit events to a Teams Channel. It provides more detail than the standard email subscriber and of course since you have the source code, you can modify it to do anything you want. Compiling and publishing this project requires that you have the AWS Lambda toolkit for .NET installed. Assuming you are using the project code &quot;as is&quot; then the following steps will install it.

1. Compile and package the project with the command **dotnet lambda package --configuration &quot;Lambda-Debug&quot;** (you run this from the command prompt in the root of the application directory). It will create a zip file named **BalsamicSolutions.CodeCommit2Teams.zip** in the directory **..\BalsamicSolutions.CodeCommit2Teams\bin\Lambda-Debug\netcoreapp2.1** This is the file you will upload to Lambda.
2. Create a Teams channel for your account and enable web hooks [https://docs.microsoft.com/en-us/microsoftteams/platform/webhooks-and-connectors/how-to/add-incoming-webhook](https://docs.microsoft.com/en-us/microsoftteams/platform/webhooks-and-connectors/how-to/add-incoming-webhook) make sure to retain the web hook url for configuration of the lambda handler.
3. Following these directions [https://docs.aws.amazon.com/codecommit/latest/userguide/how-to-notify-lambda.html](https://docs.aws.amazon.com/codecommit/latest/userguide/how-to-notify-lambda.html) create a new Lambda handler for code commit. Select one repository you want to notify on. Be sure to select all triggers for the new branch. When configuring the lambda upload the zip file from step one and set the Lambda hander name to   **BalsamicSolutions.CodeCommit2Teams::BalsamicSolutions.CodeCommit2Teams.LambdaNotificationHandlers::HandleBranchEvent**
4. Edit the lambda configuration and set environment variables as follows:
  1. **TeamsChannelUrl s** et to the web hook url from step 2
  2. **MaxChangesWarningThreshold** set to 50 or some other number as you see fit
  3. **TimeZone** set to the name of the time zone you want to use for notification date information. For example **Eastern Standard Time** , you can get all the time zone names from the **timeZones.json** file in the project.
5. Edit the IAM role that was created by the Lambda wizard in step 3, add the **AWSCodeCommitReadOnly** permission policy to the role. This will allow the lambda handler to look up details about commits in CodeCommit
6. If you also want notifications on pull requests and new branch/tag creations then you will need to setup notification rules on the actual repository.
  1.  From the AWS Console navigate to CodeCommit select the repository you are monitoring and select **Manage Notification Rules** from the **Notify menu**. Enable notifications for your repository (select all notifications) and create a new SNS topic.
  2. In the AWS console navigate to SNS and edit the new SNS topic by creating a subscription to the Lambda Handler

Your done, you will now receive detailed information on all of the events that happen in the CodeCommit repository. You can of course update the messages however you like. We plan on publishing a future version of this that will create Action cards to approve pull requests in the Teams message channel.

Enjoy and please send us feedback
