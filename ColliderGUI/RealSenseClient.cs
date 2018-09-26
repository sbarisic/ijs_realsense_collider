using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Threading;
using System.Runtime.InteropServices;
using FishGfx.RealSense;
using FishGfx;
using System.Diagnostics;
using FishGfx.Graphics.Drawables;

namespace ColliderGUI {
	public static unsafe class RealSenseClient {
		const bool FakePosition = false; // was used for debugging. Might not even work anymore..

		static int W;
		static int H;

		public static bool Ready = false;

		public static void Init() {
			if (Program.UseThreading) {
				Thread PollThread = new Thread(InitInternal); // Run InitInternal on a new thread
				PollThread.IsBackground = true;
				PollThread.Start();

				while (!Ready)
					Thread.Sleep(10);
			} else
				InitInternal(); // Run InitInternal directly
		}

        /// <summary>
        /// 
        /// </summary>
		static void InitInternal() {
			Console.WriteLine(ConsoleColor.DarkCyan, "Starting RealSense");

			if (!FakePosition) {
                // The variable Resolution contains all available combinations of resolutions, formats sensors, etc. you can get from the camera.
				IEnumerable<FrameData> Resolutions = RealSenseCamera.QueryResolutions().OrderBy((Data) => -Data.Framerate); //  Resolutions is a list of frame data which contains the sensors and the resolutions. Basically all the possible formats/outputs of the camera. Depth,ARGB, etc. in different resolutions.
				IEnumerable<FrameData> DepthResolutions = Resolutions.Where((Data) => Data.Type == FrameType.Depth); // Selects the resolutions/formats for depth frame type

				int ReqW = 640;
				int ReqH = 480;

				//int ReqW = 848;
				//int ReqH = 480;

				//int ReqW = 1280;
				//int ReqH = 720;

                // From the available resolutions/formats pick only the one that we want.
				FrameData DepthRes = DepthResolutions.Where((Data) => Data.Width == ReqW && Data.Height == ReqH && Data.Format == FrameFormat.Z16).First();
				FrameData ColorRes = Resolutions.Where((Data) => Data.Width == ReqW && Data.Height == ReqH && Data.Format == FrameFormat.Rgb8).First();

				W = ColorRes.Width;
				H = ColorRes.Height;

				Console.WriteLine(ConsoleColor.DarkCyan, "RealSense running at {0}x{1}", W, H);

                // This options were copied from the Intel RealSense Viewer. The demo program to connect to the camera...
				RealSenseCamera.SetOption(DepthRes, RealSenseOption.VisualPreset, 1); //4
				RealSenseCamera.SetOption(DepthRes, RealSenseOption.EmitterEnabled, 0); // This enables/disables the IR pattern projector on the camera. Curently turned off, because it interfeers with the ototrack cameras.
				RealSenseCamera.SetOption(DepthRes, RealSenseOption.EnableAutoExposure, 1);

				//RealSenseCamera.SetOption(DepthRes, RealSenseOption.LaserPower, 30); // Set different power leves of the IR laser emitter

				RealSenseCamera.DisableAllStreams(); // Make sure to terminate any open data stream from the camera.
				RealSenseCamera.EnableStream(DepthRes, ColorRes); // This tells the camera what kind of data to send back. Type of video/data streams. In this case the depth image in specified format and color image.
				RealSenseCamera.Start(); // Starts the camera with the selected streams

				Console.WriteLine(ConsoleColor.DarkCyan, "RealSense ready");

				if (Program.UseThreading)
					while (true)
						Loop();

			} else { // Fake point cloud of the camera
				Ready = true;

				float Scale = 1.0f / 500.0f;
				int PlaneSize = 100;
				Vertex3[] Verts = OnPointCloud(PlaneSize * PlaneSize, null, null);

				for (int y = 0; y < PlaneSize; y++)
					for (int x = 0; x < PlaneSize; x++)
						Verts[y * PlaneSize + x] = new Vertex3(x * Scale - ((PlaneSize / 2) * Scale), y * Scale - ((PlaneSize / 2) * Scale), 0.5f);

				while (true)
					OnPointCloud(Verts.Length, Verts, null);
			}
		}

        /// <summary>
        /// Loop to query data from the Realsense camera with RealSenseCamera.PollForFrames
        /// </summary>
		public static void Loop() {
            // Custom made wrapper funtion to poll for frames (point cloud) from the Real Sense Camera.
            if (Program.RealSenseEnabled && RealSenseCamera.PollForFrames(null, OnPointCloud)) {
				if (!Ready) // This sets the variable ready to true after the first point cloud frame is recieved. Because other parts of the code cannot work until there is some data from the camera.
					Ready = true;
			}
		}

