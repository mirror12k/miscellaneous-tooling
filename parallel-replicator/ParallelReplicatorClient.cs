using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;



/**
 * Connects to a selected ParallelReplicator-server and creates files and directories as reported by the server
 * Usage: ParallelReplicatorClient.exe <server-url> <replicate-directory>
 */



// Provides methods for processing file system strings in a cross-platform manner.
// Most of the methods don't do a complete parsing (such as examining a UNC hostname), 
// but they will handle most string operations.
public static class PathNetCore {

    /// <summary>
    /// Create a relative path from one path to another. Paths will be resolved before calculating the difference.
    /// Default path comparison for the active platform will be used (OrdinalIgnoreCase for Windows or Mac, Ordinal for Unix).
    /// </summary>
    /// <param name="relativeTo">The source path the output should be relative to. This path is always considered to be a directory.</param>
    /// <param name="path">The destination path.</param>
    /// <returns>The relative path or <paramref name="path"/> if the paths don't share the same root.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="relativeTo"/> or <paramref name="path"/> is <c>null</c> or an empty string.</exception>
    public static string GetRelativePath(string relativeTo, string path) {
        return GetRelativePath(relativeTo, path, StringComparison);
    }

    private static string GetRelativePath(string relativeTo, string path, StringComparison comparisonType) {
        if (string.IsNullOrEmpty(relativeTo)) throw new ArgumentNullException(nameof(relativeTo));
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

        relativeTo = Path.GetFullPath(relativeTo);
        path = Path.GetFullPath(path);

        // Need to check if the roots are different- if they are we need to return the "to" path.
        if (!PathInternalNetCore.AreRootsEqual(relativeTo, path, comparisonType))
            return path;

        int commonLength = PathInternalNetCore.GetCommonPathLength(relativeTo, path,
            ignoreCase: comparisonType == StringComparison.OrdinalIgnoreCase);

        // If there is nothing in common they can't share the same root, return the "to" path as is.
        if (commonLength == 0)
            return path;

        // Trailing separators aren't significant for comparison
        int relativeToLength = relativeTo.Length;
        if (PathInternalNetCore.EndsInDirectorySeparator(relativeTo))
            relativeToLength--;

        bool pathEndsInSeparator = PathInternalNetCore.EndsInDirectorySeparator(path);
        int pathLength = path.Length;
        if (pathEndsInSeparator)
            pathLength--;

        // If we have effectively the same path, return "."
        if (relativeToLength == pathLength && commonLength >= relativeToLength) return ".";

        // We have the same root, we need to calculate the difference now using the
        // common Length and Segment count past the length.
        //
        // Some examples:
        //
        //  C:\Foo C:\Bar L3, S1 -> ..\Bar
        //  C:\Foo C:\Foo\Bar L6, S0 -> Bar
        //  C:\Foo\Bar C:\Bar\Bar L3, S2 -> ..\..\Bar\Bar
        //  C:\Foo\Foo C:\Foo\Bar L7, S1 -> ..\Bar

        StringBuilder
            sb = new StringBuilder(); //StringBuilderCache.Acquire(Math.Max(relativeTo.Length, path.Length));

        // Add parent segments for segments past the common on the "from" path
        if (commonLength < relativeToLength) {
            sb.Append("..");

            for (int i = commonLength + 1; i < relativeToLength; i++) {
                if (PathInternalNetCore.IsDirectorySeparator(relativeTo[i])) {
                    sb.Append(DirectorySeparatorChar);
                    sb.Append("..");
                }
            }
        }
        else if (PathInternalNetCore.IsDirectorySeparator(path[commonLength])) {
            // No parent segments and we need to eat the initial separator
            //  (C:\Foo C:\Foo\Bar case)
            commonLength++;
        }

        // Now add the rest of the "to" path, adding back the trailing separator
        int differenceLength = pathLength - commonLength;
        if (pathEndsInSeparator)
            differenceLength++;

        if (differenceLength > 0) {
            if (sb.Length > 0) {
                sb.Append(DirectorySeparatorChar);
            }

            sb.Append(path, commonLength, differenceLength);
        }

        return sb.ToString(); //StringBuilderCache.GetStringAndRelease(sb);
    }

