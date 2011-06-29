// ****************************************************************
// Copyright 2002-2003, Charlie Poole
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************

using System;
using System.IO;
using System.Text;
using System.Reflection;
#if CLR_2_0 || CLR_4_0
using System.Collections.Generic;
#else
using System.Collections;
#endif
using System.Runtime.InteropServices;

namespace NUnit.Engine.Internal
{
	/// <summary>
	/// Static methods for manipulating project paths, including both directories
	/// and files. Some synonyms for System.Path methods are included as well.
	/// </summary> 
	public class PathUtils
	{
		public const uint FILE_ATTRIBUTE_DIRECTORY  = 0x00000010;  
		public const uint FILE_ATTRIBUTE_NORMAL     = 0x00000080;  
		public const int MAX_PATH = 256;

		protected static char DirectorySeparatorChar = Path.DirectorySeparatorChar;
		protected static char AltDirectorySeparatorChar = Path.AltDirectorySeparatorChar;

		#region Public methods

        /// <summary>
        /// Returns a boolean indicating whether the specified path
        /// is that of an assembly - that is a dll or exe file.
        /// </summary>
        /// <param name="path">Path to a file.</param>
        /// <returns>True if the file extension is dll or exe, otherwise false.</returns>
        public static bool IsAssemblyFileType(string path)
        {
            string extension = Path.GetExtension(path).ToLower();
            return extension == ".dll" || extension == ".exe";
        }

        /// <summary>
		/// Returns the relative path from a base directory to another
		/// directory or file.
		/// </summary>
		public static string RelativePath( string from, string to )
		{
			if (from == null)
				throw new ArgumentNullException (from);
			if (to == null)
				throw new ArgumentNullException (to);

            string toPathRoot = Path.GetPathRoot(to);
            if (toPathRoot == null || toPathRoot == string.Empty)
                return to;
            string fromPathRoot = Path.GetPathRoot(from);

            if (!PathsEqual(toPathRoot, fromPathRoot))
                return null;

            string fromNoRoot = from.Substring(fromPathRoot.Length);
            string toNoRoot = to.Substring(toPathRoot.Length);

            string[] _from = SplitPath(fromNoRoot);
            string[] _to = SplitPath(toNoRoot);

			StringBuilder sb = new StringBuilder (Math.Max (from.Length, to.Length));

			int last_common, min = Math.Min (_from.Length, _to.Length);
			for (last_common = 0; last_common < min;  ++last_common) 
			{
                if (!PathsEqual(_from[last_common], _to[last_common]))
                    break;
            }

			if (last_common < _from.Length)
				sb.Append ("..");
			for (int i = last_common + 1; i < _from.Length; ++i) 
			{
				sb.Append (PathUtils.DirectorySeparatorChar).Append ("..");
			}

			if (sb.Length > 0)
				sb.Append (PathUtils.DirectorySeparatorChar);
			if (last_common < _to.Length)
				sb.Append (_to [last_common]);
			for (int i = last_common + 1; i < _to.Length; ++i) 
			{
				sb.Append (PathUtils.DirectorySeparatorChar).Append (_to [i]);
			}

			return sb.ToString ();
		}

		/// <summary>
		/// Return the canonical form of a path.
		/// </summary>
		public static string Canonicalize( string path )
		{
#if CLR_2_0 || CLR_4_0
            List<string> parts = new List<string>(
#else
			ArrayList parts = new ArrayList(
#endif
				path.Split( DirectorySeparatorChar, AltDirectorySeparatorChar ) );

			for( int index = 0; index < parts.Count; )
			{
				string part = (string)parts[index];
		
				switch( part )
				{
					case ".":
						parts.RemoveAt( index );
						break;
				
					case "..":
						parts.RemoveAt( index );
						if ( index > 0 )
							parts.RemoveAt( --index );
						break;
					default:
						index++;
						break;
				}
			}

            // Trailing separator removal
            if (parts.Count > 1 && path.Length > 1 && (string)parts[parts.Count - 1] == "")
                parts.RemoveAt(parts.Count - 1);

#if CLR_2_0 || CLR_4_0
            return String.Join(DirectorySeparatorChar.ToString(), parts.ToArray());
#else
            return String.Join(DirectorySeparatorChar.ToString(), (string[])parts.ToArray(typeof(string)));
#endif
        }

		/// <summary>
		/// True if the two paths are the same or if the second is
		/// directly or indirectly under the first. Note that paths 
		/// using different network shares or drive letters are 
		/// considered unrelated, even if they end up referencing
		/// the same subtrees in the file system.
		/// </summary>
		public static bool SamePathOrUnder( string path1, string path2 )
		{
			path1 = Canonicalize( path1 );
			path2 = Canonicalize( path2 );

			int length1 = path1.Length;
			int length2 = path2.Length;

			// if path1 is longer, then path2 can't be under it
			if ( length1 > length2 )
				return false;

			// if lengths are the same, check for equality
			if ( length1 == length2 )
				return string.Compare( path1, path2, IsWindows() ) == 0;

			// path 2 is longer than path 1: see if initial parts match
			if ( string.Compare( path1, path2.Substring( 0, length1 ), IsWindows() ) != 0 )
				return false;
			
			// must match through or up to a directory separator boundary
			return	path2[length1-1] == DirectorySeparatorChar ||
				path2[length1] == DirectorySeparatorChar;
		}

		#endregion

		#region Helper Methods

		private static bool IsWindows()
		{
			return PathUtils.DirectorySeparatorChar == '\\';
		}

        private static string[] SplitPath(string path)
        {
            char[] separators = new char[] { PathUtils.DirectorySeparatorChar, PathUtils.AltDirectorySeparatorChar };

            string[] trialSplit = path.Split(separators);

            int emptyEntries = 0;
            foreach (string piece in trialSplit)
                if (piece == string.Empty)
                    emptyEntries++;

            if (emptyEntries == 0)
                return trialSplit;

            string[] finalSplit = new string[trialSplit.Length - emptyEntries];
            int index = 0;
            foreach (string piece in trialSplit)
                if (piece != string.Empty)
                    finalSplit[index++] = piece;

            return finalSplit;
        }

        private static bool PathsEqual(string path1, string path2)
        {
            if (PathUtils.IsWindows())
                return path1.ToLower().Equals(path2.ToLower());
            else
                return path1.Equals(path2);
        }

        #endregion
	}
}