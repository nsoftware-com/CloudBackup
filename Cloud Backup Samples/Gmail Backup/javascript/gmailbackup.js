/*
 * Cloud Backup 2022 JavaScript Edition - Sample Project
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
 
const readline = require("readline");
const cloudbackup = require("@nsoftware/cloudbackup");

if(!cloudbackup) {
  console.error("Cannot find cloudbackup.");
  process.exit(1);
}
let rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout
});

let gmailbackup1;
let finishedMessageIds;
let backupCompleted;
let messagesDeleted;
main();

async function main() {
  const argv = process.argv;
  if (argv.length < 8) {
    console.log("\nIncorrect arguments specified.")
    console.log("\nUsage: node gmailbackup.js -id client_id -secret client_secret -p path [-f filter] [-s YYYY/mm/DD] [-e YYYY/mm/DD] [-c max_connections] [-d]\n");
    console.log("-id:           OAuth Client ID associated with the registered application (required)");
    console.log("-secret:       OAuth Client Secret associated with the registered application (required)");
    console.log("-p:            Directory to save messages to (required)");
    console.log("-f:            Filter applied when retrieving messages (optional)");
    console.log("-s:            Upper limit of date range when retrieving messages (optional)");
    console.log("-e:            Lower limit of date range when retrieving messages (optional)");
    console.log("-c:            Number of simultaneous connections used (optional)");
    console.log("-d:            Flag to enable whether the component will sync messages deleted remotely (optional)");
    console.log("\nExample: node gmailbackup.js -id client_id -secret client_secret -p ../../test_folder -f in:sent -s 2023/09/01 -e 2023/09/15 -c 5 -d\n");
    console.log("Example: gmailbackup.js -id client_id -secret client_secret -p ../../test_folder\n");
    process.exit();
  } else {
    try {
      gmailbackup1 = new cloudbackup.gmailbackup();
      finishedMessageIds = [];
      backupCompleted = false;
      messagesDeleted = 0;
  
      gmailbackup1.on("AfterMessageBackup", function(e) {
        finishedMessageIds.push(e.id);
        console.log("Message backed up successfully. Progress: " + finishedMessageIds.length + "/" + gmailbackup1.getMessageCount());
      })
      .on("BeforeMessageBackup", function(e) {
        if (e.skip) {
          console.log("Message exists locally, skipping: " + e.backupFile);
        }
      })
      .on("EndBackup", function(e) {
        backupCompleted = true;
        console.log("Backup Completed");
        console.log("Messages backed up : " + finishedMessageIds.length);
        console.log("Messages skipped: " + (gmailbackup1.getMessageCount() - finishedMessageIds.length));
        if (gmailbackup1.isSyncDeletes()) {
          console.log("Messages deleted: " + messagesDeleted);
        }
      })
      .on("Log", function(e) {
        console.log(e.message);
      })
      .on("MessageDelete", function(e) {
        messagesDeleted++;
        console.log("Message not present remotely, deleting local file: " + e.backupFile);
      })
      .on("MessageError", function(e) {
        if (e.retry) {
          // By default, we will retry 5 times
          console.log("Error backing up message, retrying: " + e.errorCode + ": " + e.errorMessage);
        } else {
          console.log("Error backing up message, skipping: " + e.errorCode + ": " + e.errorMessage);
        }
      });
  
      // Set all command line arguments
      process.argv.forEach(function (val, i, array) {
        if (val.startsWith("-")) {
          if (val === "-p") {
            gmailbackup1.setDataFolder(array[i + 1]);
          }
          if (val === "-f") {
            gmailbackup1.setFilter(array[i + 1]);
          }
          if (val === "-s") {
            gmailbackup1.setStartDate(array[i + 1]);
          }
          if (val === "-e") {
            gmailbackup1.setEndDate(array[i + 1]);
          }
          if (val === "-c") {
            gmailbackup1.setMaxConnections(parseInt(array[i + 1]));
          }
          if (val === "-d") {
            gmailbackup1.setSyncDeletes(true);
          }
          if (val === "-id") {
            gmailbackup1.getOAuth().setClientId(array[i + 1]);
          }
          if (val === "-secret") {
            gmailbackup1.getOAuth().setClientSecret(array[i + 1]);
          }
        }
      });
  
      console.log("This is a basic demo showing how to backup your Gmail account.");
      console.log("To begin, please authorize the component.");
  
      // Get valid OAuth token
      gmailbackup1.getOAuth().setServerAuthURL("https://accounts.google.com/o/oauth2/auth");
      gmailbackup1.getOAuth().setServerTokenURL("https://accounts.google.com/o/oauth2/token");
      gmailbackup1.getOAuth().setAuthorizationScope("https://www.googleapis.com/auth/gmail.readonly");
      gmailbackup1.getOAuth().setGrantType(0);
      await gmailbackup1.authorize();
  
      // Authorization successful, start backup process
      console.log("Authorization Successful");
      console.log("Starting Backup");
      console.log("Retrieving message list (note: this operation may take some time).");
      await gmailbackup1.startBackup();
  
      while (!backupCompleted) {
        await gmailbackup1.doEvents();
      }
    } catch (err) {
      console.log(err);
    }
    process.exit();
  }
}

function prompt(promptName, label, punctuation, defaultVal)
{
  lastPrompt = promptName;
  lastDefault = defaultVal;
  process.stdout.write(`${label} [${defaultVal}] ${punctuation} `);
}
