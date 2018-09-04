using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ColliderGUI {
	[Flags]
	public enum DesiredAccess : uint {
		GenericRead = 0x80000000,
		GenericWrite = 0x40000000,
		GenericExecute = 0x20000000,
		GenericAll = 0x10000000
	}

	public enum StdHandle : uint {
		Input = unchecked((uint)(-10)),
		Output = unchecked((uint)(-11)),
		Error = unchecked((uint)(-12)),
	}

	[Flags]
	public enum CharAttr : ushort {
		FG_B = 0x0001,
		FG_G = 0x0002,
		FG_R = 0x0004,
		FG_INTENSITY = 0x0008,
		BG_B = 0x0010,
		BG_G = 0x0020,
		BG_R = 0x0040,
		BG_INTENSITY = 0x0080,
		CMN_LVB_LEADING_BYTE = 0x0100,
		CMN_LVB_TRAILING_BYTE = 0x0200,
		CMN_LVB_GRID_HORIZONTAL = 0x0400,
		CMN_LVB_GRID_LVERTICAL = 0x0800,
		CMN_LVB_GRID_RVERTICAL = 0x1000,
		CMN_LVB_REVERSE_VIDEO = 0x4000,
		CMN_LVB_UNDERSCORE = 0x8000
	}

	public static class Kernel32 {
		const string LibName = "kernel32";
		const CallingConvention CConv = CallingConvention.Winapi;

		[DllImport(LibName, CallingConvention = CConv)]
		public static extern bool SetDllDirectory(string Dir);

		[DllImport(LibName, CallingConvention = CConv)]
		public static extern bool AllocConsole();

		[DllImport(LibName, CallingConvention = CConv)]
		public static extern IntPtr GetStdHandle(StdHandle H);

		[DllImport(LibName, CallingConvention = CConv)]
		public static extern void SetStdHandle(StdHandle H, IntPtr Handle);

		[DllImport(LibName, CallingConvention = CConv)]
		public static extern IntPtr CreateFile(string Name, DesiredAccess Access, FileShare ShareMode, IntPtr Sec, FileMode CrDisp, FileAttributes FlagsAttrs, IntPtr Template);

		public static IntPtr CreateFile(string Name, DesiredAccess Access) {
			return CreateFile(Name, Access, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
		}

		[DllImport(LibName, CallingConvention = CConv)]
		public static extern bool SetConsoleTextAttribute(IntPtr ConOut, CharAttr Attributes);

		[DllImport(LibName, CallingConvention = CConv)]
		public static extern bool SetConsoleTitle(string Title);
	}
}
