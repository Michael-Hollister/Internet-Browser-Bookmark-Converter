using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
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
    /// Represents a Internet Explorer favorite.
    /// </summary>
    [Serializable]
    public class IE_Favorite : ILinkData
    {

        #region Fields

        // Constants

        // The directory name of the place where Internet Explorer stores the favorites bar links.
        // NOTE: Links is the directory name for 'Favorites Bar'.
        public const string FavoritesBar = "Links"; // "Favorites Bar";

        // Exception text
        private const string exceptionHierarchyCorruption = "The parent level directories does not exist for ";

        // Variables

        /// <summary>
        /// Stores all the favorites in the favorites directory.
        /// </summary>
        public static List<IE_Favorite> FavoritesList = new List<IE_Favorite>();

        #region Properties

        public string Path { get; set; }

        #region ILinkData Members

        public string URL { get; set; }
        public string Title { get; set; }

        /// <summary>
        /// Contains the relative path from the favorites folder to the parent directory of the favorite.
        /// </summary>
        public string PathHierarchy { get; set; }

        /// <summary>
        /// Indicates the type of resource if it is a directory or a link.
        /// </summary>
        public TypeID ResourceType { get; set; }

        /// <summary>
        /// Indicates if the favorite is excluded from operations being performed on it.
        /// </summary>
        public bool IsExcluded { get; set; }

        #endregion

        /// <summary>
        /// Not Implemented.  (TFS WI-5)
        /// </summary>
        public DateTime LastModified { get; set; }
        /// <summary>
        /// Not Implemented.  (TFS WI-5)
        /// </summary>
        public DateTime DateAdded { get; set; }

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an IE_Favorite object that represents an Internet Explorer favorite or directory.
        /// </summary>
        /// <param name="path">The full path to the resource.</param>
        /// <param name="url">The link that the favorite links to.</param>
        /// <param name="type">The type of resource.  (Link or a directory)</param>
        /// <param name="lastModified">Not Implemented.  (TFS WI-5)</param>
        /// <param name="dateAdded">Not Implemented.  (TFS WI-5)</param>
        public IE_Favorite(string path, string url, TypeID type, DateTime lastModified, DateTime dateAdded)
        {
            this.Path = path;
            this.URL = url;
            this.LastModified = lastModified;
            this.DateAdded = dateAdded;

            // This information will be used for Firefox's database comparasions
            // The conditional is used to be consistant with firefox path hiearachy that has an empty string for the root path.
            this.PathHierarchy = System.IO.Path.GetDirectoryName(path.Substring(CommandLine.IEFavoritesPath.Length)) == "" ?
                null : "\\" + System.IO.Path.GetDirectoryName(path.Substring(CommandLine.IEFavoritesPath.Length));
            this.Title = System.IO.Path.GetFileNameWithoutExtension(path);
            this.ResourceType = type;
            this.IsExcluded = false;

            foreach (string exclusionPath in CommandLine.IEFavoritesExclusions)
                if (this.Path.Contains(path))
                    this.IsExcluded = true;
        }

        #endregion

        #region Methods

        #region ILinkData Methods

        /// <summary>
        /// Evaluates the bookmark to see if they have the same title, url, and path heirarchy.
        /// </summary>
        /// <param name="bookmark">The bookmark to compare to.</param>
        /// <returns>Returns true if they are the same.</returns>
        public bool IsCongruentTo(ILinkData bookmark)
        {
            switch (bookmark.ResourceType)
            {
                case TypeID.Link:
                    return bookmark.ResourceType == this.ResourceType && bookmark.URL == this.URL && bookmark.Title == this.Title && bookmark.PathHierarchy == this.PathHierarchy;
                case TypeID.Directory:
                    return bookmark.ResourceType == this.ResourceType && bookmark.Title == this.Title && bookmark.PathHierarchy == this.PathHierarchy;
                case TypeID.Null:
                    return bookmark.ResourceType == this.ResourceType && bookmark.URL == this.URL && bookmark.Title == this.Title && bookmark.PathHierarchy == this.PathHierarchy;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns true if the favorite is a child (in the path hierarchy) of the link.
        /// </summary>
        /// <param name="parentID">The link to compare to.</param>
        /// <returns>True if the favorite is a child of that link.</returns>
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

        /// <summary>
        /// Iterates over the favorites list to see if the link data exists.
        /// </summary>
        /// <param name="link">The link data to compare for.</param>
        /// <returns>Returns true if the same data is in the favorites list.</returns>
        public static bool LinkDataExists(ILinkData link)
        {
            foreach (ILinkData favorite in FavoritesList)
                if (favorite.IsCongruentTo(link))
                    return true;
            return false;
        }

        #endregion

        /// <summary>
        /// This reads all the favorites from disk to store into the list.
        /// </summary>
        /// <param name="IEFavoritesPath">The path to the favorites folder.</param>
        public static void EnumerateFavoritesDirectory()
        {
            #region Directories

            // The reason why this is not done in parallel is for the issue if directories were not added in the proper tree order.
            foreach (string directory in Directory.EnumerateDirectories(CommandLine.IEFavoritesPath, "*", SearchOption.AllDirectories))
            {
                // The metadata is currently not used.  (TFS WI-5)
                FavoritesList.Add(new IE_Favorite(
                        directory,
                        null,
                        TypeID.Directory,
                        File.GetLastWriteTime(directory),
                        File.GetCreationTime(directory)));
            }

            #endregion

            #region Links

            Parallel.ForEach<string>(Directory.EnumerateFiles(CommandLine.IEFavoritesPath, "*.url", SearchOption.AllDirectories),
                (favorite) =>
                {
                    // The metadata is currently not used.  (TFS WI-5)
                    FavoritesList.Add(new IE_Favorite(
                            favorite,
                            Shortcut.GetInternetShortcutTarget(favorite),
                            TypeID.Link,
                            File.GetLastWriteTime(favorite),
                            File.GetCreationTime(favorite)));
                });
            
            #endregion

        }

        /// <summary>
        /// Creates a favorite from a bookmark.
        /// </summary>
        /// <param name="path">Bookmark to convert.</param>
        /// <exception cref="Exception">Throws exceptionHierarchyCorruption if the parent level directories don't exist.</exception>
        public static void ConvertToFavorite(FF_Bookmark bookmark)
        {
            switch (bookmark.ResourceType)
            {
                case TypeID.Link:
                    if (!bookmark.DBSystemEntry && !bookmark.IsExcluded)
                    {
                        // If the hieararchy is out of order, then just throw an exception
                        if (!Directory.Exists(bookmark.MapBookmarkToPath(false)))
                            throw new Exception(exceptionHierarchyCorruption + bookmark.ToString());
                            
                        Shortcut.CreateInternetShortcut(bookmark.MapBookmarkToPath() + ".url", bookmark.URL);
                        FavoritesList.Add(new IE_Favorite(bookmark.MapBookmarkToPath(), bookmark.URL, TypeID.Link
                            , File.GetLastWriteTime(bookmark.MapBookmarkToPath()), File.GetCreationTime(bookmark.MapBookmarkToPath())));
                    }
                    break;
                
                case TypeID.Directory:
                    if (!bookmark.DBSystemEntry && !bookmark.IsExcluded)
                    {
                        Directory.CreateDirectory(bookmark.MapBookmarkToPath());
                        FavoritesList.Add(new IE_Favorite(bookmark.MapBookmarkToPath(), null, TypeID.Directory
                            , File.GetLastWriteTime(bookmark.MapBookmarkToPath()), File.GetCreationTime(bookmark.MapBookmarkToPath())));
                    }
                    break;
                
                case TypeID.Null:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Removes a favorite that has the same data as the bookmark does.
        /// </summary>
        /// <param name="bookmark">The bookmark that has been removed.</param>
        public static void RemoveFavorite(FF_Bookmark bookmark)
        {
            if (!bookmark.DBSystemEntry && !bookmark.IsExcluded)
            {
                List<IE_Favorite> removalList = new List<IE_Favorite>();

                foreach (IE_Favorite favorite in FavoritesList)
                {
                    if (favorite.IsCongruentTo(bookmark))
                    {
                        // Add any child files to the removal list
                        if(favorite.ResourceType == TypeID.Directory)
                            foreach (IE_Favorite child in FavoritesList)
                                if (child != favorite && child.IsChildOf(favorite))
                                    removalList.Add(child);

                        removalList.Add(favorite);
                        break;
                    }
                }
                
                foreach (IE_Favorite favorite in removalList)
                {
                    switch (favorite.ResourceType)
                    {
                        case TypeID.Link:
                            // Check for path existance since it might have been already deleated via a directory delete
                            if (System.IO.File.Exists(favorite.Path))
                                System.IO.File.Delete(favorite.Path);
                            break;
                        case TypeID.Directory:
                            if (System.IO.Directory.Exists(favorite.Path))
                                System.IO.Directory.Delete(favorite.Path, true);
                            break;
                        case TypeID.Null:
                            break;
                        default:
                            break;
                    }

                    FavoritesList.Remove(favorite);
                }
            }
        }

        /// <summary>
        /// Prints the property values of the IE_Favorite object.
        /// </summary>
        /// <returns>Information about the object properties</returns>
        public override string ToString()
        {
            //return "IE_Favorite: Title = " + this.Path + " | URL = " + this.URL + " | LastModified = " + this.LastModified + " | DateAdded = " + this.DateAdded;
            return string.Format("IE_Favorite LinkData: ResourceType={0} : Title={1} : URL={2} : PathHierarchy={3}",this.ResourceType,this.Title,this.URL,this.PathHierarchy);
        }

        #endregion

    }
}

