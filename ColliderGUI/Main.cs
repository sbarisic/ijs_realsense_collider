using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Numerics;
using FishGfx;
using FishGfx.Graphics;
using FishGfx.System;
using System.Globalization;

namespace ColliderGUI {
	public static class Program {
		public static bool MarkDirtyAuto = false;
		public static bool UseThreading = false;

		public static bool RealSenseEnabled = false;
		public static bool RenderPoints = false;
		public static bool RenderVoxels = false;

		public static float Treadmill_X = 0;
		public static float Treadmill_Y = 0;
		public static float Treadmill_Z = 0;
		public static float VoxelSize = 0;

		public static Vector3 WorldOrigin;
		public static Vector3 CameraHeight;

		static void LoadConfig() {
			string[] Lines = File.ReadAllLines("config.txt");

			for (int i = 0; i < Lines.Length; i++) {
				string Line = Lines[i].Trim();

				if (Line.Length == 0)
					continue;

				string[] Vals = Line.Split('=');
				FieldInfo Field = typeof(Program).GetField(Vals[0].Trim(), BindingFlags.Public | BindingFlags.Static);

				if (Field.FieldType == typeof(float))
					Field.SetValue(null, float.Parse(Vals[1].Trim(), CultureInfo.InvariantCulture));
				else if (Field.FieldType == typeof(bool))
					Field.SetValue(null, Vals[1].Trim().ToLower() == "true");
			}
		}

		static void Main(string[] Args) {
			LoadConfig();

			int VoxelsX = (int)(Treadmill_X / VoxelSize);
			int VoxelsY = (int)(Treadmill_Y / VoxelSize);
			int VoxelsZ = (int)(Treadmill_Z / VoxelSize);

			WorldOrigin = new Vector3(1200, 0, 0);
			CameraHeight = new Vector3(0, 1400, 0);

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
