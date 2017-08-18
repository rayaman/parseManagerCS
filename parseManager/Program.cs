/*
 * Created by SharpDevelop.
 * User: Ryan
 * Date: 8/17/2017
 * Time: 11:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
public class define : standardParseDefine // If you want the standard methods you must include this
{
	public void testM(string arg1)
	{
		Console.WriteLine(arg1);
	}
	public void testM2(string arg1)
	{
		Console.WriteLine(arg1 + " it works!!!");
	}
}
namespace parseManager
{
	class Program
	{
		public static void Main(string[] args)
		{
			parseManager test = new parseManager("parsetest2.txt", "define"); // define is where your methods will be held
			nextType next = test.Next(); // TODO implement the next method
			while(next.GetCMDType()!="EOF"){
				Console.Write(next.GetText());
				next = test.Next();
			}
			//var temp=test.InvokeR("TEST",new object[]{});
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
}