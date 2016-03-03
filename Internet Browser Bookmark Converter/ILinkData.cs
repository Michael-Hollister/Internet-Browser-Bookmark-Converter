using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*******************************************************************
* Copyright (c) 2013, Michael Hollister                            *
*                                                                  *
* This source code is subject to the terms of The MIT License.     *
* If a copy of The MIT License was not distributed with this file, *
* you can obtain one at http://opensource.org/licenses/MIT.        *
*******************************************************************/

namespace Internet_Browser_Bookmark_Converter
{
    public enum TypeID { Link, Directory, Null }

    /// <summary>
    /// Contains common data that is shared between favorites and bookmarks
    /// </summary>
    public interface ILinkData
    {
        string URL { get; set;}
        string Title { get; set; }
        string PathHierarchy { get; set; }
        TypeID ResourceType { get; set; }
        bool IsExcluded { get; set; }

        bool IsCongruentTo(ILinkData link);
        bool IsChildOf(ILinkData link);
    }
}
