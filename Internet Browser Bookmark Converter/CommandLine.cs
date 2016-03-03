using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;

/*******************************************************************
* Copyright (c) 2013, Michael Hollister                            *
*                                                                  *
* This source code is subject to the terms of The MIT License.     *
* If a copy of The MIT License was not distributed with this file, *
* you can obtain one at http://opensource.org/licenses/MIT.        *
*******************************************************************/

namespace Internet_Browser_Bookmark_Converter
{
    /// <summary>
    /// Contains the settings that can be set in the commandline.
    /// </summary>
    public static class CommandLine
    {
        #region Fields

        // Command line parameters
        private static bool showHelp = false;

        // Command line flags
        private const string flagFFConvert = "ffc|ffconvert";
        private const string flagFFProfilePath = "ffpp|ffprofilepath=";
        //private const string flagFFBookmarkExclusions = "";
        private const string flagIEConvert = "iec|ieconvert";
        private const string flagIEFavoritesPath = "iefp|iefavoritespath=";
        //private const string flagIEFavoritesExclusions = "";
        private const string flagShowHelp = "h|help|?";
        
        // Documentation strings.
        private const string helpFFConvert = "[bool] : Converts Internet Explorer favorites to Firefox bookmarks";
        private const string helpFFProfilePath = "[string] : The path to firefox profile folder";
        private const string helpFFBookmarkExclusions = "[string] : A space delimited list of string of paths to the folders that will be excluded in the conversion";
        private const string helpIEConvert = "[bool] : Converts Firefox bookmarks to Internet Explorer favorites";
        private const string helpIEFavoritesPath = "[string] : The path to the favorites folder";
        private const string helpIEFavoritesExclusions = "[string] : A space delimited list of string of paths to the folders that will be excluded in the conversion";
        private const string helpShowHelp = "[bool] : Shows this message";

        // Error codes
        private const int errorInvalidPath = 1;

        #region Properties Fields

        // Private property variables
        private static string _FFProfilePath = null;
        private static string _IEFavoritesPath = null;

        #endregion

        #region Properties

        /// <summary>
        /// A flag to indicate if FF bookmarks will be converted to IE favorites.
        /// </summary>
        public static bool FFConvert { get; set; }
        /// <summary>
        /// A flag to indicate if IE favorites will be converted to FF bookmarks.
        /// </summary>
        public static bool IEConvert { get; set; }

        /// <summary>
        /// Contains the path to the profile directory.
        /// </summary>
        public static string FFProfilePath
        {

            get { return _FFProfilePath; }
            set
            {
                // Make sure it exists first.
                if (System.IO.Directory.Exists(value))
                    // Append a '\' if it is not in the path string.
                    _FFProfilePath = value[value.Length-1] == '\\' || value[value.Length-1] == '/' ? value :
                        value + UtilityMethods.Path.ReturnPathSlash(value);
                else
                {
                    _FFProfilePath = null;
                    throw new System.IO.DirectoryNotFoundException("The directory does not exists at " + value);
                }
            }
        }

        /// <summary>
        /// Contains the path to the favorites directory.
        /// </summary>
        public static string IEFavoritesPath
        {
            get { return _IEFavoritesPath; }
            set
            {
                // Make sure it exists first.
                if (System.IO.Directory.Exists(value))
                    _IEFavoritesPath = value[value.Length-1] == '\\' || value[value.Length-1] == '/' ? _IEFavoritesPath = value :
                        _IEFavoritesPath = value + UtilityMethods.Path.ReturnPathSlash(value);
                else
                {
                    _IEFavoritesPath = null;
                    throw new System.IO.DirectoryNotFoundException("The directory does not exists at " + value);
                }
            }
        }

        /// <summary>
        /// Returns the path to the Internet explorer favorites bar folder that contains the toolbar button links.
        /// </summary>
        public static string IEFavoritesToolbarPath
        {
            get
            {
                return _IEFavoritesPath + UtilityMethods.Path.ReturnPathSlash(_IEFavoritesPath)
                + "Favorites Bar" + UtilityMethods.Path.ReturnPathSlash(_IEFavoritesPath);
            }
        }

        /// <summary>
        /// Contains all the paths the user wanted to exclude from conversion.
        /// </summary>
        public static List<string> IEFavoritesExclusions { get; set; }
        /// <summary>
        /// Contains all the paths the user wanted to exclude from conversion.
        /// </summary> 
        public static List<string> FFBookmarkExclusions { get; set; }

        #endregion


        #endregion

