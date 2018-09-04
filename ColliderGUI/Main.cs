using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using RealsenseCollider;
using FishGfx;
using FishGfx.Graphics;
using FishGfx.System;

namespace ColliderGUI {
	class Program {
		static void Main(string[] Args) {
			Console.Spawn(Args.Contains("--console"));
			RenderWindow.Init(60);



			while (RenderWindow.Tick()) {
				// Additional logic which should execute per-frame goes here
			}
		}
	}
}
