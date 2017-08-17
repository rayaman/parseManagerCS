/*
 * Created by SharpDevelop.
 * User: Ryan
 * Date: 8/17/2017
 * Time: 11:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;


public class define
{
    public void testM(string arg1)
    {
    	Console.WriteLine(arg1 + " it works!");
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
			Console.WriteLine("Hello World!");
			parseManager test = new parseManager("path","define");
			test.invokeA("testM",new object[]{""});
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
}