    // Public static readonly variant of the separators. The Path implementation itself is using
    // internal const variant of the separators for better performance.
    public static readonly char DirectorySeparatorChar = PathInternalNetCore.DirectorySeparatorChar;
    public static readonly char AltDirectorySeparatorChar = PathInternalNetCore.AltDirectorySeparatorChar;
    public static readonly char VolumeSeparatorChar = PathInternalNetCore.VolumeSeparatorChar;
    public static readonly char PathSeparator = PathInternalNetCore.PathSeparator;

    /// <summary>Returns a comparison that can be used to compare file and directory names for equality.</summary>
    internal static StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;
}

/// <summary>Contains internal path helpers that are shared between many projects.</summary>
internal static class PathInternalNetCore {
    internal const char DirectorySeparatorChar = '\\';
    internal const char AltDirectorySeparatorChar = '/';
    internal const char VolumeSeparatorChar = ':';
    internal const char PathSeparator = ';';

    internal const string ExtendedDevicePathPrefix = @"\\?\";
    internal const string UncPathPrefix = @"\\";
    internal const string UncDevicePrefixToInsert = @"?\UNC\";
    internal const string UncExtendedPathPrefix = @"\\?\UNC\";
    internal const string DevicePathPrefix = @"\\.\";

    //internal const int MaxShortPath = 260;

    // \\?\, \\.\, \??\
    internal const int DevicePrefixLength = 4;

    /// <summary>
    /// Returns true if the two paths have the same root
    /// </summary>
    internal static bool AreRootsEqual(string first, string second, StringComparison comparisonType) {
        int firstRootLength = GetRootLength(first);
        int secondRootLength = GetRootLength(second);

        return firstRootLength == secondRootLength
               && string.Compare(
                   strA: first,
                   indexA: 0,
                   strB: second,
                   indexB: 0,
                   length: firstRootLength,
                   comparisonType: comparisonType) == 0;
    }

    /// <summary>
    /// Gets the length of the root of the path (drive, share, etc.).
    /// </summary>
    internal static int GetRootLength(string path) {
        int i = 0;
        int volumeSeparatorLength = 2; // Length to the colon "C:"
        int uncRootLength = 2; // Length to the start of the server name "\\"

        bool extendedSyntax = path.StartsWith(ExtendedDevicePathPrefix);
        bool extendedUncSyntax = path.StartsWith(UncExtendedPathPrefix);
        if (extendedSyntax) {
            // Shift the position we look for the root from to account for the extended prefix
            if (extendedUncSyntax) {
                // "\\" -> "\\?\UNC\"
                uncRootLength = UncExtendedPathPrefix.Length;
            }
            else {
                // "C:" -> "\\?\C:"
                volumeSeparatorLength += ExtendedDevicePathPrefix.Length;
            }
        }

        if ((!extendedSyntax || extendedUncSyntax) && path.Length > 0 && IsDirectorySeparator(path[0])) {
            // UNC or simple rooted path (e.g. "\foo", NOT "\\?\C:\foo")

            i = 1; //  Drive rooted (\foo) is one character
            if (extendedUncSyntax || (path.Length > 1 && IsDirectorySeparator(path[1]))) {
                // UNC (\\?\UNC\ or \\), scan past the next two directory separators at most
                // (e.g. to \\?\UNC\Server\Share or \\Server\Share\)
                i = uncRootLength;
                int n = 2; // Maximum separators to skip
                while (i < path.Length && (!IsDirectorySeparator(path[i]) || --n > 0)) i++;
            }
        }
        else if (path.Length >= volumeSeparatorLength &&
                 path[volumeSeparatorLength - 1] == PathNetCore.VolumeSeparatorChar) {
            // Path is at least longer than where we expect a colon, and has a colon (\\?\A:, A:)
            // If the colon is followed by a directory separator, move past it
            i = volumeSeparatorLength;
            if (path.Length >= volumeSeparatorLength + 1 && IsDirectorySeparator(path[volumeSeparatorLength])) i++;
        }

        return i;
    }

    /// <summary>
    /// True if the given character is a directory separator.
    /// </summary>
    internal static bool IsDirectorySeparator(char c) {
        return c == PathNetCore.DirectorySeparatorChar || c == PathNetCore.AltDirectorySeparatorChar;
    }

