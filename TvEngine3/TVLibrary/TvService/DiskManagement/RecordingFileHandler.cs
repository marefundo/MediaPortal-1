#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using TvThumbnails;
using TvDatabase;
using TvLibrary.Log;

namespace TvService
{
  public class RecordingFileHandler
  {
    /// <summary>
    /// Deletes a recording from the disk where it has been saved.
    /// When recording, a unique filename is generated by concatening a underscore
    /// and number to the file name(for example &lt;title - channel&gt;_1). That unique file
    /// name is stored in the database and should
    /// be used as base when deleting files related to the recording. 
    /// Thus all files with exactly the same base name but with _any_ extension
    /// will be deleted(for example the releated matroska .xml file)
    /// If the above results in an empty folder, it is also removed.
    /// </summary>
    /// <param name="fileNameForRec">The recording we want to delete the files for.</param>
    /// <param name="wasPendingDeletionAdded">true if a pending deletion was added</param>
    public static bool DeleteRecordingOnDisk(string fileNameForRec, out bool wasPendingDeletionAdded)
    {
      wasPendingDeletionAdded = false;
      Log.Debug("DeleteRecordingOnDisk: '{0}'", fileNameForRec);
      bool filesDeleted = false;
      try
      {
        // Check if directory exists first, otherwise GetFiles throws an error
        if (Directory.Exists(Path.GetDirectoryName(fileNameForRec)))
        {
          // Find and delete all files with same name(without extension) in the recording dir
          DeleteAllRelatedFiles(fileNameForRec);
          try
          {
            CleanRecordingFolders(fileNameForRec);
          }
          catch (Exception)
          {
            wasPendingDeletionAdded = true;
          }
          filesDeleted = true;
        }
      }
      catch (Exception ex)
      {
        Log.Error("RecordingFileHandler: Error while deleting a recording from disk: {0}", ex.Message);
        wasPendingDeletionAdded = true;
        filesDeleted = false;
      }

      if (wasPendingDeletionAdded)
      {
        AddRecordingToPendingDeletion(fileNameForRec);
      }

      return filesDeleted;
    }

    /// <summary>
    /// Deletes a recording from the disk where it has been saved.
    /// When recording, a unique filename is generated by concatening a underscore
    /// and number to the file name(for example &lt;title - channel&gt;_1). That unique file
    /// name is stored in the database and should
    /// be used as base when deleting files related to the recording. 
    /// Thus all files with exactly the same base name but with _any_ extension
    /// will be deleted(for example the releated matroska .xml file)
    /// If the above results in an empty folder, it is also removed.
    /// </summary>
    /// <param name="fileNameForRec">The recording we want to delete the files for.</param>
    public static bool DeleteRecordingOnDisk(string fileNameForRec)
    {
      bool wasPendingDeletionAdded = false;
      return (DeleteRecordingOnDisk(fileNameForRec, out wasPendingDeletionAdded));
    }

    private static void AddRecordingToPendingDeletion(string fileNameForRec)
    {
      try
      {
        bool doesPendingDeletionExist = (PendingDeletion.Retrieve(fileNameForRec) != null);
        if (!doesPendingDeletionExist)
        {
          Log.Error("RecordingFileHandler: adding filename to list of pending deletions: {0}", fileNameForRec);
          PendingDeletion addNewPendingDeletion = new PendingDeletion(fileNameForRec);
          addNewPendingDeletion.Persist();
        }
      }
      catch (Exception ex2)
      {
        Log.Error("DeleteRecordingOnDisk - tried to add to list of pending deletions exception={0}, filename={1}",
                  ex2.Message, fileNameForRec);
      }
    }

    private static void DeleteAllRelatedFiles(string fileNameForRec)
    {
      string[] relatedFiles =
        Directory.GetFiles(Path.GetDirectoryName(fileNameForRec),
                           Path.GetFileNameWithoutExtension(fileNameForRec) + @".*");

      foreach (string fileName in relatedFiles)
      {
        Log.Debug(" - deleting '{0}'", fileName);
        // File.Delete will _not_ throw on "File does not exist"
        File.Delete(fileName);
      }

      string thumbNail = string.Format("{0}\\{1}{2}",
        Thumbs.ThumbnailFolder,
        Path.GetFileNameWithoutExtension(fileNameForRec),
        ".jpg");
      Log.Debug(" - deleting '{0}'", thumbNail);
      File.Delete(thumbNail);
    }

