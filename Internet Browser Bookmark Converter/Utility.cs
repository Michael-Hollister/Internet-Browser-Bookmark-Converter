using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

/*******************************************************************
* Copyright (c) 2013, Michael Hollister                            *
*                                                                  *
* This source code is subject to the terms of The MIT License.     *
* If a copy of The MIT License was not distributed with this file, *
* you can obtain one at http://opensource.org/licenses/MIT.        *
*******************************************************************/

namespace UtilityMethods
{
    /// <summary>
    /// Contains utility methods for path strings.
    /// </summary>
    public static class Path
    {
        /// <summary>
        /// Returns the path slash that is contained in the string.
        /// </summary>
        /// <param name="path">The path string.</param>
        /// <returns>Returns the slash.</returns>
        public static string ReturnPathSlash(string path)
        {
            if (path == null)
                return null;
            else
                return path[path.Length - 1] == '\\' ? "/" : "\\";
        }
    }
}
