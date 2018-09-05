using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Globalization;
using Microsoft.Win32.SafeHandles;
using _Console = System.Console;

namespace ColliderGUI {
	static class Console {
		static object Lock = new object();

		static bool ConsoleExists = false;
		static IntPtr CONIN;
		static IntPtr CONOUT;

		static StreamWriter LogFile;

		public static void Spawn(bool Spawn) {
			LogFile = new StreamWriter(File.Open("log.txt", FileMode.Create));
			LogFile.AutoFlush = true;
			LogWriteLine("Log created on {0}", DateTime.Now.ToString("dd. MM. yyyy. HH:mm:ss", CultureInfo.InvariantCulture));

			if (!Spawn)
				return;

			if (Kernel32.AllocConsole()) {
				CONIN = Kernel32.CreateFile("CONIN$", DesiredAccess.GenericRead | DesiredAccess.GenericWrite);
				CONOUT = Kernel32.CreateFile("CONOUT$", DesiredAccess.GenericWrite);

				Kernel32.SetStdHandle(StdHandle.Input, CONIN);
				Kernel32.SetStdHandle(StdHandle.Output, CONOUT);
				Kernel32.SetStdHandle(StdHandle.Error, CONOUT);
				Kernel32.SetConsoleTitle(nameof(ColliderGUI) + " Console");

				ConsoleExists = true;
				SetForeColor();
			}
		}

		public static void SetForeColor(ConsoleColor Clr = ConsoleColor.Gray) {
			if (!ConsoleExists)
				return;

			Kernel32.SetConsoleTextAttribute(CONOUT, (CharAttr)Clr);
		}

		public static void Write(string Msg) {
			LogFile.Write(Msg);
			LogFile.Flush();

			if (!ConsoleExists)
				return;

			_Console.Write(Msg);
		}

		public static void Write(string Fmt, params object[] Args) {
			Write(string.Format(Fmt, Args));
		}

		public static void WriteLine(string Msg) {
			Write(Msg + "\n");
		}

		public static void WriteLine(ConsoleColor Clr, string Msg) {
			lock (Lock) {
				SetForeColor(Clr);
				WriteLine(Msg);
				SetForeColor();
			}
		}

		public static void WriteLine(string Fmt, params object[] Args) {
			WriteLine(string.Format(Fmt, Args));
		}

		public static void WriteLine(ConsoleColor Clr, string Fmt, params object[] Args) {
			WriteLine(Clr, string.Format(Fmt, Args));
		}

		public static void LogWriteLine(string Msg) {
			LogFile.WriteLine(Msg);
			LogFile.Flush();
		}

		public static void LogWriteLine(string Fmt, params object[] Args) {
			LogFile.WriteLine(string.Format(Fmt, Args));
			LogFile.Flush();
		}
	}
}