    /// <summary>
    /// Get the common path length from the start of the string.
    /// </summary>
    internal static int GetCommonPathLength(string first, string second, bool ignoreCase) {
        int commonChars = EqualStartingCharacterCount(first, second, ignoreCase: ignoreCase);

        // If nothing matches
        if (commonChars == 0)
            return commonChars;

        // Or we're a full string and equal length or match to a separator
        if (commonChars == first.Length
            && (commonChars == second.Length || IsDirectorySeparator(second[commonChars])))
            return commonChars;

        if (commonChars == second.Length && IsDirectorySeparator(first[commonChars]))
            return commonChars;

        // It's possible we matched somewhere in the middle of a segment e.g. C:\Foodie and C:\Foobar.
        while (commonChars > 0 && !IsDirectorySeparator(first[commonChars - 1]))
            commonChars--;

        return commonChars;
    }

    /// <summary>
    /// Gets the count of common characters from the left optionally ignoring case
    /// </summary>
    internal static unsafe int EqualStartingCharacterCount(string first, string second, bool ignoreCase) {
        if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second)) return 0;

        int commonChars = 0;

        fixed (char* f = first)
        fixed (char* s = second) {
            char* l = f;
            char* r = s;
            char* leftEnd = l + first.Length;
            char* rightEnd = r + second.Length;

            while (l != leftEnd && r != rightEnd
                                && (*l == *r || (ignoreCase &&
                                                 char.ToUpperInvariant((*l)) == char.ToUpperInvariant((*r))))) {
                commonChars++;
                l++;
                r++;
            }
        }

        return commonChars;
    }

    /// <summary>
    /// Returns true if the path ends in a directory separator.
    /// </summary>
    internal static bool EndsInDirectorySeparator(string path)
        => path.Length > 0 && IsDirectorySeparator(path[path.Length - 1]);
}


public class ParallelReplicatorClient {

	public static int buffer_size = 16 * 1024 * 1024;
	public static string base_directory;

	public static CancellationTokenSource cts;
	public static ClientWebSocket ws;
	public static FileSystemWatcher watcher = null;


	public static void Main(string[] args) {
		if (args.Length < 2) {
			Console.WriteLine("Usage: ParallelReplicatorClient.exe <server-url> <replicate-directory>\n");
			return;
		}

		var url = args[0];
		base_directory = args[1];
		Console.Write("replicating files to: " + base_directory + "\n");

		cts = new CancellationTokenSource();

		var _ = ConnectAsync(url);
		_ = WatchAsync();

		Console.WriteLine("ParallelReplicator client connecting...\n");
		Console.WriteLine("Press any key to exit.\n");
		Console.ReadKey();
	}

	public static byte[] ReadAllBytesAggressive(string path) {
		var i = 0;
		while (true) {
			try {
				return File.ReadAllBytes(path);
			} catch (IOException e) {
				Thread.Sleep(50);
				i += 50;
				if (i >= 500)
					throw e;
			}
		}
	}

	public static async Task ConnectAsync(string url) {
		ws = new ClientWebSocket();
		Console.WriteLine("connecting");
		await ws.ConnectAsync(new Uri(url), cts.Token);

		MemoryStream outputStream = null;
		WebSocketReceiveResult receiveResult = null;
		var buffer = new byte[buffer_size];
		var ass = new ArraySegment<Byte>(buffer);
		try {
			while (true) {
				outputStream = new MemoryStream(buffer_size);
				do {
					receiveResult = await ws.ReceiveAsync(ass, cts.Token);
					if (receiveResult.MessageType != WebSocketMessageType.Close)
						outputStream.Write(buffer, 0, receiveResult.Count);
				}
				while (!receiveResult.EndOfMessage);
				if (receiveResult.MessageType == WebSocketMessageType.Close) break;
				outputStream.Position = 0;
				ResponseReceived(outputStream);
			}
		}
		catch (Exception e) {
			Console.WriteLine("exception: " + e);
		}
	}

	public static async Task WatchAsync() {
		watcher = new FileSystemWatcher(base_directory);
		// watcher.Filter = "*.*";
		// watcher.Created += OnChanged;
		// watcher.Renamed += OnChanged;
		// watcher.Deleted += OnChanged;
		// watcher.EnableRaisingEvents = true;

		watcher.NotifyFilter = NotifyFilters.DirectoryName
		                     | NotifyFilters.FileName
		                     | NotifyFilters.LastWrite;

		watcher.Changed += OnChanged;
		watcher.Created += OnChanged;
		watcher.Renamed += OnRenamed;
		watcher.Deleted += OnDeleted;
		// watcher.Error += OnError;

		// watcher.Filter = "*";
		watcher.IncludeSubdirectories = true;
		watcher.EnableRaisingEvents = true;
	}

