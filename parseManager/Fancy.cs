/*
 * Created by SharpDevelop.
 * User: Ryan
 * Date: 8/27/2017
 * Time: 8:29 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace FancyPrintCS
{
	public static class Fancy
	{
		static char[][] fvars = {
			new char[]{ '╚', '═', '╝', '║', '╔', '╗', '╠', '╣', '╩', '╦', '╬' },
			new char[]{ '+', '═', '+', '|', '+', '+', '+', '+', '+', '+', '+' },
			new char[]{ '└', '─', '┘', '│', '┌', '┐', '├', '┤', '┴', '┬', '┼' },
			new char[]{ '+', '~', '+', '|', '+', '+', '+', '+', '+', '+', '+' },
			new char[]{ '+', '-', '+', '|', '+', '+', '+', '+', '+', '+', '+' },
			new char[]{ '╙', '─', '╜', '║', '╓', '╖', '╟', '╢', '╨', '╥', '╫' },
			new char[]{ '╘', '═', '╛', '│', '╒', '╕', '╞', '╡', '╧', '╤', '╪' }
		};
		static int form = 3;
		static int fvar;
		public static void SetForm(int n)
		{
			if (n < 1 || n > 3) {
				Console.WriteLine("Invalid int value! Only 1, 2 and 3");
			} else {
				form = n;
			}
		}
		public static void SetFVar(int f){
			if(f<0 || f>6){
				Console.WriteLine("Invalid int value! Only 0-7 is allowed!");
			} else {
				fvar=f;
			}
		}
		public static void SetForm(string n)
		{
			if (n.ToLower()=="left"){
				form=2;
			} else if(n.ToLower()=="right"){
				form=3;
			} else if(n.ToLower()=="center"){
				form=1;
			}
		}
		public static void Print(string[] msg)
		{
			int max = 0;
			var f = fvars[fvar];
			for (int i = 0; i < msg.Length; i++) {
				if (msg[i].Length > max) {
					max = msg[i].Length + 2;
				}
			}
			Console.WriteLine(f[4] + new String(f[1], max) + f[5]);
			string space1 = "";
			string space2 = "";
			for (int i = 0; i < msg.Length; i++) {
				if (form == 1) { // CENTER
					if ((max - 2) != msg[i].Length) {
						space1 = new String(' ', (max - msg[i].Length) / 2 + ((max - msg[i].Length) % 2));
						space2 = new String(' ', ((max - msg[i].Length) / 2));
					} else {
						space1 = new String(' ', (max - msg[i].Length) / 2 + ((max - msg[i].Length) % 2));
						space2 = new String(' ', ((max - msg[i].Length) / 2));
					}
				} else if (form == 2) { // LEFT
					space1 = "";
					space2 = new String(' ', max - msg[i].Length);
				} else if (form == 3) { // RIGHT
					space2 = "";
					space1 = new String(' ', max - msg[i].Length);
				}
				if (msg[i] == "/l") {
					Console.WriteLine(f[6] + new String(f[1], max) + f[7]);
				} else {
					Console.WriteLine(f[3] + space1 + msg[i] + space2 + f[3]);
				}
			}
			Console.WriteLine(f[0] + new String(f[1], max) + f[2]);
		}
		public static void Print(string msg)
		{
			var msgArr = msg.Split(',');
			Fancy.Print(msgArr);
		}
	}
}
