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

		public static void Init(float TargetFramerate, float Treadmill_X, float Treadmill_Z) {
			RenderWindow.Treadmill_X = Treadmill_X;
			RenderWindow.Treadmill_Z = Treadmill_Z;

			Framerate = TargetFramerate;
			Console.WriteLine(ConsoleColor.DarkCyan, "Target framerate: {0} FPS", TargetFramerate);

			Vector2 Res = RWnd.GetDesktopResolution() * 0.9f;
			RWnd = new RWnd((int)Res.X, (int)Res.Y, nameof(ColliderGUI));

			Console.WriteLine(ConsoleColor.Green, "Running {0}", RenderAPI.Version);
			Console.WriteLine(ConsoleColor.Green, RenderAPI.Renderer);
			Console.LogWriteLine("OpenGL Extensions:\n    {0}", string.Join("\n    ", RenderAPI.Extensions));

			RWnd.CaptureCursor = true;
			RWnd.OnMouseMoveDelta += (Wnd, X, Y) => {
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
			};

			SetupCamera();
			LoadAssets();
		}

		static void SetupCamera() {
			Cam = ShaderUniforms.Camera;
			Cam.MouseMovement = true;

			Cam.SetPerspective(RWnd.WindowSize.X, RWnd.WindowSize.Y);
			Cam.Position = new Vector3(-50, 100, -50);
			Cam.LookAt(new Vector3(Treadmill_X / 2, 0, Treadmill_Z / 2));
		}

		static ShaderProgram Default;
		static ShaderProgram DefaultFlatColor;
		static RenderModel Cube;

		static void LoadAssets() {
			Default = new ShaderProgram(new ShaderStage(ShaderType.VertexShader, "data/default3d.vert"),
				new ShaderStage(ShaderType.FragmentShader, "data/default.frag"));

			DefaultFlatColor = new ShaderProgram(new ShaderStage(ShaderType.VertexShader, "data/default3d.vert"),
				new ShaderStage(ShaderType.FragmentShader, "data/defaultFlatColor.frag"));

			Cube = new RenderModel(Obj.Load("data/models/cube/cube.obj"));
			Cube.SetMaterialTexture("cube", Texture.FromFile("data/textures/xy_grid.png"));
			Cube.Matrix = Matrix4x4.CreateTranslation(new Vector3(0.5f, -0.5f, 0.5f)) * Matrix4x4.CreateScale(new Vector3(Treadmill_X, 5, Treadmill_Z));
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

			Voxels.Update();
		}

		static void Draw(float Dt) {
			Cube.Draw(Default);

			ShaderUniforms.Model = Matrix4x4.Identity;
			DefaultFlatColor.Bind();
			Voxels.VoxMesh.Draw();
			DefaultFlatColor.Unbind();
		}
	}
}
