/*
 * Created by SharpDevelop.
 * User: Ryan
 * Date: 8/17/2017
 * Time: 11:54 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
namespace parseManager
{
	/// <summary>
	/// Description of parseManager.
	/// </summary>
	public class parseManager
	{
		string filepath;
		bool hasDefine;
		string define;
		string entry="START";
		Type defineType;
		MethodInfo defineMethod;
		object defineClassObject;
		Dictionary<string, string> chunks = new Dictionary<string, string>();
		private void parse(){
			try
		    {
			    StreamReader sr = File.OpenText (filepath);
				string CDFDATA = sr.ReadToEnd ();
				string pattern = @"\[(.+)\][\r\n]*?\{([^\0]+?)\}";
				var match = Regex.Matches( CDFDATA, pattern );
				foreach (Match m in match)
					chunks.Add (m.Groups [1].ToString (), m.Groups [2].ToString ());
		    }
		    catch (FileNotFoundException ex)
		    {
		    	Console.WriteLine("File '"+filepath+"' does not exist!\n"+ex);
		    }
			
		}
		public parseManager(string filepath)
		{
			this.filepath=filepath;
			hasDefine=false;
			parse();
		}
		public parseManager(string filepath,string define){
			this.define=define;
			hasDefine=true;
			this.filepath=filepath;
			defineType = Type.GetType(define);
			ConstructorInfo defineConstructor = defineType.GetConstructor(Type.EmptyTypes);
			defineClassObject = defineConstructor.Invoke(new object[]{});
			parse();
		}	
		public int invokeA(string method, object[] args){ // TODO collect the returned arguments if any
			if (!hasDefine)
				return -1;
			defineMethod = defineType.GetMethod(method);
			defineMethod.Invoke(defineClassObject, args);
			return 0;
		}
		public int invokeNA(string method, object[] args){ // Simple Invoking!
			if (!hasDefine)
				return -1;
			defineMethod = defineType.GetMethod(method);
			defineMethod.Invoke(defineClassObject, args);
			return 0;
		}
		public nextType next(){
			return new nextType("method");
		}
	}
	public class nextType
	{
		string type;
		string text;
		Dictionary<string, object> other = new Dictionary<string, object>();
		public nextType(string type){
			this.type=type;
		}
		public string getType(){
			return type;
		}
		public void setText(string text){
			this.text=text;
		}
		public string getText(){
			return text;
		}
		public object getData(string name){
			return other[name];
		}
		public void addData(string varname,object data){
			other.Add(varname,data);
		}
	}
}