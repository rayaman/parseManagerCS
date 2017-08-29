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
namespace parseManagerCS
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			if (args.Length == 0) {
				Console.Write("Please Include a file path!");
				Console.ReadLine();
				Environment.Exit(0);
			}
			parseManager PM = new parseManager(args[0]);
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