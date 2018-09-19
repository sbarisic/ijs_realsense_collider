using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using FishGfx.Graphics;
using FishGfx.Graphics.Drawables;
using FishGfx.Formats;
using FishGfx;

namespace ColliderGUI {
	enum VoxelType {
		Empty,
		Solid,
		NonSolid,
	}

	struct VoxelEntry {
		public static readonly VoxelEntry Empty = new VoxelEntry(VoxelType.Empty);

		public VoxelType Type;
		public Color Color;

		public VoxelEntry(VoxelType T, Color Clr) {
			Type = T;
			Color = Clr;
		}

		public VoxelEntry(VoxelType T) : this(T, Color.White) {
		}
	}

	static class Voxels {
		static float VoxelSize;
		static int Width;
		static int Depth;
		static int Height;

		static VoxelEntry[] VoxelArray;
		static bool Dirty;

		static Vertex3[] CubeVerts;

		static List<Vertex3> MeshVerts;
		public static Mesh3D VoxMesh;

		public static void Init(int Width, int Height, int Depth, float VoxelSize) {
			Voxels.Width = Width;
			Voxels.Height = Height;
			Voxels.Depth = Depth;
			Voxels.VoxelSize = VoxelSize;

			MeshVerts = new List<Vertex3>();
			VoxMesh = new Mesh3D(BufferUsage.DynamicDraw);
			VoxelArray = new VoxelEntry[Width * Height * Depth];

			Fill(VoxelEntry.Empty);

			CubeVerts = Obj.Load("data/models/cube/cube.obj").First().Vertices.ToArray();

			if (Program.MarkDirtyAuto)
				Dirty = true;
		}

		static int PosToIdx(int X, int Y, int Z) {
			return (Z * Width * Height) + (Y * Width) + X;
		}

		static void IdxToPos(int Idx, out int X, out int Y, out int Z) {
			Z = Idx / (Width * Height);
			Idx -= (Z * Width * Height);
			Y = Idx / Width;
			X = Idx % Width;
		}

		static void WorldCoordToPos(Vector3 WorldCoord, out int X, out int Y, out int Z) {
			WorldCoord /= VoxelSize;

			X = (int)WorldCoord.X;
			Y = (int)WorldCoord.Y;
			Z = (int)WorldCoord.Z;
		}

		public static VoxelEntry GetVoxel(int X, int Y, int Z) {
			if (X < 0 || X >= Width)
				return VoxelEntry.Empty;

			if (Y < 0 || Y >= Height)
				return VoxelEntry.Empty;

			if (Z < 0 || Z >= Depth)
				return VoxelEntry.Empty;

			return VoxelArray[PosToIdx(X, Y, Z)];
		}

		public static void SetVoxel(int X, int Y, int Z, VoxelEntry Vox) {
			if (X < 0 || X >= Width)
				return;

			if (Y < 0 || Y >= Height)
				return;

			if (Z < 0 || Z >= Depth)
				return;

			int Idx = PosToIdx(X, Y, Z);

			if (VoxelArray[Idx].Type != Vox.Type) {
				VoxelArray[Idx] = Vox;

				if (Program.MarkDirtyAuto)
					Dirty = true;
			}
		}

		public static void SetVoxel(Vector3 WorldCoords, VoxelEntry Vox) {
			WorldCoordToPos(WorldCoords, out int X, out int Y, out int Z);
			SetVoxel(X, Y, Z, Vox);
		}

		public static void Fill(VoxelEntry Vox) {
			for (int i = 0; i < VoxelArray.Length; i++)
				VoxelArray[i] = Vox;

			if (Program.MarkDirtyAuto)
				Dirty = true;
		}

		public static bool Ray(Vector3 WorldStart, Vector3 WorldEnd, OnRayCastLineSegment OnRayCast) {
			WorldCoordToPos(WorldStart, out int StartX, out int StartY, out int StartZ);
			WorldCoordToPos(WorldEnd, out int EndX, out int EndY, out int EndZ);
			return RayCast.RayCast3D(StartX, StartY, StartZ, EndX, EndY, EndZ, OnRayCast);
		}

		public static void Update() {
			if (!Dirty)
				return;
			Dirty = false;

			MeshVerts.Clear();

			for (int i = 0; i < VoxelArray.Length; i++) {
				IdxToPos(i, out int X, out int Y, out int Z);
				VoxelEntry T = VoxelEntry.Empty;

				if (Visible(T = GetVoxel(X, Y, Z))) {
					if (!Visible(X + 1, Y, Z) || !Visible(X - 1, Y, Z) || !Visible(X, Y + 1, Z) || !Visible(X, Y - 1, Z) || !Visible(X, Y, Z + 1) || !Visible(X, Y, Z - 1)) {
						EmitVoxel(T, X, Y, Z, MeshVerts);
					}
				}
			}

			if (MeshVerts.Count > 0)
				VoxMesh.SetVertices(MeshVerts.ToArray());
		}

		static void EmitVoxel(VoxelEntry T, int X, int Y, int Z, List<Vertex3> Verts) {
			for (int i = 0; i < CubeVerts.Length; i++) {
				Vertex3 V = CubeVerts[i];
				V.Position = (V.Position + new Vector3(0.5f) + new Vector3(X, Y, Z)) * VoxelSize;
				V.Color = T.Color;

				Verts.Add(V);
			}
		}

		public static void MarkDirty() {
			Dirty = true;
		}

		public static bool IsSolid(int X, int Y, int Z) {
			return IsSolid(GetVoxel(X, Y, Z));
		}

		public static bool IsSolid(VoxelEntry T) {
			if (T.Type == VoxelType.Solid)
				return true;

			return false;
		}

		public static bool Visible(int X, int Y, int Z) {
			return Visible(GetVoxel(X, Y, Z));
		}

		public static bool Visible(VoxelEntry T) {
			if (T.Type == VoxelType.Empty)
				return false;

			return true;
		}
	}
}