        /// <summary>
        /// Parses the command line arguments to store in the class.
        /// </summary>
        public static void ParseArguments(string[] args)
        {
            // Initialize all command line options
            OptionSet Options = new OptionSet() { 
                { flagFFConvert, helpFFConvert, v => { FFConvert = (v != null); } },
                { flagFFProfilePath, helpFFProfilePath, v => 
                {
                    // Make sure the path exists
                    try{
                        FFProfilePath = v;
                    }
                    catch(System.IO.DirectoryNotFoundException e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Aborting program...");
                        Environment.Exit(errorInvalidPath);
                    }
                } },

                { flagIEConvert, helpIEConvert, v => { IEConvert = (v != null); } },
                { flagIEFavoritesPath, helpIEFavoritesPath, v => 
                { 
                    // Make sure the path exists
                    try{
                        IEFavoritesPath = v;
                    }
                    catch(System.IO.DirectoryNotFoundException e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Aborting program...");
                        Environment.Exit(errorInvalidPath);
                    }
                } },

                { flagShowHelp, helpShowHelp, v => { showHelp = (v != null); } },

                // Use default handler to parse multi arg lists
                { "<>", v => {
                    string command = v.Substring(0,v.IndexOf('='));

                    if (command.Contains("iefpe") || command.Contains("iefavoritespathexclusions"))
                    {
                        // Verify the paths before exiting
                        List<string> paths = ParseMultiArgPaths(v.Substring(v.IndexOf('=') + 1));
                        IEFavoritesExclusions = VerifyListExclusionsPaths(paths);
                    }

                    if (command.Contains("ffbpe") || command.Contains("ffbookmarkexclusions"))
                    {
                        // Since the database can't be enumerated before processing command line arguments, there will be no verification the these database paths exist.

                        // temp, truncate the first character of the path as it is used as a dummy for parsing.
                        List<string> paths = ParseMultiArgPaths(v.Substring(v.IndexOf('=') + 1));

                        foreach (string path in paths)
                        {
                            FFBookmarkExclusions.Add(path.Substring(2));
                        }


                        //FFBookmarkExlusions = VerifyListExclusionsPaths(paths);
                    }
                }}

            };

            // Initialize default values before reading them
            FFConvert = false;
            IEConvert = false;
           
            // Prevent null reference exceptions
            IEFavoritesExclusions = new List<string>();
            FFBookmarkExclusions = new List<string>();

            // Parse the command line arguments
            try
            {
                var l = Options.Parse(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in command line parsing: " + e.Message);
            }
            
            // Show help if selected
            if (showHelp)
                ShowHelpText(Options);
        }


        /// <summary>
        /// Displays information about the command line arguments to the user.
        /// </summary>
        private static void ShowHelpText(OptionSet Options)
        {
            Console.WriteLine(Program.AppSettings.programName + " " + Program.AppSettings.version);
            Console.WriteLine();
            Console.WriteLine("=========================");
            Console.WriteLine("Comand line documentation");
            Console.WriteLine("=========================");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine();
            Options.WriteOptionDescriptions(Console.Out);

            // Since FF and IE excluded paths are multi-arg lists, they don't dispay in the documentation.
            Console.WriteLine();
            Console.WriteLine("      --{0}, --{1} \n{2}", "iefpe", "iefavoritespath", helpIEFavoritesExclusions);
            Console.WriteLine();
            Console.WriteLine("      --{0}, --{1} \n{2}", "ffbpe", "ffbookmarkexclusions", helpFFBookmarkExclusions);
            Console.WriteLine();
        }

        /// <summary>
        /// Verifies the paths point to valid directories.  Any directories with notexistent paths will be removed.
        /// </summary>
        /// <param name="list">The list to check for nonexistent directories.</param>
        /// <returns>Returns the list with notexistent paths paths removed.</returns>
        private static List<string> VerifyListExclusionsPaths(List<string> list)
        {
            for (int x = 0; x < list.Count; x++)
            {
                if (!(System.IO.Directory.Exists(list[x])) && !(System.IO.File.Exists(list[x])))
                {
                    Console.WriteLine("Directory or file does not exit, removing exclusion: " + list[x]);
                    list.RemoveAt(x);
                    x--;
                }
            }

            return list;
        }

        /// <summary>
        /// Parses a multi-argument string into a path list.
        /// </summary>
        /// <param name="argument">The multi-argument string.</param>
        /// <returns>The list of paths.</returns>
        private static List<string> ParseMultiArgPaths(string argument)
        {
            List<string> returnList = new List<string>();

            while (true)
            {
                // Find the volume separator to use as a delimiter.
                if (argument.Substring(2).IndexOf(':') != -1)
                {
                    returnList.Add(argument.Substring(0, argument.Substring(2).IndexOf(':')));
                    argument = argument.Substring(argument.Substring(2).IndexOf(':') + 1);
                }
                else
                {
                    // Add the last element.
                    returnList.Add(argument);
                    break;
                }
            }

            return returnList;
        }


    }

}

