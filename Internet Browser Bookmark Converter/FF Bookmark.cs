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
    /// <summary>
    /// Represents a Firefox bookmark.
    /// </summary>
    [Serializable]
    public class FF_Bookmark : ILinkData
    {
        #region Fields

        #region Constants

        /// <summary>
        /// The file that contains bookmark information.  ("places.sqlite")
        /// </summary>
        public const string BookmarksFile = "places.sqlite";

        // Special database row titles
        public const string DBR_BookmarksMenu = "Bookmarks Menu";
        public const string DBR_BookmarksToolbar = "Bookmarks Toolbar";
        public const string DBR_UnsortedBookmarks = "Unsorted Bookmarks";

        // Map the 'Unsorted Bookmarks' to the uncategorized directory in the favorites directory
        public const string DBR_Mapping_UnsortedBookmarks = "” UNCATEGORIZED ”";

        // Exception text
        private const string exceptionDBNotOpen = "Database is not open.";
        private const string exceptionIndexNotFound = "Index not found: ";
        private const string exceptionHierarchyCorruption = "The parent level directories does not exist for ";

        #endregion

        #region Variables

        /// <summary>
        /// Stores all the bookmarks read from the database.
        /// </summary>
        public static List<FF_Bookmark> BookmarksList = new List<FF_Bookmark>();

        /// <summary>
        /// Contains the system directories that will not be converted.
        /// </summary>
        public static readonly string[] SystemDirectories = {
            DBR_BookmarksMenu,
            DBR_BookmarksToolbar,
            DBR_UnsortedBookmarks,
            "Tags",
            "Recently Bookmarked",
            "Recent Tags",
            "History",
            "Downloads",
            "All Bookmarks"
        };

        #endregion

        #region Properties

        #region ILinkData Members

        public string URL { get; set; }
        public string Title { get; set; }

        /// <summary>
        /// Contains the relative path from the favorites folder to the parent directory of the favorite.
        /// </summary>
        public string PathHierarchy { get; set; }

        /// <summary>
        /// Indicates wether this resource is a directory or a link.
        /// </summary>
        public TypeID ResourceType { get; set; }

        /// <summary>
        /// Indicates if the bookmark is not to have any operations performed on it.
        /// </summary>
        public bool IsExcluded { get; set; }

        #endregion

        /// <summary>
        /// Indicates if this object is a representation of a system entry in the database.
        /// </summary>
        public bool DBSystemEntry { get; set; }

        // moz_bookmarks database structure.
        private long? ID;
        /// <summary>
        /// The type of resource the bookmark is.  (Link, Directory, Etc...)
        /// </summary>
        private long? Type;
        /// <summary>
        /// The ID of the row in moz_places that stores the url of the bookmark.
        /// </summary>
        private long? FK { get; set; }
        /// <summary>
        /// The ID of the parent row.
        /// </summary>
        private long? Parent { get; set; }
        /// <summary>
        /// The position the bookmark is listed.
        /// </summary>
        private long? Position { get; set; }

        // Not Implemented.  (TFS WI-6)
        
        /// <summary>
        /// The date the bookmark was added to the database.
        /// </summary>
        private DateTime? DateAdded { get; set; }
        /// <summary>
        /// The date the bookmark was modified.
        /// </summary>
        private DateTime? LastModified { get; set; }
        private string GUID { get; set; }

        #endregion

        #region Structures

        /// <summary>
        /// Provides a way to reference the foregin key to the url in the database.
        /// </summary>
        private struct URLData
        {
            /// <summary>
            /// Stores the data to reference the foregin key to the url in moz_places
            /// </summary>
            private struct ListData
            {
                public long? ID { get; set; }
                public string URL { get; set; }
            }

            /// <summary>
            /// Stores all the URL's and ID's in moz_places
            /// </summary>
            private List<ListData> URLList;

            #region Methods

            /// <summary>
            /// Returns the url has the corresponding ID value.
            /// </summary>
            /// <param name="i">ID of the row</param>
            /// <returns>The url of the ID.  Returns null if the id was not found.</returns>
            public string this[long? i]
            {
                get
                {
                    foreach (ListData item in URLList)
                    {
                        if (item.ID == i)
                            return item.URL;
                    }

                    return null;
                }
            }

            /// <summary>
            /// Adds a id and url for later reference.
            /// </summary>
            /// <param name="id">ID of the row.</param>
            /// <param name="url">URL of the entry.</param>
            public void Add(long? id, string url)
            {
                if (URLList == null)
                    URLList = new List<ListData>();

                ListData Data = new ListData();
                Data.ID = id;
                Data.URL = url;
                URLList.Add(Data);
            }


            #endregion

        }

        /// <summary>
        /// Stores information about the database
        /// </summary>
        private struct DBInfo
        {
            /// <summary>
            /// Contains the state of the database connection.
            /// </summary>
            public static bool dbConnectionOpen = false;

            public static System.Data.SQLite.SQLiteFactory SQLFactory;
            public static System.Data.Common.DbConnection SQLConnection;
        }

        #endregion

        #region Enums

        /// <summary>
        /// The type of resource the row is.
        /// </summary>
        private enum BookmarkType
        {
            Link = 1, Directory = 2
        }

        #endregion
        
        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an FF_Bookmark object that represents an Firefox bookmark.
        /// </summary>
        /// <param name="url">The link that the bookmark links to.</param>
        /// <param name="id">The id of the row.</param>
        /// <param name="type">The type of data the row represents.  (Link, Directory, etc..)</param>
        /// <param name="fk">The id that is referenced for extra data.</param>
        /// <param name="parent">The parent row.</param>
        /// <param name="position">UNKNOWN</param>
        /// <param name="title">The title of the bookmark/directory.</param>
        public FF_Bookmark(string url, long? id, long? type, long? fk, long? parent, long? position, string title)
        {
            this.ID = id;
            this.Type = type;
            this.FK = fk;
            this.Parent = parent;
            this.Position = position;
            this.Title = title;

            this.URL = url;
            this.ResourceType = type == (long)BookmarkType.Link ? TypeID.Link : (type == (long)BookmarkType.Directory ? TypeID.Directory : TypeID.Null);
            this.PathHierarchy = this.GetParentDirectory();

            // System rows are considered to be null or empty strings, or containing defined system row titles.
            // Note: All system entries have id's that are less than 24
            if((this.Title == null || this.Title == "") || (id < 24 && SystemDirectories.Contains(this.Title)))
                this.DBSystemEntry = true;
            else
                this.DBSystemEntry = false;

            if (IsBookmarkExcluded(this))
                this.IsExcluded = true;
        }

        // Not Implemented.  (TFS WI-6)
        //   private DateTime DateAdded { get; set; }
        //   private DateTime LastModified { get; set; }
        //   private Guid GUID { get; set; }

        #endregion
        
        #region Methods
        
        #region Private Methods

        #region Database Methods

        /// <summary>
        /// Reads a column and returns the value.
        /// </summary>
        /// <param name="columnName">The name of the column to read.</param>
        /// <param name="tableReader">The data reader.</param>
        /// <returns>The read column value.</returns>
        private static long? DBReadTableData(ref System.Data.Common.DbDataReader tableReader, string columnName)
        {
            long? data; long tempData;

            // Correct some invalid cast exceptions by parsing the string into the value
            if (long.TryParse(tableReader[columnName].ToString(), out tempData))
                data = tempData;
            else
                data = null;

            return data;
        }

        /// <summary>
        /// Returns a row id for a given table in the queries.
        /// </summary>
        /// <param name="maxIDQuery">The query that returns the max id of a table.</param>
        /// <param name="idQuery">The query that returns all the id's of a table.</param>
        /// <returns>A unique id that is numerically greater than all other ids.</returns>
        private static long DBGenerateRowID(string maxIDQuery, string idQuery)
        {
            long rowID = 0;

            System.Data.Common.DbCommand SQLCommand = DBInfo.SQLFactory.CreateCommand();
            SQLCommand.Connection = DBInfo.SQLConnection;
            SQLCommand.CommandText = maxIDQuery;
            rowID = (long)SQLCommand.ExecuteScalar() + 1;

            return rowID;
        }

        /// <summary>
        /// Creates a database row for from the following parameters.
        /// </summary>
        /// <param name="type">Determines whether the type will be a folder or a link.</param>
        /// <param name="title">Title of the resource.</param>
        /// <param name="parent">Parent id of the row.</param>
        /// <param name="url">Url if this resource is a link.</param>
        /// <param name="fk">The id where the url is contained.</param>
        /// <param name="position">UNKNOWN</param>
        /// <param name="id">The id of the row.</param>
        private static void DBCreateRow(long? type, string title, long? parent, string url = null, long? fk = null,
            long? position = null, long? id = null)
        {
            System.Data.Common.DbCommand SQLCommand = DBInfo.SQLFactory.CreateCommand();
            long rowID = 0;

            // Bookmark folders don't need any information from moz_places, so that is skiped if this row being created is a directory
            if (type == (long?)FF_Bookmark.BookmarkType.Link)
            {
                // Since the url may allready exist in moz_places, check there first.
                SQLCommand = DBInfo.SQLFactory.CreateCommand();
                SQLCommand.Connection = DBInfo.SQLConnection;
                SQLCommand.CommandText = "SELECT id FROM moz_places WHERE url = @url";
                SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@url", url));
                long? urlID = (long?)SQLCommand.ExecuteScalar();

                if (!(urlID == null))
                    rowID = (long)urlID;
                else
                {
                    rowID = DBGenerateRowID("SELECT MAX(id) FROM moz_places;", "SELECT id FROM moz_places;");
                    bool hidden = true;

                    SQLCommand = DBInfo.SQLFactory.CreateCommand();
                    SQLCommand.Connection = DBInfo.SQLConnection;
                    SQLCommand.CommandText = "INSERT INTO moz_places (id,url,title,hidden) VALUES(@id,@url,@title,@hidden);";

                    SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@id", rowID));
                    SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@url", url));
                    SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@title", title));
                    SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@hidden", hidden));

                    SQLCommand.ExecuteNonQuery();
                }
            }

            #region moz_bookmarks row insertion

            if (id == null)
                id = DBGenerateRowID("SELECT MAX(id) FROM moz_bookmarks;", "SELECT id FROM moz_bookmarks;");

            // Assign null fields
            if (position == null)
                position = 0;
            if (type == (long?)FF_Bookmark.BookmarkType.Link)
                fk = rowID;

            SQLCommand = DBInfo.SQLFactory.CreateCommand();
            SQLCommand.Connection = DBInfo.SQLConnection;
            SQLCommand.CommandText = "INSERT INTO moz_bookmarks (id,type,fk,parent,position,title) VALUES(@id,@type,@fk,@parent,@position,@title);";
            SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@id", id));
            SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@type", type));
            SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@fk", fk));
            SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@parent", parent));
            SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@position", position));
            SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@title", title));
            SQLCommand.ExecuteNonQuery();

            #endregion

            FF_Bookmark.BookmarksList.Add(new FF_Bookmark(url, id, type, fk, parent, position, title));
           
        }

        /// <summary>
        /// Removes the bookmark entry from the database.
        /// </summary>
        /// <param name="bookmark">The bookmark to remove.</param>
        private static void DBRemoveRow(FF_Bookmark bookmark)
        {
            System.Data.Common.DbCommand SQLCommand = DBInfo.SQLFactory.CreateCommand();

            // Bookmarks link entries also have data in moz_places, so remove the data there as well.
            if (bookmark.ResourceType == TypeID.Link)
            {
                    // No index reference is stored in the bookmark on where the link resides in moz_places, so requery the DB for that info.
                    SQLCommand = DBInfo.SQLFactory.CreateCommand();
                    SQLCommand.Connection = DBInfo.SQLConnection;
                    SQLCommand.CommandText = "SELECT id FROM moz_places WHERE url = @url";
                    SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@url", bookmark.URL));
                    long? urlID = (long?)SQLCommand.ExecuteScalar();

                    // Since there may be other links that reference the same row in moz_places, check the whole database to 
                    // see if it is referenced before deleatng the row.
                    bool abort = false;
                    
                    foreach (FF_Bookmark listbookmark in FF_Bookmark.BookmarksList)
                    {
                        if (listbookmark != bookmark && listbookmark.FK == urlID)
                            abort = true;

                    }

                    if (!abort)
                    {
                        SQLCommand = DBInfo.SQLFactory.CreateCommand();
                        SQLCommand.Connection = DBInfo.SQLConnection;
                        SQLCommand.CommandText = "DELETE FROM moz_places WHERE id = @id;";

                        SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@id", bookmark.ID));
                        SQLCommand.ExecuteNonQuery();      
                    }
            }

            SQLCommand = DBInfo.SQLFactory.CreateCommand();
            SQLCommand.Connection = DBInfo.SQLConnection;
            SQLCommand.CommandText = "DELETE FROM moz_bookmarks WHERE id = @id;";

            SQLCommand.Parameters.Add(new System.Data.SQLite.SQLiteParameter("@id", bookmark.ID));
            SQLCommand.ExecuteNonQuery();

        }

        #endregion

        /// <summary>
        /// Returns the index that is in the bookmarks list that contains the id.
        /// </summary>
        /// <param name="id">The id of the row.</param>
        /// <exception cref="NullReferenceException">Throws NullReferenceException if the id does not exist.</exception>
        /// <returns>The list index that contains the row information.  Returns 0 if not found.</returns>
        /// 
        private static int GetBookmarkListIndexFromID(long? id)
        {
            foreach (FF_Bookmark bookmark in BookmarksList)
                if (bookmark.ID == id)
                    return BookmarksList.IndexOf(bookmark);

            throw new NullReferenceException(exceptionIndexNotFound + id);
        }

        /// <summary>
        /// Checks to see if the bookmark should be excluded from the convertion operation.
        /// </summary>
        /// <param name="bookmark">The bookmark to check.</param>
        /// <returns>Returns true if the bookmark is in the exclusion list.</returns>
        private static bool IsBookmarkExcluded(FF_Bookmark bookmark)
        {
            // Beyond excluding the values from the command line list, exclude the database system entries.
            // 'bookmark.ID < 24', it may be possible that the bookmark title might contains any of the system directories that are to be 
            // excluded.  All new bookmark entries id's will be greater than 24.  So this will make sure that there are no missed conversions.
            if (bookmark.DBSystemEntry)
                return false;
            else
            {
                if (CommandLine.FFBookmarkExclusions.Contains(bookmark.PathHierarchy + "\\" + bookmark.Title) ||
                    CommandLine.FFBookmarkExclusions.Contains(bookmark.PathHierarchy == null ? "" : bookmark.PathHierarchy))
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Checks to see if the needed directories to put the bookmark in the database is the same as the favorites folder structure.
        /// </summary>
        /// <param name="favorite">The favorite data to compare for.</param>
        /// <returns>Returns true if the directory hierarchy is allreay in the database.</returns>
        private static bool FavoriteDirectoryHierarchyInDB(IE_Favorite favorite)
        {
            // Null path hiearchies are at the root, so they exist.
            if (favorite.PathHierarchy == null)
                return true;
            else
            {
                // Map the favorite bar directory to the bookmarks toolbar directory
                string pathHierarchy = System.IO.Path.GetFileNameWithoutExtension(favorite.PathHierarchy) == IE_Favorite.FavoritesBar ? 
                    DBR_BookmarksToolbar : System.IO.Path.GetFileNameWithoutExtension(favorite.PathHierarchy);

                if (BookmarksList.Exists((bookmark) =>
                {
                    if (bookmark.ResourceType == TypeID.Directory && bookmark.Title == pathHierarchy)
                        return true;
                    else
                        return false;
                }))
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Returns the directory the link is residing.
        /// </summary>
        /// <returns>Returns relative the path to the directory.  Returns null if not found.</returns>
        private string GetParentDirectory()
        {
            // Don't process the first row of the database since that is the root
            if (this.Parent != 0)
            {
                int index = GetBookmarkListIndexFromID(this.Parent);

                switch (BookmarksList[index].Title)
                {
                    case null:
                    case "":
                    case DBR_BookmarksMenu:
                        return null;

                    case DBR_BookmarksToolbar:
                        return "\\" + IE_Favorite.FavoritesBar;

                    case DBR_UnsortedBookmarks:
                        return "\\" + DBR_Mapping_UnsortedBookmarks;

                    default:
                        // Recursively walk up the directory tree to fill up to generate the path  
                        return BookmarksList[index].GetParentDirectory() + "\\" + BookmarksList[index].Title;
                }
            }

            return null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Opens the connection to the firefox database.
        /// </summary>
        public static void DBOpenConnection()
        {
            if (!DBInfo.dbConnectionOpen)
            {
                DBInfo.dbConnectionOpen = true;

                // Initialization
                DBInfo.SQLFactory = new System.Data.SQLite.SQLiteFactory();
                DBInfo.SQLConnection = DBInfo.SQLFactory.CreateConnection();
                DBInfo.SQLConnection.ConnectionString = @"Data Source=" + CommandLine.FFProfilePath + @"\" + BookmarksFile;
                DBInfo.SQLConnection.Open();
            }
        }

        /// <summary>
        /// Closes the database connection to the firefox database.
        /// </summary>
        public static void DBCloseConnection()
        {
            if (DBInfo.dbConnectionOpen)
            {
                DBInfo.dbConnectionOpen = false;
                DBInfo.SQLConnection.Close();
            }
        }

        #region ILinkData Methods

        /// <summary>
        /// Evaluates the favorite to see if they have the same title, url, and path heirarchy.
        /// </summary>
        /// <param name="bookmark">The favorite to compare to.</param>
        /// <returns>Returns true if they are the same.</returns>
        public bool IsCongruentTo(ILinkData favorite)
        {
            // Since the directories that store the links on the toolbars in the browsers are diffrent names, map the names here for checking congruence.
            string favoriteTitle = favorite.Title == IE_Favorite.FavoritesBar ? DBR_BookmarksToolbar : favorite.Title;

            switch (favorite.ResourceType)
            {
                case TypeID.Link:
                    return favorite.ResourceType == this.ResourceType && favorite.URL == this.URL && favoriteTitle == this.Title && favorite.PathHierarchy == this.PathHierarchy;
                case TypeID.Directory:
                    return favorite.ResourceType == this.ResourceType && favoriteTitle == this.Title && favorite.PathHierarchy == this.PathHierarchy;
                case TypeID.Null:
                    return favorite.ResourceType == this.ResourceType && favorite.URL == this.URL && favoriteTitle == this.Title && favorite.PathHierarchy == this.PathHierarchy;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Iterates over the bookmarks list to see if the link data exists.
        /// </summary>
        /// <param name="link">The link data to compare for.</param>
        /// <returns>Returns true if the same data is in the bookmarks list.</returns>
        public static bool LinkDataExists(ILinkData link)
        {
            foreach (ILinkData bookmark in BookmarksList)
                if (bookmark.IsCongruentTo(link))
                    return true;
            return false;
        }

        /// <summary>
        /// Returns true if the bookmark is a child (in the path hierarchy) of the link.
        /// </summary>
        /// <param name="parentID">The link to compare to.</param>
        /// <returns>True if the bookmark is a child of that link.</returns>
        public bool IsChildOf(ILinkData link)
        {
            // Only need to evaluate directories since there can't be any childs of link types.
            if (!(link.ResourceType == TypeID.Directory))
                return false;
            else
            {
                if (this.PathHierarchy == null ? false : this.PathHierarchy.Contains(link.Title))
                    return true;
                else
                    return false;
            }
        }

        #endregion

        /// <summary>
        /// This reads all the bookmarks from the database to store into the list.
        /// </summary>
        /// <param name="FFProfilePath">The path to the profile folder.</param>
        /// <exception cref="Exception">Throws exceptionDBNotOpen if the database is not open.</exception>
        // Extra information here: http://stackoverflow.com/questions/2077293/connecting-to-a-sqlite-database-from-visual-studio-2008
        public static void EnumerateProfileBookmarks()
        {
            if (!DBInfo.dbConnectionOpen)
                throw new Exception(exceptionDBNotOpen);

            // Keeps the list of invalid bookmark titles
            List<FF_Bookmark> invalidBookmarkTitles = new List<FF_Bookmark>();

            // Since the url's of the database are stored in a different table, there will have to be two queries to enumerate all the data.
            #region moz_places query

            System.Data.Common.DbCommand SQLCommand = DBInfo.SQLFactory.CreateCommand();
            SQLCommand.Connection = DBInfo.SQLConnection;
            SQLCommand.CommandText = "SELECT id,url FROM moz_places";
            System.Data.Common.DbDataReader DBReader = SQLCommand.ExecuteReader();

            // Store the URL's and the ID's
            URLData URLList = new URLData();
            while (DBReader.Read())
            {
                long? ID = DBReadTableData(ref DBReader, "id");
                string URL = DBReader["url"].ToString();
                URLList.Add(ID, URL);
            }

            #endregion
            
            #region moz_bookmarks query

            // Read type 2 (directories) first to stop any link id's being out of order.  For directories id's being out of order, just deal with it in this query.
            #region moz_bookmarks query: Directories

            // Store the read DB rows in this list since if they are constructed via FF_Bookmark type, the parents may not have already been added to the list for lookup.
            // So avoid the exception by building a object construction order that will prevent the exception.
            List<Tuple<string, long?, long?, long?, long?, long?, string>> directories = new List<Tuple<string, long?, long?, long?, long?, long?, string>>();

            SQLCommand = DBInfo.SQLFactory.CreateCommand();
            SQLCommand.Connection = DBInfo.SQLConnection;
            SQLCommand.CommandText = "SELECT * FROM moz_bookmarks WHERE type = 2";
            DBReader = SQLCommand.ExecuteReader();

            while (DBReader.Read())
            {
                long? ID = DBReadTableData(ref DBReader, "id");
                long? Type = DBReadTableData(ref DBReader, "type");
                long? FK = DBReadTableData(ref DBReader, "fk");
                long? Parent = DBReadTableData(ref DBReader, "parent");
                long? Position = DBReadTableData(ref DBReader, "position");
                string Title = DBReader["title"].ToString();

                string URL = URLList[FK];
                directories.Add(new Tuple<string, long?, long?, long?, long?, long?, string>(URL, ID, Type, FK, Parent, Position, Title));
            }

            // Now build a construction order list
            List<int> constructionOrder = new List<int>();

            // Find root before building the list.
            constructionOrder.Add(directories.FindIndex(
                (entry) =>
                {
                    // (ID)
                    if(entry.Item2 == 1)
                        return true;
                    else
                        return false;
                }));

            List<Tuple<string, long?, long?, long?, long?, long?, string>> firstLevelList;
            int lastListCount;
            List<Tuple<string, long?, long?, long?, long?, long?, string>> nthLevelList = directories.ToList();
            
            do
            {
                // Trim down the list for each item added to the construction order
                lastListCount = nthLevelList.Count;
                firstLevelList = nthLevelList;
                nthLevelList = new List<Tuple<string, long?, long?, long?, long?, long?, string>>();

                foreach (Tuple<string, long?, long?, long?, long?, long?, string> item in firstLevelList)
                {
                    // (ID)
                    if (item.Item2 == 1)
                        continue;

                    // If the index exists in the constructionOrder list, then add it.  Otherwise, put it on hold for the next call.
                    if (constructionOrder.Exists(
                        (index) =>
                        {
                            // (ID) == (Parent)
                            if (directories[index].Item2 == item.Item5)
                                return true;
                            else
                                return false;
                        }))
                        constructionOrder.Add(directories.IndexOf(item));
                    else
                        nthLevelList.Add(item);
                }

                // Check to make sure the number of items are going down on the nthLevelList, so there will not be infinite recursion.  (Could happen via corrupted bookmarks)
            } while (nthLevelList.Count != 0 && nthLevelList.Count != lastListCount);

            
            // Now finish constructing the bookmark
            foreach (int index in constructionOrder)
            {
                Tuple<string, long?, long?, long?, long?, long?, string> item = directories[index];
                FF_Bookmark bookmark = new FF_Bookmark(item.Item1, item.Item2, item.Item3, item.Item4, item.Item5, item.Item6, item.Item7);
                //System.Windows.Forms.MessageBox.Show(string.Format("Title: {0}\nParent: {1}",bookmark.Title,bookmark.GetParentDirectory()));
                BookmarksList.Add(bookmark);

                // If the bookmark contains invalid path caracters or is to long of a path, then save it to the list to deal with it later.
                bool addToInvalidList = false;

                foreach (char c in System.IO.Path.GetInvalidPathChars())
                    if (bookmark.Title.Contains(c))
                    {
                        bookmark.Title = bookmark.Title.Replace(c.ToString(), "");
                        addToInvalidList = true;
                    }

                if (bookmark.MapBookmarkToPath().Length > 250)
                {
                    // First, try to truncate the title, if that can not be done, then assign the bookmark to the next highest parent.
                    while (bookmark.MapBookmarkToPath(false).Length > 240)
                        bookmark.Parent = BookmarksList[GetBookmarkListIndexFromID(bookmark.Parent)].Parent;

                    if(bookmark.MapBookmarkToPath().Length > 250)
                    {
                        int truncateLength = bookmark.MapBookmarkToPath().Length - 250;
                        bookmark.Title = bookmark.Title.Substring(0, bookmark.Title.Length - truncateLength);
                    }
                }

                if (addToInvalidList)
                    invalidBookmarkTitles.Add(bookmark);


                // Not Implemented fields.  (TFS WI-6)
                //DateTime DateAdded = (int)Reader["dateAdded"];
                //DateTime LastModified { get; set; }
                //Guid GUID { get; set; }
            }

            #endregion

            #region moz_bookmarks query: Links

            SQLCommand = DBInfo.SQLFactory.CreateCommand();
            SQLCommand.Connection = DBInfo.SQLConnection;
            SQLCommand.CommandText = "SELECT * FROM moz_bookmarks WHERE type = 1 OR type = 3";
            System.Data.Common.DbDataReader ReaderBookmarks = SQLCommand.ExecuteReader();

            while (ReaderBookmarks.Read())
            {
                long? ID = DBReadTableData(ref ReaderBookmarks, "id");
                long? Type = DBReadTableData(ref ReaderBookmarks, "type");
                long? FK = DBReadTableData(ref ReaderBookmarks, "fk");
                long? Parent = DBReadTableData(ref ReaderBookmarks, "parent");
                long? Position = DBReadTableData(ref ReaderBookmarks, "position");
                string Title = ReaderBookmarks["title"].ToString();
                
                string URL = URLList[FK];

                FF_Bookmark bookmark = new FF_Bookmark(URL, ID, Type, FK, Parent, Position, Title);
                //System.Windows.Forms.MessageBox.Show(string.Format("Title: {0}\nParent: {1}",bookmark.Title,bookmark.GetParentDirectory()));
                BookmarksList.Add(bookmark);

                // If the bookmark contains invalid path caracters or is to long of a path, then save it to the list to deal with it later.
                bool addToInvalidList = false;

                foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                    if (bookmark.Title.Contains(c))
                    {
                        bookmark.Title = bookmark.Title.Replace(c.ToString(),"");
                        addToInvalidList = true;
                    }
 

                if (bookmark.MapBookmarkToPath().Length > 250)
                {
                    // First, try to truncate the title, if that can not be done, then assign the bookmark to the next highest parent.
                    while (bookmark.MapBookmarkToPath(false).Length > 240)
                        bookmark.Parent = BookmarksList[GetBookmarkListIndexFromID(bookmark.Parent)].Parent;

                    if(bookmark.MapBookmarkToPath().Length > 250)
                    {
                        int truncateLength = bookmark.MapBookmarkToPath().Length - 250;
                        bookmark.Title = bookmark.Title.Substring(0, bookmark.Title.Length - truncateLength);
                    }
                }

                if (addToInvalidList)
                    invalidBookmarkTitles.Add(bookmark);


                // Not Implemented fields.  (TFS WI-6)
                //DateTime DateAdded = (int)Reader["dateAdded"];
                //DateTime LastModified { get; set; }
                //Guid GUID { get; set; }
            }

            #endregion

            #endregion

            // Now fix the titles in the database
            foreach (FF_Bookmark bookmark in invalidBookmarkTitles)
            {
                DBRemoveRow(bookmark);
                DBCreateRow(bookmark.Type,bookmark.Title,bookmark.Parent,bookmark.URL,bookmark.FK,bookmark.Position,bookmark.ID);
            }
        }


        /// <summary>
        /// Creates a bookmark and inserts it into the database.
        /// </summary>
        /// <param name="path">Favorite to convert.</param>
        /// <exception cref="Exception">Throws exceptionDBNotOpen if the database is not open.</exception>
        public static void ConvertToBookmark(IE_Favorite favorite)
        {
            // Don't convert the favorites bar directory as that always exists
            if(!favorite.IsExcluded && favorite.Title != IE_Favorite.FavoritesBar)
            {
                // Make sure the entry does not allready exist
                foreach (FF_Bookmark bookmark in BookmarksList)
                    if (favorite.IsCongruentTo(bookmark))
                        return;

                if (!DBInfo.dbConnectionOpen)
                    throw new Exception(exceptionDBNotOpen);

                // It may be possible that the favorite that is about to be converted does not have its parent directories allready converted yet.
                if (!FavoriteDirectoryHierarchyInDB(favorite))
                    throw new Exception(exceptionHierarchyCorruption + favorite.ToString());

                long? parentID = null;
                long? type = (long?)(favorite.ResourceType == TypeID.Link ? FF_Bookmark.BookmarkType.Link : (favorite.ResourceType == TypeID.Directory ?
                    FF_Bookmark.BookmarkType.Directory : 0));

                if (favorite.PathHierarchy == null)
                {
                    parentID = (long)FF_Bookmark.BookmarksList.Find((bookmark) =>
                    {
                        if (bookmark.ResourceType == TypeID.Directory && bookmark.Title == DBR_BookmarksMenu)
                            return true;
                        else
                            return false;
                    }).ID;
                }
                else
                {
                    // Map the favorite bar directory to the bookmarks toolbar directory
                    string pathHierarchy = System.IO.Path.GetFileNameWithoutExtension(favorite.PathHierarchy) == IE_Favorite.FavoritesBar ? 
                        DBR_BookmarksToolbar : System.IO.Path.GetFileNameWithoutExtension(favorite.PathHierarchy);

                    parentID = (long)FF_Bookmark.BookmarksList.Find((bookmark) =>
                    {
                        if (bookmark.ResourceType == TypeID.Directory && bookmark.Title == pathHierarchy)
                            return true;
                        else
                            return false;
                    }).ID;
                }
            
                //System.Windows.Forms.MessageBox.Show("creating " + System.IO.Path.GetFileNameWithoutExtension(favorite.Path));
                DBCreateRow(type, System.IO.Path.GetFileNameWithoutExtension(favorite.Path), parentID, favorite.URL);
            }
        }

        /// <summary>
        /// Removes a bookmark that has the same data as the favorite does.
        /// </summary>
        /// <param name="favorite">The favorite that has been removed.</param>
        /// <exception cref="Exception">Throws exceptionDBNotOpen if the database is not open.</exception>
        public static void RemoveBookmark(IE_Favorite favorite)
        {
            if(!favorite.IsExcluded)
            {
                if (!DBInfo.dbConnectionOpen)
                    throw new Exception(exceptionDBNotOpen);

                List<FF_Bookmark> removalList = new List<FF_Bookmark>();

                foreach (FF_Bookmark bookmark in BookmarksList)
                {
                    if (bookmark.IsCongruentTo(favorite) && !bookmark.DBSystemEntry)
                    {
                        // Add any child files to the removal list
                        if(bookmark.ResourceType == TypeID.Directory)
                            foreach (FF_Bookmark child in BookmarksList)
                                if (child != bookmark && child.IsChildOf(bookmark))
                                    removalList.Add(child);

                        removalList.Add(bookmark);
                        break;
                    }
                }

                foreach (FF_Bookmark bookmark in removalList)
                {
                    DBRemoveRow(bookmark);
                    BookmarksList.Remove(bookmark);
                }
            }
        }

        /// <summary>
        /// Creates a string that maps the path hiearachy to the favorites directory.
        /// </summary>
        /// <returns>The path that would represent the directory/link in the favorites directory.</returns>
        public string MapBookmarkToPath(bool includeTitle = true)
        {
            // Path hiearchies don't have a '\' suffix in the string, so add them if needed.
            if (includeTitle)
                return CommandLine.IEFavoritesPath +
                    (this.PathHierarchy == null ? null : this.PathHierarchy + "\\") + this.Title;
            else
                return CommandLine.IEFavoritesPath +
                    (this.PathHierarchy == null ? null : this.PathHierarchy + "\\");
        }

        /// <summary>
        /// Prints the property values of the FF_Bookmark object.
        /// </summary>
        /// <returns>Information about the object properties</returns>
        public override string ToString()
        {
            return string.Format("FF_Bookmark LinkData: ResourceType={0} : Title={1} : URL={2} : PathHierarchy={3}", this.ResourceType, this.Title, this.URL, this.PathHierarchy);
            //return "FF_Bookmark: Title = " + this.Title + " | URL = " + this.URL;
            //this.LastModified + " | DateAdded = " + this.DateAdded;
        }

        #endregion

        #endregion

    }

    
}



