/*
 * Created by SharpDevelop.GetType
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
using parseManager;
namespace parseManager
{
	/// <summary>
	/// Description of ParseManager.
	/// </summary>
	public class parseManager
	{
		string _filepath;
		bool _hasDefine;
		string _define;
		string _entry = "START";
		Type _defineType;
		MethodInfo _defineMethod;
		object _defineClassObject;
		chunk _currentChunk;
		chunk _lastChunk = null;
		readonly ENV _mainENV = new ENV();
		public ENV _defualtENV;
		Dictionary<string, bool> _flags = new Dictionary<string, bool>();
		Dictionary<string, chunk> _chunks = new Dictionary<string, chunk>();
		Dictionary<string, string> _methods = new Dictionary<string, string>();
		void InitFlags()
		{
			_flags.Add("leaking", false);
			_flags.Add("forseelabels", true);
			_flags.Add("debugging", false);
			_flags.Add("topdown", true);
		}
		void debug(object msg)
		{
			if (_flags["debugging"])
				Console.WriteLine(msg);
		}
		void _Parse(string data)
		{
			string pattern = @"\[(.+)\][\r\n]*?\{([^\0]+?)\}";
			foreach (Match m in Regex.Matches(data, @"LOAD ([a-zA-Z0-9_\./]+)")) {
				Parse(m.Groups[1].ToString());
			}
			foreach (Match m in Regex.Matches(data, @"ENABLE ([a-zA-Z0-9_\./]+)")) {
				_flags[m.Groups[1].ToString()] = true;
			}
			foreach (Match m in Regex.Matches(data, @"ENTRY ([a-zA-Z0-9_\./]+)")) {
				_entry = m.Groups[1].ToString();
			}
			var match = Regex.Matches(data, pattern);
			foreach (Match m in match) {
				string Blck = m.Groups[1].ToString();
				string Cont = m.Groups[2].ToString();
				int loc = Blck.IndexOf(":");
				if (loc != -1) {
					_chunks[Blck.Substring(0, loc)] = new chunk(Blck.Substring(0, loc), Cont, Blck.Substring(loc + 1));
					Blck = Blck.Substring(0, loc);
				} else {
					_chunks[Blck] = new chunk(Blck, Cont);
				}
				if (_lastChunk != null) {
					_lastChunk.SetNextChunk(_chunks[Blck]);
				}
				_lastChunk = _chunks[Blck];
			}
		}
		void Parse()
		{
			try {
				StreamReader sr = File.OpenText(_filepath);
				_Parse(sr.ReadToEnd());
			} catch (FileNotFoundException) {
				Console.WriteLine("File '" + _filepath + "' does not exist! Loading failled!");
			}
		}
		void Parse(string filename)
		{
			try {
				StreamReader sr = File.OpenText(filename);
				_Parse(sr.ReadToEnd());
			} catch (FileNotFoundException) {
				Console.WriteLine("Load '" + filename + "' File not found. Loading failled!");
			}
		}
		public int RunCode(string code)
		{
			return 0; // TODO Add runcode stuff so constructs and functions can work!
		}
		public parseManager(string filepath)
		{
			InitFlags();
			_filepath = filepath;
			_hasDefine = false;
			_defualtENV = _mainENV;
			Parse();
		}
		public parseManager(string filepath, string define)
		{
			InitFlags();
			_define = define;
			_hasDefine = true;
			_filepath = filepath;
			_defineType = Type.GetType(define);
			ConstructorInfo defineConstructor = _defineType.GetConstructor(Type.EmptyTypes);
			_defineClassObject = defineConstructor.Invoke(new object[]{ });
			_defualtENV = _mainENV;
			Parse();
		}
		public object InvokeR(string method, object[] args)
		{ // TODO collect the returned arguments if any
			if (!_hasDefine)
				return null;
			_defineMethod = _defineType.GetMethod(method);
			return _defineMethod.Invoke(_defineClassObject, args);
		}
		public int InvokeNR(string method, object[] args)
		{ // Simple Invoking!
			if (!_hasDefine)
				return -1;
			_defineMethod = _defineType.GetMethod(method);
			_defineMethod.Invoke(_defineClassObject, args);
			return 0;
		}
		public void SetBlock(string BLOCK)
		{
			chunk cchunk;
			if (_chunks.TryGetValue(BLOCK, out cchunk)) {
				_currentChunk = cchunk;
			} else {
				PushError("Attempt to JUMP to a non existing block!");
			}
		}
		public ENV GetENV()
		{
			return _defualtENV;
		}
		public void SetENV()
		{
			_defualtENV = _mainENV;
		}
		public void SetENV(ENV o)
		{
			_defualtENV = o;
		}
		public void SetBlock(chunk BLOCK)
		{
			_currentChunk = BLOCK;
		}
		public void SetBlock()
		{
			chunk cchunk;
			if (_chunks.TryGetValue(_entry, out cchunk)) {
				_currentChunk = cchunk;
			} else {
				PushError("Entrypoint is Invalid!");
			}
		}
		public void PushError(string err)
		{
			Console.WriteLine(err);
		}
		public nextType Next(string BLOCK)
		{
			if (_currentChunk == null) {
				SetBlock(BLOCK);
			}
			return Next();
		}
		public nextType Next()
		{
			GLOBALS.SetPM(this);
			var tempReturn = new nextType();
			if (_currentChunk == null) {
				SetBlock();
			}
			var cCMD = _currentChunk.GetCLine();
			object[] stuff;
			if (cCMD == null) {
				if (_flags["leaking"]) {
					SetBlock(_currentChunk.GetNextChunk());
					return Next();
				}
				tempReturn.SetCMDType("EOF");
				tempReturn.SetText("Reached the end of the file!");
				return tempReturn;
			}
			var type = cCMD.GetCMDType();
			stuff = cCMD.GetArgs();
			if (type == "FUNC") {
				var func = (string)stuff[0];
				var args = (string[])stuff[1];
				if (args.Length == 1 && args[0] == "") { // assume no args inserted!
					InvokeNR(func, new object[]{ });
				} else {
					InvokeNR(func, ResolveVar(args));
				}
				tempReturn.SetCMDType("method");
				tempReturn.SetText("INVOKED METHOD: " + func);
			} else if (type == "LINE") {
				tempReturn.SetCMDType("line");
				tempReturn.SetText(parseHeader((string)stuff[0]));
			} else if (type == "FUNC_R") {
				var retargs = (string[])stuff[0];
				var func = (string)stuff[1];
				var args = (string[])stuff[2];
				object data;
				if (args.Length == 1 && args[0] == "") { // assume no args inserted!
					data = InvokeR(func, new object[]{ });
				} else {
					data = InvokeR(func, ResolveVar(args));
				}
				// For Now handle 1 var name to 1 value
				var env = GetENV();
				env[retargs[0]] = data;
			} else if(type=="ASSIGN"){
				var vars=(string[])stuff[0];
				var vals=(string[])stuff[1];
				var env = GetENV();
				var types=ResolveVar(vals);
				for(int i=0;i<types.Length;i++){
					env[vars[i]]=types[i];
				}
			}
			return tempReturn;
		}
		public string parseHeader(string header)
		{
			var results = Regex.Matches(header, @"(\$.*?\$)");
			int len = results.Count;
			string str;
			object temp;
			for (int i = 0; i < len; i++) {
				str = results[i].ToString();
				if (isVar(str.Substring(1, str.Length - 2), out temp)) {
					header = header.Replace(results[i].ToString(), temp.ToString());
				} else {
					header = header.Replace(results[i].ToString(), "null");
				}
			}
			return header;
		}
		public object[] ResolveVar(string[] v)
		{
			//_defualtENV
			var len = v.Length;
			var args = new object[len];
			object val;
			double num;
			bool boo;
			for (int i = 0; i < len; i++) {
				if (isVar(v[i], out val)) {
					args[i] = val;
				} else if (double.TryParse(v[i], out num)) {
					args[i] = num;
					debug("NUMBER: " + num);
				} else if (v[i][0] == '"' && v[i][v[i].Length - 1] == '"') {
					args[i] = parseHeader(v[i].Substring(1, v[i].Length - 2));
					debug("STRING: " + args[i]);
				} else if (bool.TryParse(v[i], out boo)) {
					args[i] = boo;
					debug("BOOL: " + boo);
				} else {
					args[i] = null;
				}
			}
			return args;
		}
		public bool isVar(string val, out object v)
		{
			if (_defualtENV[val] != null) {
				v = _defualtENV[val];
				return true;
			}
			v = null;
			return false;
		}
	}
	/*
	 * Helper Classes
	 */
	public class chunk
	{
		string _BLOCK;
		string _type;
		string _pureType;
		string[] _lines;
		List<CMD> _compiledlines = new List<CMD>();
		int _pos = 0;
		chunk _next = null;
		void _clean(string cont)
		{
			var m = Regex.Match(_type, @"([a-zA-Z0-9_]+)");
			_pureType = m.Groups[1].ToString();
			var tCont = Regex.Replace(cont, @"\-\-\[\[[\S\s]+\]\]", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\-\-.+\r\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\-\-.+\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\t", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\n\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\r\n\r\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\-\-\[\[[\S\s]+\]\]", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
			_lines = tCont.Split(new [] { "\r\n", "\n" }, StringSplitOptions.None);
			compile(); // compiles the code into something that can beused quickly
		}
		void compile()
		{
			string temp;
			for (int i = 0; i < _lines.Length - 1; i++) {
				temp = _lines[i];
				var FuncWReturn = Regex.Match(temp, "([\\[\\]\"a-zA-Z0-9_,]+)\\s?=\\s?([a-zA-Z0-9_]+)\\s?\\((.*)\\)");
				var FuncWOReturn = Regex.Match(temp, @"^([a-zA-Z0-9_]+)\s?\((.*)\)");
				var pureLine = Regex.Match(temp, "^\"(.+)\"");
				var assignment = Regex.Match(temp, "^([a-zA-Z0-9_,\\[\\]\"]+)=([a-zA-Z0-9_\",\\[\\]]+)");
				if (FuncWReturn.ToString() != "") {
					var var1 = (FuncWReturn.Groups[1]).ToString();
					var func = (FuncWReturn.Groups[2]).ToString();
					var args = (FuncWReturn.Groups[3]).ToString();
					var retargs = var1.Split(',');
					var result = Regex.Split(args, ",(?=(?:[^\"']*[\"'][^\"']*[\"'])*[^\"']*$)");
					_compiledlines.Add(new CMD("FUNC_R", new object[] { retargs, func, result }));
				} else if (FuncWOReturn.ToString() != "") {
					var func = (FuncWOReturn.Groups[1]).ToString();
					var args = (FuncWOReturn.Groups[2]).ToString();
					var result = Regex.Split(args, ",(?=(?:[^\"']*[\"'][^\"']*[\"'])*[^\"']*$)");
					_compiledlines.Add(new CMD("FUNC", new object[]{ func, result }));
				} else if (pureLine.ToString() != "") {
					_compiledlines.Add(new CMD("LINE", new object[]{ pureLine.ToString() }));
				} else if (assignment.ToString() != "") {
					var vars = Regex.Split(assignment.Groups[1].ToString(), ",(?=(?:[^\"']*[\"'][^\"']*[\"'])*[^\"']*$)");
					var vals = Regex.Split(assignment.Groups[2].ToString(), ",(?=(?:[^\"']*[\"'][^\"']*[\"'])*[^\"']*$)");
					_compiledlines.Add(new CMD("ASSIGN", new object[]{ vars, vals }));
				} else {
					_compiledlines.Add(new CMD("UNKNOWN", new object[]{ }));
				}
				//Console.WriteLine(_compiledlines[i].GetCMDType());
			}
		}
		public chunk(string name, string cont, string type)
		{
			_BLOCK = name;
			_type = type;
			_clean(cont);
		}
		public chunk(string name, string cont)
		{
			_BLOCK = name;
			_type = "CODEBLOCK";
			_clean(cont);
		}
		public void SetNextChunk(chunk next)
		{
			_next = next;
		}
		public chunk GetNextChunk()
		{
			return _next;
		}
		public chunk SetNextChunk()
		{
			return _next;
		}
		public string[] GetLines()
		{
			return _lines;
		}
		public string GetLine()
		{
			string temp = _lines[_pos];
			if (_pos == _lines.Length) {
				return null;
			}
			return temp;
		}
		public CMD GetCLine()
		{
			if (_pos < _compiledlines.Count) {
				return _compiledlines[_pos++];
			}
			return null;
		}
		public int GetPos()
		{
			return _pos;
		}
		public void SetPos(int n)
		{
			_pos = n;
		}
		public void ResetPos()
		{
			_pos = 0;
		}
		public string GetChunkPType()
		{
			return _pureType;
		}
		public string GetChunkType()
		{
			return _type;
		}
	}
	public class ENV
	{
		ENV _Parent;
		Dictionary<string, object> _vars = new Dictionary<string, object>();
		void SetParent(ENV other)
		{
			_Parent = other;
		}
		public bool TryGetValue(string ind, out object obj)
		{
			if (this[ind] != null) {
				obj = this[ind];
				return true;
			}
			obj = null;
			return false;
		}
		public object this[string ind] {
			get {
				object obj;
				if (_vars.TryGetValue(ind, out obj)) {
					return obj;
				}
				if (_Parent != null) {
					return _Parent[ind];
				} else {
					return null;
				}
			}
			set {
				_vars[ind] = value;
			}
		}
	}
	public class nextType
	{
		string _type;
		string _text;
		Dictionary<string, object> _other = new Dictionary<string, object>();
		public nextType()
		{
			_type = "UNSET";
		}
		public nextType(string type)
		{
			_type = type;
		}
		public string GetCMDType()
		{
			return _type;
		}
		public void SetText(string text)
		{
			_text = text;
		}
		public void SetCMDType(string type)
		{
			_type = type;
		}
		public string GetText()
		{
			return _text;
		}
		public object GetData(string name)
		{
			return _other[name];
		}
		public void AddData(string varname, object data)
		{
			_other[varname] = data;
		}
	}

	public class CMD
	{
		string _type;
		object[] _args;
		public CMD(string type, object[] args)
		{
			_type = type;
			_args = args;
		}
		public string GetCMDType()
		{
			return _type;
		}
		public object[] GetArgs()
		{
			return _args;
		}
	}
	/*
	 * The Standard Methods!
	 */
	static class GLOBALS
	{
		static parseManager current;
		static readonly ENV env = new ENV();
		public static object GetData(string ind)
		{
			return env[ind];
		}
		public static void AddData(string ind, object data)
		{
			env[ind] = data;
		}
		public static void SetPM(parseManager o)
		{
			current = o;
		}
		public static parseManager GetPM()
		{
			return current;
		}
	}
}
public class standardParseDefine
{
	public void EXIT()
	{
		// TODO Exit the script
	}
	public void QUIT()
	{
		// TODO Quit the script
	}
	public void GOTO(string label)
	{
		// TODO goto a label in the script
	}
	public void JUMP(string block)
	{
		// TODO jump to a block
	}
	public void SKIP(int n)
	{
		// TODO moves position of
	}
	public double ADD(double a, double b)
	{
		return a + b;
	}
	public double SUB(double a, double b)
	{
		return a - b;
	}
	public double MUL(double a, double b)
	{
		return a * b;
	}
	public double DIV(double a, double b)
	{
		return a / b;
	}
	public double MOD(double a, double b)
	{
		return a % b;
	}
}