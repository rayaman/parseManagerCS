/*
 * Created by SharpDevelop.
 * User: Ryan
 * Date: 8/17/2017
 * Time: 11:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using parseManagerCS;
namespace parseManagerCS
{
	class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			//args=new string[]{"choiceTest.txt"};
			string file;
			string print = "";
			List<char> temp = new List<char>();
			parseManager PM;
			var cpath = Process.GetCurrentProcess().MainModule.FileName;
			int counter = 0;
			if (args.Length == 0) { // if we don't have args, let's check for an appended script!
				using (FileStream fs = new FileStream(cpath, FileMode.Open, FileAccess.Read)) {
					long offset;
					int nextByte;
					for (offset = 1; offset <= fs.Length; offset++) {
						fs.Seek(-offset, SeekOrigin.End);
						nextByte = fs.ReadByte();
						if (nextByte == 0) {
							break;
						}
						counter++;
					}
					if (counter == 0 && args.Length == 0) {
						Console.WriteLine("No appended code and no file path given!\nPress Emter!");
						Console.ReadLine();
						Environment.Exit(0);
					} else {
						fs.Close();
						using (var reader = new StreamReader(cpath))
						{
							reader.BaseStream.Seek(-counter, SeekOrigin.End);
							string line;
							while ((line = reader.ReadLine()) != null) {
								print+=line+"\n";
							}
						}
					}
				}
				PM = new parseManager(print, true);
			} else { // we have args so lets load it!
				file = args[0];
				PM = new parseManager(file);
			}
			GLOBALS.SetMainPM(PM);
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