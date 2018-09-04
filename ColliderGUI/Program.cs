using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealsenseCollider;

namespace ColliderGUI {
	class Program {
		static void Main(string[] args) {
			Kernel32.SetDllDirectory("x64");

			Console.WriteLine("Done!");
			Console.ReadLine();
		}
	}
}
