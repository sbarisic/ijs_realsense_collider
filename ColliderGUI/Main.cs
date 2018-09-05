using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Numerics;
using FishGfx;
using FishGfx.Graphics;
using FishGfx.System;

namespace ColliderGUI {
	static class Program {
		public const bool RenderPoints = true;

		public static Vector3 WorldOrigin;

		static void Main(string[] Args) {
			const float Treadmill_X = 1200;
			const float Treadmill_Y = 500;
			const float Treadmill_Z = 2000;
			const float VoxelSize = 10;

			const int VoxelsX = (int)(Treadmill_X / VoxelSize);
			const int VoxelsY = (int)(Treadmill_Y / VoxelSize);
			const int VoxelsZ = (int)(Treadmill_Z / VoxelSize);

			WorldOrigin = new Vector3(1200, 0, 0);

			Console.Spawn(Args.Contains("--console"));
			OptotrakClient.Init(40023);
			RealSenseClient.Init();
			RenderWindow.Init(60, Treadmill_X, Treadmill_Z);
			Voxels.Init(VoxelsX, VoxelsY, VoxelsZ, VoxelSize);

			while (RenderWindow.Tick()) {
				// Additional logic which should execute per-frame goes here
			}
		}
	}
}
