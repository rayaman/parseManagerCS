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
public class define : standardDefine // If you want the standard methods you must include this, Also this class cannot be static!
{
	public void testM(object arg1,object arg2)
	{
		Console.WriteLine(arg1+"\t"+arg2);
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
			parseManager test = new parseManager("parsetest2.txt","define"); // define is where your methods will be held
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