	public static void OnChanged(object source, FileSystemEventArgs e) {
		Console.WriteLine("update: " + e.FullPath);

		try {
			if (Directory.Exists(e.FullPath)) {
				var buffer = Encoding.UTF8.GetBytes("updatedirectory," + Convert.ToBase64String(Encoding.UTF8.GetBytes(PathNetCore.GetRelativePath(base_directory, e.FullPath))));
				var ass = new ArraySegment<Byte>(buffer);
				ws.SendAsync(ass, System.Net.WebSockets.WebSocketMessageType.Text, true, cts.Token);
			} else {
				var data = ReadAllBytesAggressive(e.FullPath);
				var buffer = Encoding.UTF8.GetBytes("update,"
						+ Convert.ToBase64String(Encoding.UTF8.GetBytes(PathNetCore.GetRelativePath(base_directory, e.FullPath)))
						+ "," + Convert.ToBase64String(data));
				var ass = new ArraySegment<Byte>(buffer);
				ws.SendAsync(ass, System.Net.WebSockets.WebSocketMessageType.Text, true, cts.Token);
			}
		} catch (Exception ex) {
			Console.WriteLine("exception during update: " + ex);
		}

	}

	public static void OnRenamed(object source, RenamedEventArgs e) {
		Console.WriteLine("rename: " + e.OldFullPath + " -> " + e.FullPath);

		try {
			var buffer = Encoding.UTF8.GetBytes("delete," + Convert.ToBase64String(Encoding.UTF8.GetBytes(PathNetCore.GetRelativePath(base_directory, e.OldFullPath))));
			var ass = new ArraySegment<Byte>(buffer);
			ws.SendAsync(ass, System.Net.WebSockets.WebSocketMessageType.Text, true, cts.Token);

			var data = ReadAllBytesAggressive(e.FullPath);
			buffer = Encoding.UTF8.GetBytes("update,"
					+ Convert.ToBase64String(Encoding.UTF8.GetBytes(PathNetCore.GetRelativePath(base_directory, e.FullPath)))
					+ "," + Convert.ToBase64String(data));
			ass = new ArraySegment<Byte>(buffer);
			ws.SendAsync(ass, System.Net.WebSockets.WebSocketMessageType.Text, true, cts.Token);
		} catch (Exception ex) {
			Console.WriteLine("exception during rename: " + ex);
		}
	}

	public static void OnDeleted(object source, FileSystemEventArgs e) {
		try {
			Console.WriteLine("delete: " + PathNetCore.GetRelativePath(base_directory, e.FullPath));

			var buffer = Encoding.UTF8.GetBytes("delete," + Convert.ToBase64String(Encoding.UTF8.GetBytes(PathNetCore.GetRelativePath(base_directory, e.FullPath))));
			var ass = new ArraySegment<Byte>(buffer);
			ws.SendAsync(ass, System.Net.WebSockets.WebSocketMessageType.Text, true, cts.Token);
		} catch (Exception ex) {
			Console.WriteLine("exception during delete: " + ex);
		}
	}

	public static void ResponseReceived(Stream stream) {
		StreamReader reader = new StreamReader(stream);
		string message = reader.ReadToEnd();

		watcher.EnableRaisingEvents = false;

		var instructions = message.Split(new char[] { ',' });
		var cmd = instructions[0];
		var path = Encoding.UTF8.GetString(Convert.FromBase64String(instructions[1]));
		Console.WriteLine("\t cmd: " + cmd + " -> " + path);

		if (cmd == "update") {
			byte[] filedata = Convert.FromBase64String(instructions[2]);
			File.WriteAllBytes(base_directory + "/" + path, filedata);
		} else if (cmd == "updatedirectory") {
			if (!Directory.Exists(base_directory + "/" + path))
				Directory.CreateDirectory(base_directory + "/" + path);
		} else if (cmd == "delete") {
			try { File.Delete(base_directory + "/" + path); } catch (Exception) {}
			try { Directory.Delete(base_directory + "/" + path, true); } catch (Exception) {}
		}

		watcher.EnableRaisingEvents = true;
		stream.Dispose();
	}
}

