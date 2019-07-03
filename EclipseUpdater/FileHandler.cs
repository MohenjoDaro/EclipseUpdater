using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace EclipseUpdater
{
    class DirectoryHandler
    {
        public static bool CheckDirectory(string pathTarget)
        {
            return Directory.Exists(pathTarget);
        }

        public static bool CreateDirectory(string pathTarget)
        {
            try
            {
                Directory.CreateDirectory(pathTarget);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool MoveDirectory(string pathTarget, string pathDestination)
        {
            try
            {
                Directory.Move(pathTarget, pathDestination);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DestroyDirectory(string pathTarget)
        {
            try
            {
                Directory.Delete(pathTarget);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    class FileHandler
    {
        public static bool CheckFile(string pathTarget)
        {
            return File.Exists(pathTarget);
        }

        public static bool ExtractFile(string pathTarget, string pathExtractTo)
        {
            return true;
        }

        public static bool MoveFile(string pathTarget, string pathDestination)
        {
            try
            {
                File.Move(pathTarget, pathDestination);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DestroyFile(string pathTarget)
        {
            try
            {
                File.Delete(pathTarget);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
