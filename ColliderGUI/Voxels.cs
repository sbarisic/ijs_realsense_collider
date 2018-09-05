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
		None,
		Solid
	}

	struct VoxelEntry {
		public static readonly VoxelEntry None = new VoxelEntry(VoxelType.None);

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

			for (int i = 0; i < VoxelArray.Length; i++)
				VoxelArray[i] = VoxelEntry.None;
			//VoxelArray[i] = new VoxelEntry(VoxelType.Solid, GfxUtils.RandomColor());

			CubeVerts = Obj.Load("data/models/cube/cube.obj").First().Vertices.ToArray();
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
				return VoxelEntry.None;

			if (Y < 0 || Y >= Height)
				return VoxelEntry.None;

			if (Z < 0 || Z >= Depth)
				return VoxelEntry.None;

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
				Dirty = true;
			}
		}

		public static void SetVoxel(Vector3 WorldCoords, VoxelEntry Vox) {
			WorldCoordToPos(WorldCoords, out int X, out int Y, out int Z);
			SetVoxel(X, Y, Z, Vox);
		}

		public static void Update() {
			if (!Dirty)
				return;
			Dirty = false;

			MeshVerts.Clear();

			for (int i = 0; i < VoxelArray.Length; i++) {
				IdxToPos(i, out int X, out int Y, out int Z);
				VoxelEntry T = VoxelEntry.None;

				if (IsSolid(T = GetVoxel(X, Y, Z))) {
					if (!IsSolid(X + 1, Y, Z) || !IsSolid(X - 1, Y, Z) || !IsSolid(X, Y + 1, Z) || !IsSolid(X, Y - 1, Z) || !IsSolid(X, Y, Z + 1) || !IsSolid(X, Y, Z - 1)) {
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

		static bool IsSolid(int X, int Y, int Z) {
			return IsSolid(GetVoxel(X, Y, Z));
		}

		static bool IsSolid(VoxelEntry T) {
			if (T.Type == VoxelType.Solid)
				return true;

			return false;
		}
	}
}