    /// <summary>
    /// When deleting a recording we check if the folder the recording
    /// was deleted from can be deleted.
    /// A folder must not be deleted, if there are still files or subfolders in it.
    /// </summary>
    /// <param name="fileName">The recording file which is deleted.</param>
    private static void CleanRecordingFolders(string fileName)
    {
      try
      {
        Log.Debug("RecordingFileHandler: Clean orphan recording dirs for {0}", fileName);
        string recfolder = Path.GetDirectoryName(fileName);
        List<string> recordingPaths = GetRecordingPaths();

        Log.Debug("RecordingFileHandler: Checking {0} path(s) for cleanup", Convert.ToString(recordingPaths.Count));
        foreach (string checkPath in recordingPaths)
        {
          if (checkPath != string.Empty && checkPath != Path.GetPathRoot(checkPath))
          {
            // make sure we're only deleting directories which are "recording dirs" from a tv card
            if (fileName.Contains(checkPath))
            {
              Log.Debug("RecordingFileHandler: Origin for recording {0} found: {1}", Path.GetFileName(fileName),
                        checkPath);
              string deleteDir = recfolder;
              // do not attempt to step higher than the recording base path
              while (deleteDir != Path.GetDirectoryName(checkPath) && deleteDir.Length > checkPath.Length)
              {
                try
                {
                  bool hasSubDirs = HasSubDirs(deleteDir);

                  if (!hasSubDirs)
                  {
                    string[] files = Directory.GetFiles(deleteDir);
                    bool hasDirAnyImportantFiles = HasDirAnyImportantFiles(deleteDir, files);
                    if (hasDirAnyImportantFiles)
                    {
                      return;
                    }

                    bool hasDirAnyFilesOver1MBTotal = HasDirAnyFilesOver1MBTotal(deleteDir, files);
                    if (hasDirAnyFilesOver1MBTotal)
                    {
                      return;
                    }

                    DeleteFiles(files);
                    deleteDir = DeleteDir(deleteDir);
                  }
                  else
                  {
                    return;
                  }
                }
                catch (Exception ex1)
                {
                  Log.Info("RecordingFileHandler: Could not delete directory {0} - {1}", deleteDir, ex1.Message);
                  throw; //make sure its added to the pending deletions list later on.                  
                }
              }
            }
          }
          else
          {
            Log.Debug("RecordingFileHandler: Path not valid for removal - {1}", checkPath);
            return;
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error("RecordingFileHandler: Error cleaning the recording folders - {0},{1}", ex.Message, ex.StackTrace);
        throw; //make sure its added to the pending deletions list later on.                  
      }
    }

    private static bool HasSubDirs(string deleteDir)
    {
      string[] subdirs = Directory.GetDirectories(deleteDir);
      bool hasSubDirs = (subdirs.Length > 0);

      if (hasSubDirs)
      {
        Log.Debug("RecordingFileHandler: Found {0} sub-directory(s) in recording path - not cleaning {1}",
                  Convert.ToString(subdirs.Length), deleteDir);
      }
      return hasSubDirs;
    }

    private static string DeleteDir(string deleteDir)
    {
      Directory.Delete(deleteDir);
      Log.Debug("RecordingFileHandler: Deleted empty recording dir - {0}", deleteDir);
      DirectoryInfo di = Directory.GetParent(deleteDir);
      if (di != null)
      {
        deleteDir = di.FullName;
      }
      return deleteDir;
    }

    private static void DeleteFiles(string[] files)
    {
      foreach (string file in files)
      {
        File.Delete(file); //delete all files
      }
    }

    private static bool HasDirAnyFilesOver1MBTotal(string deleteDir, string[] files)
    {
      long fileSizesTotal = 0;
      bool hasDirAnyFilesOver1MBTotal = false;
      foreach (string file in files)
      {
        FileInfo info = new FileInfo(file);
        fileSizesTotal += info.Length;

        hasDirAnyFilesOver1MBTotal = (fileSizesTotal >= 1048576);
        if (hasDirAnyFilesOver1MBTotal)
        {
          Log.Debug(
            "RecordingFileHandler: Found {0} bytes worth of data (max 1mb) in directory in recording path - not cleaning {1}",
            fileSizesTotal, deleteDir);
          break;
        }
      }

      return hasDirAnyFilesOver1MBTotal;
    }

    private static bool HasDirAnyImportantFiles(string deleteDir, string[] files)
    {
      bool hasDirAnyImportantFiles = false;
      foreach (string fullFilename in files)
      {
        string fileName = Path.GetFileName(fullFilename);
        hasDirAnyImportantFiles = (!fileName.ToLower().Equals("thumbs.db"));

        if (hasDirAnyImportantFiles)
        {
          Log.Debug(
            "RecordingFileHandler: Found irrelevant file {0} in recording path {1} - not cleaning",
            fileName, deleteDir);
          break;
        }
      }

      return hasDirAnyImportantFiles;
    }

    private static List<string> GetRecordingPaths()
    {
      List<string> recordingPaths = new List<string>();
      IList<Card> cards = Card.ListAll();
      foreach (Card card in cards)
      {
        string currentCardPath = card.RecordingFolder;
        if (!recordingPaths.Contains(currentCardPath))
        {
          recordingPaths.Add(currentCardPath);
        }
      }
      return recordingPaths;
    }
  }
}