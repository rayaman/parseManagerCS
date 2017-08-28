/*
 * Created by SharpDevelop.
 * User: Ryan
 * Date: 8/17/2017
 * Time: 11:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using parseManagerCS;
using System.Windows.Input;
public class define : standardDefine // If you want the standard methods you must include this, Also this class cannot be static!
{
	int origRow = Console.CursorTop;
	int origCol = Console.CursorLeft;
	public void setPosition(parseManager PM, double x, double y)
	{
		Console.SetCursorPosition((int)x, (int)y);
	}
	public void writeAt(parseManager PM, string s, double x, double y)
	{
		try {
			Console.SetCursorPosition(origCol + (int)x, origRow + (int)y);
			Console.Write(s);
		} catch (ArgumentOutOfRangeException e) {
			Console.Clear();
			Console.WriteLine(e.Message);
		}
	}
	public bool isDown(parseManager PM, string key)
	{
		if(!ApplicationIsActivated()){
			return false;
		}
		Key kk = Key.Zoom;
		var k = key.ToUpper();
		if (k == "A") {
			kk = Key.A;
		} else if (k == "B") {
			kk = Key.B;
		} else if (k == "C") {
			kk = Key.C;
		} else if (k == "D") {
			kk = Key.D;
		} else if (k == "E") {
			kk = Key.E;
		} else if (k == "F") {
			kk = Key.F;
		} else if (k == "G") {
			kk = Key.G;
		} else if (k == "H") {
			kk = Key.H;
		} else if (k == "I") {
			kk = Key.I;
		} else if (k == "J") {
			kk = Key.J;
		} else if (k == "K") {
			kk = Key.K;
		} else if (k == "L") {
			kk = Key.L;
		} else if (k == "M") {
			kk = Key.M;
		} else if (k == "N") {
			kk = Key.N;
		} else if (k == "O") {
			kk = Key.O;
		} else if (k == "P") {
			kk = Key.P;
		} else if (k == "Q") {
			kk = Key.Q;
		} else if (k == "R") {
			kk = Key.R;
		} else if (k == "S") {
			kk = Key.S;
		} else if (k == "T") {
			kk = Key.T;
		} else if (k == "U") {
			kk = Key.U;
		} else if (k == "V") {
			kk = Key.V;
		} else if (k == "W") {
			kk = Key.W;
		} else if (k == "X") {
			kk = Key.X;
		} else if (k == "Y") {
			kk = Key.Y;
		} else if (k == "Z") {
			kk = Key.Z;
		} else if (k == "{UP}") {
			kk = Key.Up;
		} else if (k == "{DOWN}") {
			kk = Key.Down;
		} else if (k == "{LEFT}") {
			kk = Key.Left;
		} else if (k == "{RIGHT}") {
			kk = Key.Right;
		} else if (k == "{ENTER}") {
			kk = Key.Enter;
		} else if (k == "{LSHIFT}"){
			kk = Key.LeftShift;
		} else if (k == "{RSHIFT}"){
			kk = Key.RightShift;
		} else if (k == "0"){
			kk = Key.D0;
		} else if (k == "1"){
			kk = Key.D1;
		} else if (k == "2"){
			kk = Key.D2;
		} else if (k == "3"){
			kk = Key.D3;
		} else if (k == "4"){
			kk = Key.D4;
		} else if (k == "5"){
			kk = Key.D5;
		} else if (k == "6"){
			kk = Key.D6;
		} else if (k == "7"){
			kk = Key.D7;
		} else if (k == "8"){
			kk = Key.D8;
		} else if (k == "9"){
			kk = Key.D9;
		} else if(k == "{SPACE}"){
			kk = Key.Space;
		}
		return Keyboard.IsKeyDown(kk);
	}
	public string isPressing(parseManager PM)
	{
		return Console.ReadKey(true).Key.ToString();
	}
	public static bool ApplicationIsActivated()
	{
		var activatedHandle = GetForegroundWindow();
		if (activatedHandle == IntPtr.Zero) {
			return false;       // No window is currently activated
		} else {
			var procId = Process.GetCurrentProcess().Id;
			int activeProcId;
			GetWindowThreadProcessId(activatedHandle, out activeProcId);
			return activeProcId == procId;
		}
	}
	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
}
namespace parseManagerCS
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
//			if (args.Length == 0) {
//				Console.Write("Please Include a file path!");
//				Console.ReadLine();
//				Environment.Exit(0);
//			}
			parseManager PM = new parseManager("parsetest2.txt", "define");
			nextType next = PM.Next();
			string type;
			while (next.GetCMDType() != "EOF") {
				type = next.GetCMDType();
				if (type == "line") {
					Console.Write(next.GetText());
					Console.ReadLine();
				}
				next = PM.Next();
			}
		}
	}
}