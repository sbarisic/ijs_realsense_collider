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
		public const bool MarkDirtyAuto = true;
		public const bool UseThreading = false;

		public static bool RealSenseEnabled = false;
		public static bool RenderPoints = false;
		public static bool RenderVoxels = false;

		public static Vector3 WorldOrigin;
		public static Vector3 CameraHeight;

		static void Main(string[] Args) {
			const float Treadmill_X = 1200;
			const float Treadmill_Y = 500;
			const float Treadmill_Z = 2000;
			const float VoxelSize = 10;

			const int VoxelsX = (int)(Treadmill_X / VoxelSize);
			const int VoxelsY = (int)(Treadmill_Y / VoxelSize);
			const int VoxelsZ = (int)(Treadmill_Z / VoxelSize);

			WorldOrigin = new Vector3(1200, 0, 0);
			CameraHeight = new Vector3(0, 1500, 0);

			Console.Spawn(Args.Contains("--console"));
			OptotrakClient.Init(40023);
			LegClient.Init(40024);
			RenderWindow.Init(60, Treadmill_X, Treadmill_Z);
			Voxels.Init(VoxelsX, VoxelsY, VoxelsZ, VoxelSize);
			RealSenseClient.Init();

			while (RenderWindow.Tick()) {
				// Additional logic which should execute per-frame goes here

				if (!UseThreading)
					RealSenseClient.Loop();
			}
		}
	}
}
