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
			const float Treadmill_X = 1200;
			const float Treadmill_Y = 500;
			const float Treadmill_Z = 2000;
			const float VoxelSize = 5;

			Console.Spawn(Args.Contains("--console"));
			RenderWindow.Init(60, Treadmill_X, Treadmill_Z);
			Voxels.Init((int)(Treadmill_X / VoxelSize), (int)(Treadmill_Y / VoxelSize), (int)(Treadmill_Z / VoxelSize), VoxelSize);

			while (RenderWindow.Tick()) {
				// Additional logic which should execute per-frame goes here
			}
		}
	}
}