		static object Lck = new object();
		static int PointCount;
		static Vertex3[] Points;

		static Vertex3[] VertsArr;
		static Vertex3[] OnPointCloud(int Count, Vertex3[] Verts, FrameData[] Frames) {
			if (Verts == null) {
				if (VertsArr == null || VertsArr.Length < Count) {
					VertsArr = new Vertex3[Count];

					lock (Lck)
						Points = new Vertex3[Count];
				}

				return VertsArr;
			}

			FrameData Clr = Frames[1];

			lock (Lck) {// thread synchronization stuff. Used to lock an object to prevent other threads from chaging it... (Monitor.Enter)
				Vector3 CamPos = OptotrakClient.GetPos();
				OptotrakClient.GetRotationAngles(out float Yaw, out float Pitch, out float Roll);
				Matrix4x4 TransMat = Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch + (float)Math.PI, -Roll) * OptotrakClient.GetTranslation();
				PointCount = 0;

				fixed (Vertex3* PointsPtr = Points)
					Util.MemSet(new IntPtr(PointsPtr), 0, Points.Length * sizeof(Vertex3));

				//Voxels.SetVoxel(0, 0, 0, new VoxelEntry(VoxelType.Solid));

				//Voxels.Fill(new VoxelEntry(VoxelType.Solid));
				Voxels.Fill(VoxelEntry.Empty);

				for (int i = 0, j = 0; i < Count; i++) { // This loops trough all the points in the point cloud
					if (Verts[i].Position == Vector3.Zero) // If a point position in 0 discard it. 
						continue;

					Vertex3 Vert = Verts[i];
					Vert.Position = Vector3.Transform((Vert.Position * 1000), TransMat); // Transfomr meter into mm and transforms point from camera space to world space (coordinate frame)

					if (Vector3.Distance(CamPos, Vert.Position) < 500) // If a point is closer than 500 mm discard it. Because the camera does not work well with points so close. 
						continue;

					Vert.Color = Clr.GetPixel(Vert.UV, false); // Gets color and puts it into the vertex
					Points[j++] = Vert;
					PointCount++;

					Vector3 WorldPos = Vert.Position + Program.WorldOrigin; // Align graphics coordinate system to world coordinate system....

					/*Voxels.Cast(CamPos, WorldPos, (X, Y, Z) => {
						Voxels.SetVoxel(X, Y, Z, new VoxelEntry(VoxelType.None));
					});*/

					Voxels.SetVoxel(WorldPos, new VoxelEntry(VoxelType.Solid, Vert.Color)); // If any vertx is in the volume of voxels, set the corresponding voxel as solid and the same color as the vertex.
				}

                //This is the "Collision detection". The function Voxels.Ray takes in 2 position arguments and a function as an argument (the (X,Y,Z)... part). Search for delegate :) 
				RightLegCollides = !Voxels.Ray(LegClient.R_Start, LegClient.R_End, (X, Y, Z) => {
                    // The part inside this braces gets called multiple times, once for every voxel between the start and end position
					if (Voxels.IsSolid(X, Y, Z)) // Checks for actual collision, if there is a solid block at this point, return false, which stops the .Ray function.
						return false;

					Voxels.SetVoxel(X, Y, Z, new VoxelEntry(VoxelType.NonSolid, RightLegCollides ? Color.Red : Color.Green)); // Based on the value of RightLegCollides, color the voxels green or red. The red voxels actually render one frame after the colision was detected.
					return true;
				});

				LeftLegCollides = !Voxels.Ray(LegClient.L_Start, LegClient.L_End, (X, Y, Z) => {
					if (Voxels.IsSolid(X, Y, Z))
						return false;

					Voxels.SetVoxel(X, Y, Z, new VoxelEntry(VoxelType.NonSolid, LeftLegCollides ? Color.Red : Color.Green));
					return true;
				});

				if (!Program.MarkDirtyAuto)
					Voxels.MarkDirty();
			}

			return null;
		}

        public static bool RightLegCollides;
        public static bool LeftLegCollides;

        /// <summary>
        /// Used to render the verteces. The points form the camera...
        /// </summary>
        /// <param name="Mesh"></param>
		public static void GetVerts(ref Mesh3D Mesh) {
			if (Program.RenderPoints) {
				lock (Lck) {
					if (PointCount != 0)
						Mesh.SetVertices(Points, PointCount, false, true);
				}
			}
		}
	}
}
