/*
 * Cloud Backup 2022 .NET Edition - Sample Project
 *
 * This sample project demonstrates the usage of Cloud Backup in a 
 * simple, straightforward way. It is not intended to be a complete 
 * application. Error handling and other checks are simplified for clarity.
 *
 * www.nsoftware.com/cloudbackup
 *
 * This code is subject to the terms and conditions specified in the 
 * corresponding product license agreement which outlines the authorized 
 * usage and restrictions.
 * 
 */

using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using nsoftware.async.CloudBackup;

class office365backupDemo
{
  private static Office365backup office365backup1;
  private static List<string> finishedMessageIds;
  private static bool backupCompleted;
  private static int messagesDeleted;

  private static void office365backup1_OnMessageError(object sender, Office365backupMessageErrorEventArgs e) {
    if (e.Retry) {
      // By default, we will retry 5 times
      Console.WriteLine("Error backing up message, retrying: " + e.ErrorCode + ": " + e.ErrorMessage);
    } else {
      Console.WriteLine("Error backing up message, skipping: " + e.ErrorCode + ": " + e.ErrorMessage);
    }
  }

  private static void office365backup1_OnBeforeMessageBackup(object sender, Office365backupBeforeMessageBackupEventArgs e) {
    if (e.Skip) {
      Console.WriteLine("Message exists locally, skipping: " + e.BackupFile);
    }
  }

  private static void office365backup1_OnAfterMessageBackup(object sender, Office365backupAfterMessageBackupEventArgs e) {
    lock (finishedMessageIds) {
      finishedMessageIds.Add(e.Id);
      Console.WriteLine("Message backed up successfully. Progress: " + finishedMessageIds.Count + "/" + office365backup1.MessageCount);
    }
  }

  private static void office365backup1_OnMessageDelete(object sender, Office365backupMessageDeleteEventArgs e) {
    messagesDeleted++;
    Console.WriteLine("Message not present remotely, deleting local file: " + e.BackupFile);
  }

  private static void office365backup1_OnEndBackup(object sender, Office365backupEndBackupEventArgs e) {
    backupCompleted = true;
    Console.WriteLine("Backup Completed");
    Console.WriteLine("Messages backed up: " + finishedMessageIds.Count);
    Console.WriteLine("Messages skipped: " + (office365backup1.MessageCount - finishedMessageIds.Count));
    if (office365backup1.SyncDeletes) {
      Console.WriteLine("Messages deleted: " + messagesDeleted);
    }
  }

  private static void office365backup1_OnLog(object sender, Office365backupLogEventArgs e) {
    Console.WriteLine(e.Message);
  }

