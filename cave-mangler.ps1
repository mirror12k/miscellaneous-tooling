$source = @"
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;

public class CaveMangler
{
	public static Random rand = new Random();
	public static void Run(string file, string args) {
		var baseFile = File.ReadAllBytes(file);



		var proc = Process.Start(file, args);
		proc.WaitForExit();
		var baseExitCode = proc.ExitCode;
		Console.WriteLine("got exit code: " + proc.ExitCode);

		var rangeMin = rand.Next(baseFile.Length);
		var rangeSafe = rangeMin;
		var rangeMax = baseFile.Length;
		var rangeMaxTemp = rangeMax;

		while (true) {
			// Console.WriteLine("loop: ([" + rangeMin + ":" + rangeMaxTemp + "]) (" + rangeSafe + ":" + rangeMax + ")");
			var modified = baseFile.ToArray();

			Console.WriteLine("inserting possible cave at [" + rangeMin + ":" + rangeMaxTemp + "]");
			for (var i = rangeMin; i < rangeMaxTemp; i++)
				modified[i] = 0xCC;

			File.WriteAllBytes("temp-mangled.exe", modified);

			var success = false;
			try {
				proc = Process.Start("temp-mangled.exe", args);
				proc.WaitForExit();
				Console.WriteLine("got modified exit code: " + proc.ExitCode);
				success = proc.ExitCode == baseExitCode;
			} catch (Exception e) {
				Console.WriteLine("proc died: " + e);
			}

			if (success) {
				rangeSafe = rangeMaxTemp;

				if (rangeMaxTemp == rangeMax || rangeMax - rangeSafe < 3) {
					Console.WriteLine("resulting cave at [" + rangeMin + ":" + rangeSafe + "] (" + (rangeSafe - rangeMin) + " bytes)");
					return;
				}

				rangeMaxTemp = (rangeSafe + rangeMax) / 2;
			} else {
				rangeMax = rangeMaxTemp;
				rangeMaxTemp = (rangeSafe + rangeMax) / 2;

				if (rangeMax - rangeSafe < 3) {
					Console.WriteLine("resulting cave at [" + rangeMin + ":" + rangeSafe + "] (" + (rangeSafe - rangeMin) + " bytes)");
					return;
				}
			}
		}
	}
}
"@


Add-Type -TypeDefinition $source
[CaveMangler]::Run("C:\\Windows\\System32\\cmd.exe", "/c help")

