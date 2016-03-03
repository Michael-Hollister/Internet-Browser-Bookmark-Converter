using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

/*******************************************************************
* Copyright (c) 2013, Michael Hollister                            *
*                                                                  *
* This source code is subject to the terms of The MIT License.     *
* If a copy of The MIT License was not distributed with this file, *
* you can obtain one at http://opensource.org/licenses/MIT.        *
*******************************************************************/

namespace Internet_Browser_Bookmark_Converter
{
    public class Program
    {
        #region Structs

        /// <summary>
        /// The group of configuable application settings
        /// </summary>
        public struct AppSettings
        {
            public struct Keys
            {
                public const string programName = "programName";
                public const string version = "version";
                public const string backupEnabled = "backupEnabled";
                public const string backupDirectory = "backupDirectory";
                public const string backupMaximumAmountKept = "backupMaximumAmountKept";
            }

            public static readonly string programName;
            public static readonly string version;
            public static readonly bool backupEnabled;
            public static readonly string backupDirectory;
            public static readonly int backupMaximumAmountKept;

            static AppSettings()
            {
                // Read the settings
                System.Configuration.AppSettingsReader reader = new System.Configuration.AppSettingsReader();

                programName = (string)reader.GetValue(AppSettings.Keys.programName,typeof(string));
                version = (string)reader.GetValue(AppSettings.Keys.version, typeof(string));
                backupEnabled = (bool)reader.GetValue(AppSettings.Keys.backupEnabled, typeof(bool));
                backupDirectory = (string)reader.GetValue(AppSettings.Keys.backupDirectory, typeof(string));
                backupMaximumAmountKept = (int)reader.GetValue(AppSettings.Keys.backupMaximumAmountKept, typeof(int));

                if (AppSettings.backupDirectory == null || AppSettings.backupDirectory == "")
                    AppSettings.backupDirectory = Environment.CurrentDirectory + "\\Backup\\";
            }
        }

        #endregion

        static void Main(string[] args)
        {
            // Extra information 
            int numberOfConvertedFiles = 0;
            DateTime programStart = DateTime.Now;

            CommandLine.ParseArguments(args);
            
            if (AppSettings.backupEnabled)
                PerformBackup();

            // Perform conversion operations.
            // Make sure there are paths assigned and there as at least one conversion operation assigned.
            if ((CommandLine.FFProfilePath != null && CommandLine.IEFavoritesPath != null) && (CommandLine.FFConvert || CommandLine.IEConvert))
            {
                FF_Bookmark.DBOpenConnection();
                IE_Favorite.EnumerateFavoritesDirectory();
                FF_Bookmark.EnumerateProfileBookmarks();
                List<Sync.SyncFileState> FileList = Sync.CompareManifestFiles();

                Sync.PerformFileOperations(FileList);
                numberOfConvertedFiles = FileList.Count;
                FF_Bookmark.DBCloseConnection();

                Console.WriteLine(string.Format("Conversion Successful!\n\nConverted {0} files in {1} amount of time.",
                    numberOfConvertedFiles,DateTime.Now - programStart));
            }
            else
                Console.WriteLine((CommandLine.FFConvert || CommandLine.IEConvert) == false ? "The conversion operation has not been performed.  Asisgn a conversion operation flag to he command line arguments."
                    : CommandLine.FFProfilePath == null ? "Firefox profile path was not assigned.  Aborting program..."
                    : "Internet Explorer favorites path was not assigned.  Aborting program..." );

        }

        /// <summary>
        /// Backups the favorites directory and places.sqlite before conversion.
        /// </summary>
        private static void PerformBackup()
        {
            string date = DateTime.Today.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
            // Since the date string returns the format mm/dd/yy, replace the '/' characters so they will not be separate directories.
            date = date.Replace('/', '-');
            date = date.Replace(':', '-');

            // If it has not been created before, create the directory for storing the backups
            if (!System.IO.Directory.Exists(AppSettings.backupDirectory))
                System.IO.Directory.CreateDirectory(AppSettings.backupDirectory);

            if (AppSettings.backupMaximumAmountKept > 0)
            {
                List<string> oldBackups = System.IO.Directory.EnumerateDirectories(AppSettings.backupDirectory).ToList();

                if (oldBackups.Count > AppSettings.backupMaximumAmountKept)
                {
                    // Delete the oldest backups and keep the most recent ones
                    int amountToDelete = oldBackups.Count - AppSettings.backupMaximumAmountKept;

                    for (int i = 0; i < amountToDelete; i++)
                        System.IO.Directory.Delete(oldBackups[i],true);
                }
            }

            System.IO.Directory.CreateDirectory(AppSettings.backupDirectory + "\\" + date);

            // To make things easy, just copy to a empty directory and make a zip file from that
            System.IO.File.Copy(CommandLine.FFProfilePath + "\\" + FF_Bookmark.BookmarksFile, AppSettings.backupDirectory + "\\"
                + date + "\\" + FF_Bookmark.BookmarksFile);
            
            System.IO.Compression.ZipFile.CreateFromDirectory(AppSettings.backupDirectory + "\\" + date + "\\",
                AppSettings.backupDirectory + "\\" + FF_Bookmark.BookmarksFile + ".zip");

            System.IO.File.Move(AppSettings.backupDirectory + "\\" + FF_Bookmark.BookmarksFile + ".zip", 
                AppSettings.backupDirectory + "\\" + date + "\\" + FF_Bookmark.BookmarksFile + ".zip");
            System.IO.File.Delete(AppSettings.backupDirectory + "\\" + date + "\\" + FF_Bookmark.BookmarksFile);

            if (System.IO.File.Exists(CommandLine.FFProfilePath + Sync.syncFileNameFF))
                System.IO.File.Copy(CommandLine.FFProfilePath + Sync.syncFileNameFF, AppSettings.backupDirectory + "\\" + date + "\\" + Sync.syncFileNameFF);

            System.IO.Compression.ZipFile.CreateFromDirectory(CommandLine.IEFavoritesPath, AppSettings.backupDirectory + "\\" +
                date + "\\" + System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetDirectoryName(CommandLine.IEFavoritesPath)) + ".zip");
        }

    }
}

