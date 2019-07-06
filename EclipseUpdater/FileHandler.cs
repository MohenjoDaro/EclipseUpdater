using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace EclipseUpdater
{
    class DirectoryHandler
    {
        public static bool CreateDirectory(string pathTarget)
        {
            try {
                Directory.CreateDirectory(pathTarget);
                return true;
            } catch {
                return false;
            }
        }

        public static bool MoveDirectory(string pathTarget, string pathDestination, bool onlyChildren = false)
        {
            try {
                if (onlyChildren)
                {
                    // Move all sub directories and files
                    foreach (string dir in Directory.EnumerateDirectories(pathTarget))
                    {
                        Directory.Move(Path.Combine(pathTarget, dir), pathDestination);
                    }
                    foreach (string file in Directory.EnumerateFiles(pathTarget))
                    {
                        FileHandler.MoveFile(Path.Combine(pathTarget, file), pathDestination);
                    }
                }
                else
                {
                    Directory.Move(pathTarget, pathDestination);
                }
                return true;
            } catch {
                return false;
            }
        }

        public static bool RenameDirectory(string pathTarget, string nameDirectory)
        {
            try {
                Directory.Move(pathTarget, nameDirectory);
                return true;
            } catch {
                return false;
            }
        }

        public static bool MarkForDeletionDirectory(string pathTarget)
        {
            try {
                RenameDirectory(pathTarget, pathTarget + ".del");
                return true;
            } catch {
                return false;
            }
        }

        public static bool DestroyDirectory(string pathTarget)
        {
            try {
                Directory.Delete(pathTarget);
                return true;
            } catch {
                return false;
            }
        }

        public static bool DestroyMarkedForDeletion(string pathTarget)
        {
            try {
                foreach (string dir in Directory.EnumerateDirectories(pathTarget, "*.del"))
                {
                    DestroyDirectory(Path.Combine(pathTarget, dir));
                }

                foreach (string file in Directory.EnumerateFiles(pathTarget, "*.del"))
                {
                    FileHandler.DestroyFile(Path.Combine(pathTarget, file));
                }

                return true;
            } catch {
                return false;
            }
        }
    }

    class FileHandler
    {
        public static bool ExtractFile(string pathTarget, string pathExtractTo)
        {
            return true;
        }

        public static bool MoveFile(string pathTarget, string pathDestination)
        {
            try {
                File.Move(pathTarget, pathDestination);
                return true;
            } catch {
                return false;
            }
        }

        public static bool RenameFile(string pathTarget, string nameFile)
        {
            try {
                File.Move(pathTarget, Path.Combine(Path.GetDirectoryName(pathTarget), nameFile));
                return true;
            } catch {
                return false;
            }
        }

        public static bool MarkForDeletionFile(string pathTarget)
        {
            try {
                RenameFile(pathTarget, pathTarget + ".del");
                return true;
            } catch {
                return false;
            }
        }

        public static bool DestroyFile(string pathTarget)
        {
            try {
                File.Delete(pathTarget);
                return true;
            } catch {
                return false;
            }
        }
    }

    class UpdateHandler
    {
        public static string[] CheckForUpdate(int idProject)
        {
            try {
                return null; // array of needed updates
            } catch {
                return null;
            }
        }

        public static bool DownloadUpdate(string pathTarget, int idProject, string idUpdate)
        {
            try {
                return true;
            } catch {
                return false;
            }
        }
    }
}
