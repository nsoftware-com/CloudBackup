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

class gmailbackupDemo
{
  private static Gmailbackup gmailbackup1;
  private static List<string> finishedMessageIds;
  private static bool backupCompleted;
  private static int messagesDeleted;

  private static void gmailbackup1_OnMessageError(object sender, GmailbackupMessageErrorEventArgs e) {
    if (e.Retry) {
      // By default, we will retry 5 times
      Console.WriteLine("Error backing up message, retrying: " + e.ErrorCode + ": " + e.ErrorMessage);
    } else {
      Console.WriteLine("Error backing up message, skipping: " + e.ErrorCode + ": " + e.ErrorMessage);
    }
  }

  private static void gmailbackup1_OnBeforeMessageBackup(object sender, GmailbackupBeforeMessageBackupEventArgs e) {
    if (e.Skip) {
      Console.WriteLine("Message exists locally, skipping: " + e.BackupFile);
    }
  }

  private static void gmailbackup1_OnAfterMessageBackup(object sender, GmailbackupAfterMessageBackupEventArgs e) {
    lock (finishedMessageIds) {
      finishedMessageIds.Add(e.Id);
      Console.WriteLine("Message backed up successfully. Progress: " + finishedMessageIds.Count + "/" + gmailbackup1.MessageCount);
    }
  }

  private static void gmailbackup1_OnMessageDelete(object sender, GmailbackupMessageDeleteEventArgs e) {
    messagesDeleted++;
    Console.WriteLine("Message not present remotely, deleting local file: " + e.BackupFile);
  }

  private static void gmailbackup1_OnEndBackup(object sender, GmailbackupEndBackupEventArgs e) {
    backupCompleted = true;
    Console.WriteLine("Backup Completed");
    Console.WriteLine("Messages backed up: " + finishedMessageIds.Count);
    Console.WriteLine("Messages skipped: " + (gmailbackup1.MessageCount - finishedMessageIds.Count));
    if (gmailbackup1.SyncDeletes) {
      Console.WriteLine("Messages deleted: " + messagesDeleted);
    }
  }

  private static void gmailbackup1_OnLog(object sender, GmailbackupLogEventArgs e) {
    Console.WriteLine(e.Message);
  }

  static async Task Main(string[] args)
  {
    if (args.Length < 6) {
      Console.WriteLine("\nIncorrect arguments specified.");
      Console.WriteLine("\nusage: gmailbackup /id client_id /secret client_secret /p path [/f filter] [/s YYYY/mm/DD] [/e YYYY/mm/DD] [/c 1] [/d]\n");
      Console.WriteLine("id:             OAuth Client ID associated with the registered application (required)");
      Console.WriteLine("secret:         OAuth Client Secret associated with the registered application (required)");
      Console.WriteLine("p:              Directory to save messages to (required)");
      Console.WriteLine("f:              Filter applied when retrieving messages (optional)");
      Console.WriteLine("s:              Upper limit of date range when retrieving messages (optional)");
      Console.WriteLine("e:              Lower limit of date range when retrieving messages (optional)");
      Console.WriteLine("c:              Number of simultaneous connections used (optional)");
      Console.WriteLine("d:              Whether the component will sync messages deleted remotely (optional)");
      Console.WriteLine("\nExample: gmailbackup /id client_id /secret client_secret /p ../../test_folder /f in:sent /s 2023/09/01 /e 2023/09/15 /c 5 /d\n");
      Console.WriteLine("Example: gmailbackup /id client_id /secret client_secret /p ../../test_folder\n");
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    } else {
      try {
        gmailbackup1 = new Gmailbackup();
        finishedMessageIds = new List<string>();
        backupCompleted = false;
        messagesDeleted = 0;

        gmailbackup1.OnLog += gmailbackup1_OnLog;
        gmailbackup1.OnMessageError += gmailbackup1_OnMessageError;
        gmailbackup1.OnBeforeMessageBackup += gmailbackup1_OnBeforeMessageBackup;
        gmailbackup1.OnAfterMessageBackup += gmailbackup1_OnAfterMessageBackup;
        gmailbackup1.OnMessageDelete += gmailbackup1_OnMessageDelete;
        gmailbackup1.OnEndBackup += gmailbackup1_OnEndBackup;

        // Parse command line arguments
        Dictionary<string, string> myArgs = ConsoleDemo.ParseArgs(args);

        // Set all console arguments
        if (myArgs.TryGetValue("p", out string temp)) gmailbackup1.DataFolder = temp;
        if (myArgs.TryGetValue("id", out temp)) gmailbackup1.OAuth.ClientId = temp;
        if (myArgs.TryGetValue("secret", out temp)) gmailbackup1.OAuth.ClientSecret = temp;
        if (myArgs.TryGetValue("f", out temp)) gmailbackup1.Filter = temp;
        if (myArgs.TryGetValue("s", out temp)) gmailbackup1.StartDate = temp;
        if (myArgs.TryGetValue("e", out temp)) gmailbackup1.EndDate = temp;
        if (myArgs.TryGetValue("c", out temp)) gmailbackup1.MaxConnections = Int32.Parse(temp);
        if (myArgs.TryGetValue("d", out temp)) gmailbackup1.SyncDeletes = true;

        Console.WriteLine("This is a basic demo showing how to backup your Gmail account.");
        Console.WriteLine("To begin, please press enter to authorize.");
        Console.ReadLine();

        // Get valid OAuth token
        gmailbackup1.OAuth.ServerAuthURL = "https://accounts.google.com/o/oauth2/auth";
        gmailbackup1.OAuth.ServerTokenURL = "https://accounts.google.com/o/oauth2/token";
        gmailbackup1.OAuth.AuthorizationScope = "https://www.googleapis.com/auth/gmail.readonly";
        gmailbackup1.OAuth.GrantType = OAuthGrantTypes.cogtAuthorizationCode;
        await gmailbackup1.Authorize();

        // Authorization successful, start backup process
        Console.WriteLine("Authorization Successful.");
        Console.WriteLine("Starting Backup.");
        Console.WriteLine("Retrieving message list (this operation may take some time).");
        await gmailbackup1.StartBackup();

        while (!backupCompleted) {
          await gmailbackup1.DoEvents();
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