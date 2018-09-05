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
		const bool FakePosition = false;

		static int W;
		static int H;

		public static bool Ready;

		public static void Init() {
			Thread PollThread = new Thread(() => {
				Console.WriteLine(ConsoleColor.DarkCyan, "Starting RealSense");

				if (!FakePosition) {
					IEnumerable<FrameData> Resolutions = RealSenseCamera.QueryResolutions().OrderBy((Data) => -Data.Framerate);
					IEnumerable<FrameData> DepthResolutions = Resolutions.Where((Data) => Data.Type == FrameType.Depth);

					int ReqW = 640;
					float ReqH = 480;

					//int ReqW = 1280;
					//float ReqH = 720;

					FrameData DepthRes = DepthResolutions.Where((Data) => Data.Width == ReqW && Data.Height == ReqH && Data.Format == FrameFormat.Z16).First();
					FrameData ColorRes = Resolutions.Where((Data) => Data.Width == ReqW && Data.Height == ReqH && Data.Format == FrameFormat.Rgb8).First();

					W = ColorRes.Width;
					H = ColorRes.Height;
					RealSenseCamera.SetOption(DepthRes, RealSenseOption.VisualPreset, 1);//4
					RealSenseCamera.SetOption(DepthRes, RealSenseOption.EmitterEnabled, 0);
					RealSenseCamera.SetOption(DepthRes, RealSenseOption.EnableAutoExposure, 1);

					//RealSenseCamera.SetOption(DepthRes, RealSenseOption.LaserPower, 30);

					RealSenseCamera.DisableAllStreams();
					RealSenseCamera.EnableStream(DepthRes, ColorRes);
					RealSenseCamera.Start();

					Console.WriteLine(ConsoleColor.DarkCyan, "RealSense ready, polling for frames");
					while (true) {
						if (RealSenseCamera.PollForFrames(null, OnPointCloud)) {
							if (!Ready)
								Ready = true;
						}
					}
				} else {
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
			});
			PollThread.IsBackground = true;
			PollThread.Start();

			while (!Ready)
				Thread.Sleep(10);
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

			lock (Lck) {
				Vector3 CamPos = OptotrakClient.GetPos();
				OptotrakClient.GetRotationAngles(out float Yaw, out float Pitch, out float Roll);
				Matrix4x4 TransMat = Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch + (float)Math.PI, -Roll) * OptotrakClient.GetTranslation();
				PointCount = 0;

				fixed (Vertex3* PointsPtr = Points)
					Util.MemSet(new IntPtr(PointsPtr), 0, Points.Length * sizeof(Vertex3));

				//Voxels.SetVoxel(0, 0, 0, new VoxelEntry(VoxelType.Solid));

				for (int i = 0, j = 0; i < Count; i++) {
					if (Verts[i].Position == Vector3.Zero)
						continue;

					Vertex3 Vert = Verts[i];
					Vert.Color = Clr.GetPixel(Vert.UV, false);
					Vert.Position = Vector3.Transform((Vert.Position * 1000), TransMat);

					if (Vector3.DistanceSquared(CamPos, Vert.Position) < (500 * 500))
						continue;

					Points[j++] = Vert;
					PointCount++;

					Vector3 WorldPos = Vert.Position + Program.WorldOrigin;
					Voxels.SetVoxel(WorldPos, new VoxelEntry(VoxelType.Solid, Points[j].Color));
				}
			}

			return null;
		}

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
