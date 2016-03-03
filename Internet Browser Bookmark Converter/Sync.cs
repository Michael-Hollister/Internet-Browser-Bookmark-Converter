using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
    /// Handles synchronization between IE favorites and FF bookmarks
    /// </summary>
    public static class Sync
    {
        #region Documentation

        /*
            Sync  Framework:
         * 
            -----
            OM1, OM2 = Old manifests
            M1, M2  = Manifests after conversion
            D1, D2 = Favorite/Bookmark lists
            -----

            Case 1: M1 || M2 don't exist.
	            D1=x, D2=y, M1=x, M2=y, DC1=xy, DC2=yx
            Case 2:  New files.
	            OM1=xy, OM2=xy, D1=wxy, D2=xyz, M1=wxyz, M2=wxyz
            Case 3:  Removed files.
	            OM1=xy, OM2=xy, D1=y, D2=xy, M1=y, M2=y
 
         */

        #endregion

        #region Constants

        public const string syncFileNameIE = "IBBC_SyncListIE.bin";
        public const string syncFileNameFF = "IBBC_SyncListFF.bin";

        #endregion

        #region Structs

        /// <summary>
        /// The list of missing URL's in either favorites directory or Firefox profile bookmarks.
        /// </summary>
        public struct SyncFileState
        {
            /// <summary>
            /// Constructs the needed data to know what to do in the sync operation.
            /// </summary>
            /// <param name="type">The type of file the object represents.</param>
            /// <param name="operation">The type of operation that will be performed on the entry.</param>
            /// <param name="index">Contains the data on what list the index points to.</param>
            public SyncFileState(FileType type, SyncOperation operation, ILinkData data)
            {
                Type = type;
                Operation = operation;
                Data = data;
            }

            public enum FileType { InternetExplorer, Firefox };
            public enum SyncOperation { Add, Remove };

            /// <summary>
            /// The type of file the object represents.
            /// </summary>
            public FileType Type;

            /// <summary>
            /// The type of operation that will be performed on the entry.
            /// </summary>
            public SyncOperation Operation;

            /// <summary>
            /// Contains the data on the object references.
            /// </summary>
            public ILinkData Data;
            
            /// <summary>
            /// Returns the object's contents in a string form.
            /// </summary>
            /// <returns>Returns information about the object.</returns>
            public override string ToString()
            {
                string objectStr = null;

                switch (this.Type)
                {
                    case FileType.InternetExplorer:
                        switch (this.Operation)
	                    {
		                    case SyncOperation.Add:
                                objectStr = IE_Favorite.FavoritesList[GetListIndexFromLinkData(ref IE_Favorite.FavoritesList, this.Data)].ToString();
                                break;
                            case SyncOperation.Remove:
                                objectStr = IEFavoritesManifest[GetListIndexFromLinkData(ref IEFavoritesManifest, this.Data)].ToString();
                                break;
                            default:
                            break;
	                    }
                        break;
                    case FileType.Firefox:
                        switch (this.Operation)
                        {
                            case SyncOperation.Add:
                                objectStr = FF_Bookmark.BookmarksList[GetListIndexFromLinkData(ref FF_Bookmark.BookmarksList, this.Data)].ToString();
                                break;
                            case SyncOperation.Remove:
                                objectStr = FFBookmarksManifest[GetListIndexFromLinkData(ref FFBookmarksManifest, this.Data)].ToString();
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }

                return string.Format("Type={0} : Operation={1} : Object={2}",System.Enum.GetName(typeof(FileType),this.Type),
                    System.Enum.GetName(typeof(SyncOperation), this.Operation), objectStr);
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Stores the serialized IE_Favorites list before the previous conversion operation.
        /// </summary>
        public static List<IE_Favorite> IEFavoritesManifest;

        /// <summary>
        /// Stores the serialized FF_Bookmarks list before the previous conversion operation.
        /// </summary>
        public static List<FF_Bookmark> FFBookmarksManifest;

        // Exception text
        private const string exceptionLinkDataDoesNotExist = "The link data in the list does not exist for ";


        #endregion

        #region Methods

        /// <summary>
        /// Updates the manifest files after the conversion operation.
        /// </summary>
        private static void UpdateManifests()
        {
            IE_Favorite.FavoritesList = new List<IE_Favorite>();
            IE_Favorite.EnumerateFavoritesDirectory();
            FF_Bookmark.BookmarksList = new List<FF_Bookmark>();
            FF_Bookmark.EnumerateProfileBookmarks();

            if (File.Exists(CommandLine.IEFavoritesPath + syncFileNameIE))
                File.Delete(CommandLine.IEFavoritesPath + syncFileNameIE);

            if (File.Exists(CommandLine.FFProfilePath + syncFileNameFF))
                File.Delete(CommandLine.FFProfilePath + syncFileNameFF);

            CreateManifest(CommandLine.IEFavoritesPath + syncFileNameIE,IE_Favorite.FavoritesList);
            CreateManifest(CommandLine.FFProfilePath + syncFileNameFF,FF_Bookmark.BookmarksList);
        }

        /// <summary>
        /// Returns the index of the favorite that has the same link data.
        /// </summary>
        /// <param name="link">The link data to compare for.</param>
        /// <exception cref="Exception">Throws exceptionLinkDataDoesNotExist if the id does not exist.</exception>
        /// <returns>Returns the index of the favorite that has that data.</returns>
        public static int GetListIndexFromLinkData<T>(ref List<T> list, ILinkData link)
        {
            foreach (ILinkData listObject in list)
                if (listObject.IsCongruentTo(link))
                    return list.IndexOf((T)listObject);
            throw new Exception(exceptionLinkDataDoesNotExist + link.ToString());
        }

        /// <summary>
        /// Saves the list of favorites/bookmarks that were enumerated to disk.
        /// </summary>
        /// <param name="path">The path to save the serialized list.</param>
        /// <param name="list">The list to be serialized.</param>
        public static void CreateManifest<T>(string path, List<T> list)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                formatter.Serialize(stream, list);
            }
        }

        /// <summary>
        /// Checks to see what bookmarks are missing from favorites and vise versa.
        /// </summary>
        /// <returns>Returns the list of missing files.</returns>
        public static List<SyncFileState> CompareManifestFiles()
        {
            List<Sync.SyncFileState> FileList = new List<SyncFileState>();

            // Case 1: Upon first conversion, just sync up IE favorites and FF bookmarks
            if (!File.Exists(CommandLine.IEFavoritesPath + Sync.syncFileNameIE) ||
                !File.Exists(CommandLine.FFProfilePath + Sync.syncFileNameFF))
            {
                if(File.Exists(CommandLine.IEFavoritesPath + Sync.syncFileNameIE))
                    File.Delete(CommandLine.IEFavoritesPath + Sync.syncFileNameIE);

                if(File.Exists(CommandLine.FFProfilePath + Sync.syncFileNameFF))
                    File.Delete(CommandLine.FFProfilePath + Sync.syncFileNameFF);

                // Serialize empty lists since this is the first conversion operation that has been performed on this path
                Sync.CreateManifest(CommandLine.IEFavoritesPath + syncFileNameIE, new List<IE_Favorite>());
                Sync.CreateManifest(CommandLine.FFProfilePath + syncFileNameFF, new List<FF_Bookmark>());
            }

            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream stream = new FileStream(CommandLine.IEFavoritesPath + syncFileNameIE, FileMode.Open))
            {
                IEFavoritesManifest = (List<IE_Favorite>)formatter.Deserialize(stream);
            }

            using (FileStream stream = new FileStream(CommandLine.FFProfilePath + syncFileNameFF, FileMode.Open))
            {
                FFBookmarksManifest = (List<FF_Bookmark>)formatter.Deserialize(stream);
            }

            // Sometimes null entries get serialized, so remove them to avoid exceptions
            for (int x = 0; x < IEFavoritesManifest.Count; x++)
                if (IEFavoritesManifest[x] == null)
                {
                    IEFavoritesManifest.Remove(IEFavoritesManifest[x]);
                    x--;
                }

            for (int x = 0; x < FFBookmarksManifest.Count; x++)
                if (FFBookmarksManifest[x] == null)
                {
                    FFBookmarksManifest.Remove(FFBookmarksManifest[x]);
                    x--;
                }

                #region Case 2:  Compare for new files
                // The process that is used to detect new files goes as follows.  If the manifest does not contain new file enumerated in the
                // directory, the file then has been added since the last conversion operation.

                foreach (IE_Favorite favorite in IE_Favorite.FavoritesList)
                    if (!IEFavoritesManifest.Exists((f) =>
                    {
                        switch (f.ResourceType)
                        {
                            case TypeID.Link:
                                return favorite.URL == f.URL && favorite.Title == f.Title && favorite.PathHierarchy == f.PathHierarchy;
                            case TypeID.Directory:
                                return favorite.Title == f.Title && favorite.PathHierarchy == f.PathHierarchy;
                            case TypeID.Null:
                                return favorite.ResourceType == f.ResourceType && favorite.URL == f.URL && favorite.Title == f.Title && favorite.PathHierarchy == f.PathHierarchy;
                            default:
                                return false;
                        }
                    }) && !favorite.IsExcluded)
                        FileList.Add(new SyncFileState(SyncFileState.FileType.InternetExplorer, SyncFileState.SyncOperation.Add, favorite));


            foreach (FF_Bookmark bookmark in FF_Bookmark.BookmarksList)
                if (!FFBookmarksManifest.Exists((b) => {
                    switch (b.ResourceType)
                    {
                        case TypeID.Link:
                            return bookmark.URL == b.URL && bookmark.Title == b.Title && bookmark.PathHierarchy == b.PathHierarchy;
                        case TypeID.Directory:
                            return bookmark.Title == b.Title && bookmark.PathHierarchy == b.PathHierarchy;
                        case TypeID.Null:
                            return bookmark.ResourceType == b.ResourceType && bookmark.URL == b.URL && bookmark.Title == b.Title && bookmark.PathHierarchy == b.PathHierarchy;
                        default:
                            return false;
                    }
                }) && !bookmark.DBSystemEntry && !bookmark.IsExcluded)
                    FileList.Add(new SyncFileState(SyncFileState.FileType.Firefox, SyncFileState.SyncOperation.Add, bookmark));
            
            #endregion

            #region Case 3:  Compare for removed files
            // The process that is used to remove files goes as follows.  If the enumerated directories/databsae does not contain 
            // a file that was in the manifest, then that file has been removed since the last conversion operation.

            foreach (IE_Favorite favorite in IEFavoritesManifest)
                if (!IE_Favorite.LinkDataExists((ILinkData)favorite))
                    FileList.Add(new SyncFileState(SyncFileState.FileType.InternetExplorer, SyncFileState.SyncOperation.Remove, favorite));

            foreach (FF_Bookmark bookmark in FFBookmarksManifest)
                if (!FF_Bookmark.LinkDataExists((ILinkData)bookmark))
                    FileList.Add(new SyncFileState(SyncFileState.FileType.Firefox, SyncFileState.SyncOperation.Remove, bookmark));

            #endregion

            #region Comparison of favorites directory to bookmarks database
            // Even though the manifests have been assigned operations of addition or removing, now this has to compare the favorites directory
            // to Firefox's database since there might already be some same entries in both places. 
            
            for (int x = 0; x < FileList.Count; x++)
            {
                if (FileList[x].Type == SyncFileState.FileType.InternetExplorer)
                {
                    if (FileList[x].Operation == SyncFileState.SyncOperation.Add &&
                        FF_Bookmark.LinkDataExists((ILinkData)IE_Favorite.FavoritesList[GetListIndexFromLinkData(ref IE_Favorite.FavoritesList, FileList[x].Data)]))
                    {
                        FileList.RemoveAt(x);
                        x--;
                        continue;
                    }

                    if (FileList[x].Operation == SyncFileState.SyncOperation.Remove &&
                        !FF_Bookmark.LinkDataExists((ILinkData)IEFavoritesManifest[GetListIndexFromLinkData(ref IEFavoritesManifest, FileList[x].Data)]))
                    {
                        FileList.RemoveAt(x);
                        x--;
                        continue;
                    }
                }

                if (FileList[x].Type == SyncFileState.FileType.Firefox)
                {
                    if (FileList[x].Operation == SyncFileState.SyncOperation.Add &&
                        IE_Favorite.LinkDataExists((ILinkData)FF_Bookmark.BookmarksList[GetListIndexFromLinkData(ref FF_Bookmark.BookmarksList,FileList[x].Data)]))
                    {
                        FileList.RemoveAt(x);
                        x--;
                        continue;
                    }

                    if (FileList[x].Operation == SyncFileState.SyncOperation.Remove &&
                        !IE_Favorite.LinkDataExists((ILinkData)FFBookmarksManifest[GetListIndexFromLinkData(ref FFBookmarksManifest, FileList[x].Data)]))
                    {
                        FileList.RemoveAt(x);
                        x--;
                        continue;
                    }

                }
            }

            #endregion

            return FileList;
        }


        /// <summary>
        /// Performs the conversion or removal operation on the file list.
        /// <param name="FileList">The list of the files that will be operated on.</param>
        /// </summary>
        public static void PerformFileOperations(List<Sync.SyncFileState> FileList)
        {
            foreach (Sync.SyncFileState data in FileList)
            {
                #region Internet Explorer

                if (CommandLine.IEConvert && data.Type == SyncFileState.FileType.InternetExplorer)
                {
                    if (data.Operation == SyncFileState.SyncOperation.Add)
                        FF_Bookmark.ConvertToBookmark(IE_Favorite.FavoritesList[GetListIndexFromLinkData(ref IE_Favorite.FavoritesList, data.Data)]);

                    if (data.Operation == SyncFileState.SyncOperation.Remove)
                        FF_Bookmark.RemoveBookmark(IEFavoritesManifest[GetListIndexFromLinkData(ref IEFavoritesManifest, data.Data)]);
                }

                #endregion

                #region Firefox

                if (CommandLine.FFConvert && data.Type == SyncFileState.FileType.Firefox)
                {
                    if (data.Operation == SyncFileState.SyncOperation.Add)
                        IE_Favorite.ConvertToFavorite(FF_Bookmark.BookmarksList[GetListIndexFromLinkData(ref FF_Bookmark.BookmarksList, data.Data)]);

                    if (data.Operation == SyncFileState.SyncOperation.Remove)
                        IE_Favorite.RemoveFavorite(FFBookmarksManifest[GetListIndexFromLinkData(ref FFBookmarksManifest, data.Data)]);
                }

                #endregion

            }

            UpdateManifests();
        }

        #endregion


    }
}
