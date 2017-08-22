/*
 * Created by SharpDevelop.
 * User: Ryan
 * Date: 8/17/2017
 * Time: 11:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using parseManager; // IMPORTANT
using NCalc;
public class define : standardDefine // If you want the standard methods you must include this, Also this class cannot be static!
{
	public void testM(object arg1)
	{
		Console.WriteLine(arg1);
	}
	public void testM2(string arg1)
	{
		Console.WriteLine(arg1 + " it works!!!");
	}
	public void TEST()
	{
		var test=GLOBALS.GetPM();
		var env=test.GetENV();
		Console.WriteLine(env["test"]);
	}
	public void TEST2(object msg)
	{
		Console.WriteLine(msg);
	}
	public void TEST3(double msg, string msg2)
	{
		Console.WriteLine(msg + "|" + msg2);
	}
	public double TEST4(double num){
		return num+1;
	}
}
namespace parseManager
{
	class Program
	{
		public static void Main(string[] args)
		{
			
			parseManager test = new parseManager("parsetest2.txt"); // define is where your methods will be held
			var env = test.GetENV();
			env["test"]="TEST!";
			env["test2"]=12345;
			nextType next = test.Next(); // TODO implement the next method
			string type;
			while(next.GetCMDType()!="EOF"){
				type = next.GetCMDType();
				if(type=="line"){
					Console.Write(next.GetText());
					Console.ReadLine();
				}
				next = test.Next();
			}
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
}