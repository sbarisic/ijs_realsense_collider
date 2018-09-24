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
		public static bool SpawnConsole = true; // enable or disable spawning the console at startup
		public static bool MarkDirtyAuto = false; // Internal for optimization
		public static bool UseThreading = false; // Internal for optimization (run RealSense on another thread)

		public static bool RealSenseEnabled = false; // Capture data from the realSense camera (F1 key..)
		public static bool RenderPoints = false;
		public static bool RenderVoxels = false;

		public static float Treadmill_X = 0; // size in mm of voxel space (X direction)
		public static float Treadmill_Y = 0; // size in mm of voxel space (Y direction)
        public static float Treadmill_Z = 0;
		public static float VoxelSize = 0; // size of voxel in mm

		// Moves the optotrak 0,0,0 location to be the bottom-left corner of the threadmill, used to correct the alignment of the cameras to the coordinate frame of this program
		public static float OptotrakOffset_X = 0; 
		public static float OptotrakOffset_Y = 0;
		public static float OptotrakOffset_Z = 0;

		//public static float Variable = 0;

		public static float PelvisHeight = 1300; // the pelvis height in the MVN analyze software. This should be correct.

		// .....................

		public static Vector3 OptotrakOffset;
		public static Vector3 WorldOrigin;
		public static Vector3 CameraHeight;

        /// <summary>
        /// Loads the config.txt file into all the static variables at the top of the program.
        /// </summary>
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

			OptotrakOffset = new Vector3(OptotrakOffset_X, OptotrakOffset_Y, OptotrakOffset_Z).YZX();
		}

		static void Main(string[] Args) {
			LoadConfig();

			int VoxelsX = (int)(Treadmill_X / VoxelSize);
			int VoxelsY = (int)(Treadmill_Y / VoxelSize);
			int VoxelsZ = (int)(Treadmill_Z / VoxelSize);
            
			WorldOrigin = new Vector3(Treadmill_X, 0, 0); // Shifts the world origin to the left side (the actual render window 0,0,0 is in the right corner)
			CameraHeight = new Vector3(0, PelvisHeight, 0); // the distance from the floor to the camera

			Console.Spawn(Args.Contains("--console") || SpawnConsole);

			OptotrakClient.Init(40023); // initializes optotrack client and starts a thread for it. Starts also a thread for UDP (on port 40023)
            LegClient.Init(40024); // The same as above but for MVN Analyze

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
