using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ColliderGUI {
	public static class Kernel32 {
		const string LibName = "kernel32";
		const CallingConvention CConv = CallingConvention.Winapi;

		[DllImport(LibName, CallingConvention = CConv)]
		public static extern bool SetDllDirectory(string Dir);
	}
}