  static async Task Main(string[] args)
  {
    if (args.Length < 6) {
      Console.WriteLine("\nIncorrect arguments specified.");
      Console.WriteLine("\nusage: office365backup /id client_id /secret client_secret /p path [/f filter] [/s YYYY/mm/DD] [/e YYYY/mm/DD] [/c 1] [/d]\n");
      Console.WriteLine("id:             OAuth Client ID associated with the registered application (required)");
      Console.WriteLine("secret:         OAuth Client Secret associated with the registered application (required)");
      Console.WriteLine("p:              Directory to save messages to (required)");
      Console.WriteLine("f:              Filter applied when retrieving messages (optional)");
      Console.WriteLine("s:              Upper limit of date range when retrieving messages (optional)");
      Console.WriteLine("e:              Lower limit of date range when retrieving messages (optional)");
      Console.WriteLine("c:              Number of simultaneous connections used (optional)");
      Console.WriteLine("d:              Whether the component will sync messages deleted remotely (optional)");
      Console.WriteLine("\nExample: office365backup /id client_id /secret client_secret /p ../../test_folder /f \"parentFolderId eq 'Inbox'\" /s 2023/09/01 /e 2023/09/15 /c 5 /d\n");
      Console.WriteLine("Example: office365backup /id client_id /secret client_secret /p ../../test_folder\n");
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    } else {
      try {
        office365backup1 = new Office365backup();
        finishedMessageIds = new List<string>();
        backupCompleted = false;
        messagesDeleted = 0;

        office365backup1.OnLog += office365backup1_OnLog;
        office365backup1.OnMessageError += office365backup1_OnMessageError;
        office365backup1.OnBeforeMessageBackup += office365backup1_OnBeforeMessageBackup;
        office365backup1.OnAfterMessageBackup += office365backup1_OnAfterMessageBackup;
        office365backup1.OnMessageDelete += office365backup1_OnMessageDelete;
        office365backup1.OnEndBackup += office365backup1_OnEndBackup;

        // Parse command line arguments
        Dictionary<string, string> myArgs = ConsoleDemo.ParseArgs(args);

        // Set all command line arguments
        if (myArgs.TryGetValue("p", out string temp)) office365backup1.DataFolder = temp;
        if (myArgs.TryGetValue("id", out temp)) office365backup1.OAuth.ClientId = temp;
        if (myArgs.TryGetValue("secret", out temp)) office365backup1.OAuth.ClientSecret = temp;
        if (myArgs.TryGetValue("f", out temp)) office365backup1.Filter = temp;
        if (myArgs.TryGetValue("s", out temp)) office365backup1.StartDate = temp;
        if (myArgs.TryGetValue("e", out temp)) office365backup1.EndDate = temp;
        if (myArgs.TryGetValue("c", out temp)) office365backup1.MaxConnections = Int32.Parse(temp);
        if (myArgs.TryGetValue("d", out temp)) office365backup1.SyncDeletes = true;

        Console.WriteLine("This is a basic demo showing how to backup your Office365 mail account.");
        Console.WriteLine("To begin, please press enter to authorize.");
        Console.ReadLine();

        // Get valid OAuth token
        office365backup1.OAuth.ServerAuthURL = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        office365backup1.OAuth.ServerTokenURL = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        office365backup1.OAuth.AuthorizationScope = "offline_access mail.read";
        office365backup1.OAuth.GrantType = OAuthGrantTypes.cogtAuthorizationCode;
        await office365backup1.Authorize();

        // Authorization successful, start backup process
        Console.WriteLine("Authorization Successful.");
        Console.WriteLine("Starting Backup.");
        Console.WriteLine("Retrieving message list (this operation may take some time).");
        await office365backup1.StartBackup();

        while (!backupCompleted) {
          await office365backup1.DoEvents();
        }

      } catch (Exception ex) {
        Console.WriteLine("Error encountered: " + ex.Message);
      }
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    }
  }
}


class ConsoleDemo
{
  public static Dictionary<string, string> ParseArgs(string[] args)
  {
    Dictionary<string, string> dict = new Dictionary<string, string>();

    for (int i = 0; i < args.Length; i++)
    {
      // If it starts with a "/" check the next argument.
      // If the next argument does NOT start with a "/" then this is paired, and the next argument is the value.
      // Otherwise, the next argument starts with a "/" and the current argument is a switch.

      // If it doesn't start with a "/" then it's not paired and we assume it's a standalone argument.

      if (args[i].StartsWith("/"))
      {
        // Either a paired argument or a switch.
        if (i + 1 < args.Length && !args[i + 1].StartsWith("/"))
        {
          // Paired argument.
          dict.Add(args[i].TrimStart('/'), args[i + 1]);
          // Skip the value in the next iteration.
          i++;
        }
        else
        {
          // Switch, no value.
          dict.Add(args[i].TrimStart('/'), "");
        }
      }
      else
      {
        // Standalone argument. The argument is the value, use the index as a key.
        dict.Add(i.ToString(), args[i]);
      }
    }
    return dict;
  }

  public static string Prompt(string prompt, string defaultVal)
  {
    Console.Write(prompt + (defaultVal.Length > 0 ? " [" + defaultVal + "]": "") + ": ");
    string val = Console.ReadLine();
    if (val.Length == 0) val = defaultVal;
    return val;
  }
}