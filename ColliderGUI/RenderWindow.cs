using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using FishGfx;
using FishGfx.Graphics;
using FishGfx.System;
using FishGfx.Graphics.Drawables;
using FishGfx.Formats;

using RWnd = FishGfx.Graphics.RenderWindow;

namespace ColliderGUI {
	static class RenderWindow {
		static float Dt;
		static Stopwatch SWatch;
		static float Framerate;

		static RWnd RWnd;
		static Camera Cam;
		static Vector3 MoveVec;

		static float Treadmill_X;
		static float Treadmill_Z;

		static bool _CaptureMouse;
		static bool CaptureMouse {
			get {
				return _CaptureMouse;
			}
			set {
				RWnd.CaptureCursor = (_CaptureMouse = value);
			}
		}

		public static void Init(float TargetFramerate, float Treadmill_X, float Treadmill_Z) {
			RenderWindow.Treadmill_X = Treadmill_X;
			RenderWindow.Treadmill_Z = Treadmill_Z;

			Framerate = TargetFramerate;
			Console.WriteLine(ConsoleColor.Green, "Target framerate: {0} FPS", TargetFramerate);
			Console.WriteLine(ConsoleColor.Green, "M  - Camera position");
			Console.WriteLine(ConsoleColor.Green, "F1 - Toggle RealSense capture");
			Console.WriteLine(ConsoleColor.Green, "F2 - Toggle mouse capture");
			Console.WriteLine(ConsoleColor.Green, "F3 - Toggle render points");
			Console.WriteLine(ConsoleColor.Green, "F4 - Toggle render voxels");
			Console.WriteLine(ConsoleColor.Green, "F5 - Move camera to RealSense position");

			Vector2 Res = RWnd.GetDesktopResolution() * 0.9f;
			RWnd = new RWnd((int)Res.X, (int)Res.Y, nameof(ColliderGUI));

			Console.WriteLine(ConsoleColor.Green, "Running {0}", RenderAPI.Version);
			Console.WriteLine(ConsoleColor.Green, RenderAPI.Renderer);
			Console.LogWriteLine("OpenGL Extensions:\n    {0}", string.Join("\n    ", RenderAPI.Extensions));

			CaptureMouse = false;
			RWnd.OnMouseMoveDelta += (Wnd, X, Y) => {
				if (CaptureMouse)
					Cam.Update(-new Vector2(X, Y));
			};

			RWnd.OnKey += (RWnd Wnd, Key Key, int Scancode, bool Pressed, bool Repeat, KeyMods Mods) => {
				if (Key == Key.Space)
					MoveVec.Y = Pressed ? 1 : 0;
				else if (Key == Key.C)
					MoveVec.Y = Pressed ? -1 : 0;
				else if (Key == Key.W)
					MoveVec.Z = Pressed ? -1 : 0;
				else if (Key == Key.A)
					MoveVec.X = Pressed ? -1 : 0;
				else if (Key == Key.S)
					MoveVec.Z = Pressed ? 1 : 0;
				else if (Key == Key.D)
					MoveVec.X = Pressed ? 1 : 0;

				if (Pressed && Key == Key.Escape)
					RWnd.Close();

				if (Pressed && Key == Key.M)
					Console.WriteLine(ConsoleColor.Green, "Pos: {0}", Cam.Position);

				if (Pressed && Key == Key.F1)
					Program.RealSenseEnabled = !Program.RealSenseEnabled;

				if (Pressed && Key == Key.F2)
					CaptureMouse = !CaptureMouse;

				if (Pressed && Key == Key.F3)
					Program.RenderPoints = !Program.RenderPoints;

				if (Pressed && Key == Key.F4)
					Program.RenderVoxels = !Program.RenderVoxels;

				if (Pressed && Key == Key.F5)
					Cam.Position = OptotrakClient.GetPos();
			};

			SetupCamera();
			LoadAssets();

			Gfx.PointSize(5);
		}

		static void SetupCamera() {
			Cam = ShaderUniforms.Camera;
			Cam.MouseMovement = true;

			Cam.SetPerspective(RWnd.WindowSize.X, RWnd.WindowSize.Y);

			Cam.Position = new Vector3(-1167.559f, 952.6618f, 278.9347f);
			Cam.LookAt(new Vector3(-Treadmill_X / 2, 0, Treadmill_Z));
		}

		static ShaderProgram Default;
		static ShaderProgram DefaultFlatColor;
		static RenderModel WorldSurface;
		static RenderModel Plane;
		static RenderModel Pin;
		static Mesh3D Points;

		static Texture PinMat1;
		static Texture PinMat2;

		static void SetPinTex(Texture T) {
			Pin.SetMaterialTexture("pin_mat", T);
		}

