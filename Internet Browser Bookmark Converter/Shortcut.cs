using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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
    /// A static class for managing shortcuts.
    /// </summary>
    public static class Shortcut
    {
        /// <summary>
        /// Gets the shortcut target.
        /// </summary>
        /// <param name="path">The path to the shortcut.</param>
        /// <returns>The URL of the shortcut.</returns>
        public static string GetInternetShortcutTarget(string path)
        {
            string Data;

            // Since there is no easy way to get the url from a link, they must be read from the byte stream
            using (StreamReader Reader = new StreamReader(path))
            {
                do
                {
                    Data = Reader.ReadLine();
                    if (Data.ToLower().Contains("url="))
                    {
                        break;
                    }
                } while (!Reader.EndOfStream);
            }

            return Data.Substring(Data.ToLower().IndexOf("url=") + 4);
        }

        /// <summary>
        /// Creates a internet shortcut.
        /// </summary>
        /// <param name="path">The path to create it.</param>
        /// <param name="url">The internet link.</param>
        public static void CreateInternetShortcut(string path, string url)
        {
            // Self-Note: Try to find a reference on the web for link file sturcture...
            using (StreamWriter Writer = new StreamWriter(path))
            {
                Writer.WriteLine("[InternetShortcut]");
                Writer.WriteLine("URL=" + url);
                Writer.Flush();
            }
        }


    }
}
