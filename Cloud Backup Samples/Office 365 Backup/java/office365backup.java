/*
 * Cloud Backup 2022 Java Edition - Sample Project
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
 */

import java.io.*;
import java.util.*;
import cloudbackup.*;

import static java.lang.Integer.parseInt;

public class office365backup extends ConsoleDemo {
  private static Office365backup office365backup1;
  private static List<String> finishedMessageIds;
  private static boolean backupCompleted;
  private static int messagesDeleted;

  public static void main(String[] args) {
    if (args.length < 6) {
      System.out.println("\nIncorrect arguments specified.");
      System.out.println("\nusage: office365backup -id client_id -secret client_secret -p path [-f filter] [-s YYYY/mm/DD] [-e YYYY/mm/DD] [-c max_connections] [-d]\n");
      System.out.println("-id:           OAuth Client ID associated with the registered application (required)");
      System.out.println("-secret:       OAuth Client Secret associated with the registered application (required)");
      System.out.println("-p:            Directory to save messages to (required)");
      System.out.println("-f:            Filter applied when retrieving messages (optional)");
      System.out.println("-s:            Upper limit of date range when retrieving messages (optional)");
      System.out.println("-e:            Lower limit of date range when retrieving messages (optional)");
      System.out.println("-c:            Number of simultaneous connections used (optional)");
      System.out.println("-d:            Flag to enable whether the component will sync messages deleted remotely (optional)");
      System.out.println("\nExample: office365backup -id client_id -secret client_secret -p ../../test_folder -f \"parentFolderId eq 'Inbox'\" -s 2023/09/01 -e 2023/09/15 -c 5 -d\n");
      System.out.println("Example: office365backup -id client_id -secret client_secret -p ../../test_folder\n");
      System.out.println("Press any key to exit...");
      input();
    } else {
      try {
        office365backup1 = new Office365backup();
        finishedMessageIds = new ArrayList<>();
        backupCompleted = false;
        messagesDeleted = 0;

        office365backup1.addOffice365backupEventListener(new DefaultOffice365backupEventListener() {
          public void afterMessageBackup(Office365backupAfterMessageBackupEvent e) {
            synchronized (finishedMessageIds) {
              finishedMessageIds.add(e.id);
              System.out.println("Message backed up successfully. Progress: " + finishedMessageIds.size() + "/" + office365backup1.getMessageCount());
            }
          }
          public void beforeMessageBackup(Office365backupBeforeMessageBackupEvent e) {
            if (e.skip) {
              System.out.println("Message exists locally, skipping: " + e.backupFile);
            }
          } 
          public void endBackup(Office365backupEndBackupEvent e) {
            backupCompleted = true;
            System.out.println("Backup Completed");
            System.out.println("Messages backed up: " + finishedMessageIds.size());
            System.out.println("Messages skipped: " + (office365backup1.getMessageCount() - finishedMessageIds.size()));
            if (office365backup1.isSyncDeletes()) {
              System.out.println("Messages deleted: " + messagesDeleted);
            }
          }
          public void log(Office365backupLogEvent e) {
            System.out.println(e.message);
          }
          public void messageDelete(Office365backupMessageDeleteEvent e) {
            messagesDeleted++;
            System.out.println("Message not present remotely, deleting local file: " + e.backupFile);
          }
          public void messageError(Office365backupMessageErrorEvent e) {
            if (e.retry) {
              // By default, we will retry 5 times
              System.out.println("Error backing up message, retrying: " + e.errorCode + ": " + e.errorMessage);
            } else {
              System.out.println("Error backing up message, skipping: " + e.errorCode + ": " + e.errorMessage);
            }
          }
        });

        // Set all command line arguments
        for (int i = 0; i < args.length; i++) {
          if (args[i].startsWith("-")) {
            if (args[i].toLowerCase().equals("-p")) {
              office365backup1.setDataFolder(args[i + 1]);
            }
            if (args[i].toLowerCase().equals("-f")) {
              office365backup1.setFilter(args[i + 1]);
            }
            if (args[i].toLowerCase().equals("-s")) {
              office365backup1.setStartDate(args[i + 1]);
            }
            if (args[i].toLowerCase().equals("-e")) {
              office365backup1.setEndDate(args[i + 1]);
            }
            if (args[i].toLowerCase().equals("-c")) {
              office365backup1.setMaxConnections(parseInt(args[i + 1]));
            }
            if (args[i].toLowerCase().equals("-d")) {
              office365backup1.setSyncDeletes(true);
            }
            if (args[i].toLowerCase().equals("-id")) {
              office365backup1.getOAuth().setClientId(args[i + 1]);
            }
            if (args[i].toLowerCase().equals("-secret")) {
              office365backup1.getOAuth().setClientSecret(args[i + 1]);
            }
          }
        }

        System.out.println("This is a basic demo showing how to backup your Office365 mail account.");
        System.out.println("To begin, please press enter to authorize.");
        input();

        // Get valid OAuth token
        office365backup1.getOAuth().setServerAuthURL("https://login.microsoftonline.com/common/oauth2/v2.0/authorize");
        office365backup1.getOAuth().setServerTokenURL("https://login.microsoftonline.com/common/oauth2/v2.0/token");
        office365backup1.getOAuth().setAuthorizationScope("offline_access mail.read");
        office365backup1.getOAuth().setGrantType(0);
        office365backup1.authorize();

        // Authorization successful, start backup process
        System.out.println("Authorization Successful");
        System.out.println("Starting Backup");
        System.out.println("Retrieving message list (note: this operation may take some time).");
        office365backup1.startBackup();

        while (!backupCompleted) {
          office365backup1.doEvents();
        }
      } catch (Exception ex) {
        System.out.println(ex.getMessage());
      }
      System.out.println("Press any key to exit...");
      input();
    }
  }
}
class ConsoleDemo {
  private static BufferedReader bf = new BufferedReader(new InputStreamReader(System.in));

  static String input() {
    try {
      return bf.readLine();
    } catch (IOException ioe) {
      return "";
    }
  }
  static char read() {
    return input().charAt(0);
  }

  static String prompt(String label) {
    return prompt(label, ":");
  }
  static String prompt(String label, String punctuation) {
    System.out.print(label + punctuation + " ");
    return input();
  }

  static String prompt(String label, String punctuation, String defaultVal)
  {
	System.out.print(label + " [" + defaultVal + "] " + punctuation + " ");
	String response = input();
	if(response.equals(""))
		return defaultVal;
	else
		return response;
  }

  static char ask(String label) {
    return ask(label, "?");
  }
  static char ask(String label, String punctuation) {
    return ask(label, punctuation, "(y/n)");
  }
  static char ask(String label, String punctuation, String answers) {
    System.out.print(label + punctuation + " " + answers + " ");
    return Character.toLowerCase(read());
  }

  static void displayError(Exception e) {
    System.out.print("Error");
    if (e instanceof CloudBackupException) {
      System.out.print(" (" + ((CloudBackupException) e).getCode() + ")");
    }
    System.out.println(": " + e.getMessage());
    e.printStackTrace();
  }
}