		static void LoadAssets() {
			Default = new ShaderProgram(new ShaderStage(ShaderType.VertexShader, "data/default3d.vert"),
				new ShaderStage(ShaderType.FragmentShader, "data/default.frag"));

			DefaultFlatColor = new ShaderProgram(new ShaderStage(ShaderType.VertexShader, "data/default3d.vert"),
				new ShaderStage(ShaderType.FragmentShader, "data/defaultFlatColor.frag"));

			/// Load cube
			WorldSurface = new RenderModel(Obj.Load("data/models/cube/cube.obj"));
			WorldSurface.SetMaterialTexture("cube", Texture.FromFile("data/textures/xy_grid.png"));
			WorldSurface.Matrix = Matrix4x4.CreateTranslation(new Vector3(0.5f, -0.5f, 0.5f)) * Matrix4x4.CreateScale(new Vector3(Treadmill_X, 5, Treadmill_Z));
			WorldSurface.Matrix *= Matrix4x4.CreateTranslation(-Program.WorldOrigin);

			// Load pin
			GenericMesh[] Meshes = Obj.Load("data/models/biplane/biplane.obj");

			Matrix4x4 RotMat = Matrix4x4.CreateFromYawPitchRoll(-(float)Math.PI, 0, 0);
			for (int i = 0; i < Meshes.Length; i++)
				Meshes[i].ForEachPosition((In) => Vector3.Transform(In, RotMat));//*/

			Plane = new RenderModel(Meshes, false, false);
			Texture WhiteTex = Texture.FromFile("data/textures/colors/white.png");
			for (int i = 0; i < Meshes.Length; i++) {
				Plane.SetMaterialTexture(Meshes[i].MaterialName, WhiteTex);
				Plane.GetMaterialMesh(Meshes[i].MaterialName).DefaultColor = GfxUtils.RandomColor();
			}

			// Pin
			Pin = new RenderModel(Obj.Load("data/models/pin/pin.obj"), true, false);
			PinMat1 = Texture.FromFile("data/models/pin/pin_mat.png");
			PinMat2 = Texture.FromFile("data/models/pin/pin_mat2.png");

			// Points
			Points = new Mesh3D(BufferUsage.StreamDraw);
			Points.PrimitiveType = PrimitiveType.Points;
		}

		public static bool Tick() {
			if (SWatch == null)
				SWatch = Stopwatch.StartNew();

			while (SWatch.ElapsedMilliseconds / 1000.0f < (1.0f / Framerate))
				;

			Dt = SWatch.ElapsedMilliseconds / 1000.0f;
			SWatch.Restart();

			Update(Dt);

			ShaderUniforms.Model = Matrix4x4.Identity;
			Gfx.Clear();
			Draw(Dt);
			RWnd.SwapBuffers();
			Events.Poll();

			if (RWnd.ShouldClose)
				return false;
			return true;
		}

		static void Update(float Dt) {
			const float MoveSpeed = 500;

			if (!(MoveVec.X == 0 && MoveVec.Y == 0 && MoveVec.Z == 0))
				Cam.Position += Cam.ToWorldNormal(Vector3.Normalize(MoveVec)) * MoveSpeed * Dt;

			if (Program.RenderVoxels)
				Voxels.Update();
		}

		static void DrawPin(Texture T, Vector3 Pos) {
			SetPinTex(T);
			Pin.Matrix = Matrix4x4.CreateScale(15) * Matrix4x4.CreateTranslation(Pos);
			Pin.Draw(Default);
		}

		static void Draw(float Dt) {
			WorldSurface.Draw(Default);

			if (Program.RenderVoxels) {
				ShaderUniforms.Model = Matrix4x4.CreateTranslation(-Program.WorldOrigin);
				DefaultFlatColor.Bind();
				Voxels.VoxMesh.Draw();
				DefaultFlatColor.Unbind();
			}

			Matrix4x4 TransRot = OptotrakClient.GetRotation() * OptotrakClient.GetTranslation();
			ShaderUniforms.Model = Matrix4x4.CreateScale(10) * TransRot;
			Default.Bind();
			Plane.Draw();
			Default.Unbind();

			if (Program.RenderPoints) {
				RealSenseClient.GetVerts(ref Points);
				ShaderUniforms.Model = Matrix4x4.Identity;
				DefaultFlatColor.Bind();
				Points.Draw();
				DefaultFlatColor.Unbind();
			}

			DrawPin(PinMat1, LegClient.R_Start - Program.WorldOrigin);
			DrawPin(PinMat2, LegClient.R_End - Program.WorldOrigin);

			DrawPin(PinMat1, LegClient.L_Start - Program.WorldOrigin);
			DrawPin(PinMat2, LegClient.L_End - Program.WorldOrigin);
		}
	}
}
