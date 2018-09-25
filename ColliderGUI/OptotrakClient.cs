using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FishGfx;

namespace ColliderGUI {
	public class CircularVectorBuffer { //Stores an array of 10 vectors and averages them (a running average filter)
		const int Len = 10; // Can be set to n

		Vector3[] Vectors;
		int CurIdx;

		public CircularVectorBuffer() {
			Vectors = new Vector3[Len];
			CurIdx = 0;
		}

		public void Push(Vector3 V) {
			Vectors[CurIdx++] = V;
			if (CurIdx >= Len)
				CurIdx = 0;
		}

		public Vector3 GetAverage() {
			Vector3 Ret = Vector3.Zero;

			for (int i = 0; i < Len; i++)
				Ret += Vectors[i];

			return Ret / Len;
		}

		public Vector3 PushGetAverage(Vector3 V) {
			Push(V);
			return GetAverage();
		}
	}

	public static unsafe class OptotrakClient {
		const bool FakePosition = false; // for debugging, return random values if no optotrack is connected

		static bool FirstItemReceived; // Flag used for feedback if the machine is getting optotrack data
		static UdpClient UDP;
		static int Port;

		public static Vector3 MarkerA;
		public static Vector3 MarkerB;
		public static Vector3 MarkerC;

		static void PrintInfo() { // Print in console IP and port of local machine
			string LocalIP = "0.0.0.0";

			using (Socket S = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP)) {
				S.Connect("8.8.8.8", 65530);
				LocalIP = (S.LocalEndPoint as IPEndPoint).Address.ToString();
			}

			Console.WriteLine(ConsoleColor.Yellow, "Listening for Optotrak packets on {0}:{1}", LocalIP, Port);
		}

		public static void Init(int Port) {
			OptotrakClient.Port = Port; // Set the UDP port
			PrintInfo();
			FirstItemReceived = false;
            // Create a thread for UDP
			Thread WorkerThread = new Thread(() => {
				if (!FakePosition) {
					UDP = new UdpClient(Port);
					UDP.DontFragment = true;
				} else {
					MarkerA = MarkerB = MarkerC = new Vector3(0, 100, 0);
				}

				while (true) // The loop for the optotrack recieving program
					ReceiveVectors();
			});
			WorkerThread.IsBackground = true; // with this the thread stops when closing the GUI
			WorkerThread.Start(); // start the thread
        
		}

		public static byte[] ReceiveRaw() {
			IPEndPoint Sender = new IPEndPoint(IPAddress.Any, Port); // IPAddres.Any is for the local machine. It means it should listen to on all available network cards
		    return UDP.Receive(ref Sender); // If an IP filter is needed, add code to compare Sender.Address or something like that... (find online)

		}

        /// <summary>
        /// Average out the position of the 3 markers on the camera.
        /// </summary>
        /// <returns></returns>
		public static Vector3 GetPos() {
			// TODO: ?
			//return ((MarkerA + MarkerB + MarkerC) / 3) - new Vector3(-83, 0, -215);

			return ((MarkerA + MarkerB + MarkerC) / 3);
		}

		public static Matrix4x4 GetTranslation() {
			return Matrix4x4.CreateTranslation(GetPos());
		}

		public static Vector3 GetNormal() {
			if (FakePosition)
				return Vector3.Normalize(new Vector3(-1, 0.3f, 0.5f));

			Vector3 U = MarkerB - MarkerA; // Get vector from A to B
			Vector3 V = MarkerC - MarkerA; // Get vector from A to C

            // Manually calculate the cross product of U an V
			float X = U.Y * V.Z - U.Z * V.Y;
			float Y = U.Z * V.X - U.X * V.Z;
			float Z = U.X * V.Y - U.Y * V.X;
            // Normalize the cross product and return the normal
			return Vector3.Normalize(new Vector3(X, Y, Z));
		}

		public static Matrix4x4 GetRotation() {
			GetRotationAngles(out float Yaw, out float Pitch, out float Roll);
			return Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch, Roll);
		}

		public static void GetRotationAngles(out float Yaw, out float Pitch, out float Roll) {
			GetNormal().NormalToPitchYaw(out Pitch, out Yaw); // Get Yaw and pitch from the normal
			if (float.IsNaN(Pitch))
				Pitch = 0;
			if (float.IsNaN(Yaw))
				Yaw = 0;

			Matrix4x4.Invert(Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch, 0), out Matrix4x4 InvYawPitch);

            //Used to calculate the roll. From the marker positions and the Yaw and pitch angles
			Vector3 A = Vector3.Transform(MarkerA, InvYawPitch);
			Vector3 B = Vector3.Transform(MarkerB, InvYawPitch);
			Vector3 C = Vector3.Transform(MarkerC, InvYawPitch);
			Vector3 Center = (A + B + C) / 3;

			float XDiff = Center.X - A.X;
			float YDiff = Center.Y - A.Y;
			Roll = (float)(Math.Atan2(YDiff, XDiff) + Math.PI / 2);
		}

		static bool IsVisible(Vector3 V) {
			const float Threshold = 10000;

			if (V.X > Threshold || V.X < -Threshold)
				return false;

			return true;
		}

		static Stopwatch SWatch;
		static CircularVectorBuffer ABuffer = new CircularVectorBuffer(), BBuffer = new CircularVectorBuffer(), CBuffer = new CircularVectorBuffer();

		public static void ReceiveVectors() {
			if (FakePosition) {
				if (SWatch == null)
					SWatch = Stopwatch.StartNew();

				float Rad = 300;
				float TS = 1000.0f;
				Vector3 Pos = new Vector3((float)Math.Sin(SWatch.ElapsedMilliseconds / TS) * Rad, 100, (float)Math.Cos(SWatch.ElapsedMilliseconds / TS) * Rad);

				MarkerA = MarkerB = MarkerC = Pos;
				return;
			}

			byte[] Bytes = ReceiveRaw();
			//Console.WriteLine(Bytes.Length);

			Vector3* Vectors = stackalloc Vector3[3];
			Marshal.Copy(Bytes, 0, new IntPtr(Vectors), 3 * 3 * sizeof(float)); // Create vectors from bytes recieved over UDP

			Vector3 A = ABuffer.PushGetAverage(Vectors[0] + Program.OptotrakOffset); // Method in CircularVectorBuffer class. This basically performs the running average filter
			Vector3 B = BBuffer.PushGetAverage(Vectors[1] + Program.OptotrakOffset);
			Vector3 C = CBuffer.PushGetAverage(Vectors[2] + Program.OptotrakOffset);

			if (IsVisible(A)) // Checks for marker visibility. If it is not the last value is used.
				MarkerA = A.YZX();

			if (IsVisible(B))
				MarkerB = B.YZX();

			if (IsVisible(C))
				MarkerC = C.YZX();


			if (!FirstItemReceived) {
				FirstItemReceived = true;
				Console.WriteLine(ConsoleColor.Yellow, "Receiving Optotrak data! {0}", GetPos());
			}
		}
	}
}
