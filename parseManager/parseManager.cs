/*
 * User: Ryan
 * Date: 8/17/2017
 * Time: 11:54 AM
 */
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading;
using System.Windows.Input;
using FancyPrintCS;
using NAudio.Wave;
using parseManagerCS;
namespace parseManagerCS
{
	/// The parseManager is an Advance Config Script
	/// It allows the user to run code while also defining variables
	/// This also has very flexible flow control meaning you can use it for chat logic and such
	public class parseManager
	{
		public string _VERSION = "1.3.0";
		standardDefine _invoke = new standardDefine();
		string _filepath;
		bool _active = true;
		string _define = "NO_DEFINE";
		string _entry = "START";
		bool _isInternal;
		Type _defineType;
		bool isThread;
		standardDefine def = new standardDefine();
		MethodInfo _defineMethod;
		object _defineClassObject;
		chunk _currentChunk;
		chunk _lastChunk = null;
		ENV _mainENV = new ENV();
		public ENV _defualtENV;
		Stack<ENV> _fStack = new Stack<ENV>();
		Dictionary<string, bool> _flags = new Dictionary<string, bool>();
		Dictionary<string, chunk> _chunks = new Dictionary<string, chunk>();
		Dictionary<string, string> _methods = new Dictionary<string, string>();
		void INITENV()
		{
			GLOBALS.SetPM(this);
			_mainENV["Color_Black"] = ConsoleColor.Black;
			_mainENV["Color_Blue"] = ConsoleColor.Blue;
			_mainENV["Color_Cyan"] = ConsoleColor.Cyan;
			_mainENV["Color_DarkBlue"] = ConsoleColor.DarkBlue;
			_mainENV["Color_DarkCyan"] = ConsoleColor.DarkCyan;
			_mainENV["Color_DarkGray"] = ConsoleColor.DarkGray;
			_mainENV["Color_DarkGreen"] = ConsoleColor.DarkGreen;
			_mainENV["Color_DarkMagenta"] = ConsoleColor.DarkMagenta;
			_mainENV["Color_DarkRed"] = ConsoleColor.DarkRed;
			_mainENV["Color_DarkYellow"] = ConsoleColor.DarkYellow;
			_mainENV["Color_Gray"] = ConsoleColor.Gray;
			_mainENV["Color_Green"] = ConsoleColor.Green;
			_mainENV["Color_Magenta"] = ConsoleColor.Magenta;
			_mainENV["Color_Red"] = ConsoleColor.Red;
			_mainENV["Color_White"] = ConsoleColor.White;
			_mainENV["Color_Yellow"] = ConsoleColor.Yellow;
			_mainENV["VERSION"] = _VERSION;
		}
		public void _SetDENV(ENV env)
		{
			_mainENV = env;
		}
		public void makeThread()
		{
			isThread = true;
		}
		public bool isAThread()
		{
			return isThread;
		}
		public parseManager(string filepath)
		{
			InitFlags();
			_filepath = filepath;
			_defineType = Type.GetType("standardDefine");
			ConstructorInfo defineConstructor = _defineType.GetConstructor(Type.EmptyTypes);
			_defineClassObject = defineConstructor.Invoke(new object[]{ });
			_defualtENV = _mainENV;
			INITENV();
			Parse();
		}
		public parseManager(string filepath, string define)
		{
			InitFlags();
			_define = define;
			_filepath = filepath;
			_defineType = Type.GetType(define);
			ConstructorInfo defineConstructor = _defineType.GetConstructor(Type.EmptyTypes);
			_defineClassObject = defineConstructor.Invoke(new object[]{ });
			_defualtENV = _mainENV;
			INITENV();
			Parse();
		}
		public parseManager(string code, string define, bool c)
		{
			InitFlags();
			_define = define;
			_filepath = code;
			_isInternal = true;
			_defineType = Type.GetType(define);
			ConstructorInfo defineConstructor = _defineType.GetConstructor(Type.EmptyTypes);
			_defineClassObject = defineConstructor.Invoke(new object[]{ });
			_defualtENV = _mainENV;
			INITENV();
			Parse(code, c);
		}
		public parseManager(string code, bool c)
		{
			_isInternal = true;
			InitFlags();
			_filepath = code;
			_defineType = Type.GetType("standardDefine");
			ConstructorInfo defineConstructor = _defineType.GetConstructor(Type.EmptyTypes);
			_defineClassObject = defineConstructor.Invoke(new object[]{ });
			_defualtENV = _mainENV;
			INITENV();
			Parse(code, c);
		}
		public bool IsInternal()
		{
			return _isInternal;
		}
		public string GetFilepath()
		{
			return _filepath;
		}
		public string GetDefine()
		{
			return _define;
		}
		void InitFlags()
		{
			_flags.Add("leaking", false);
			_flags.Add("forseelabels", true);
			_flags.Add("debugging", false);
			_flags.Add("topdown", true);
			_flags.Add("casesensitive", true);
			_flags.Add("strictsyntax",false);
		}
		public ENV Pop()
		{
			return _fStack.Pop();
		}
		public bool GetFlag(string flag)
		{
			bool f;
			if (_flags.TryGetValue(flag, out f)) {
				return f;
			}
			return false;
		}
		public void debug(object msg)
		{
			if (_flags["debugging"])
				Console.WriteLine("DEBUGGING: " + msg);
		}
		
		void _Parse(string data, string hFile)
		{
			foreach (Match m in Regex.Matches(data, @"LOAD ([a-zA-Z0-9_\./]+)")) {
				Parse(m.Groups[1].ToString());
			}
			foreach (Match m in Regex.Matches(data, @"ENABLE ([a-zA-Z0-9_\./]+)")) {
				_flags[m.Groups[1].ToString().ToLower()] = true;
			}
			foreach (Match m in Regex.Matches(data, @"DISABLE ([a-zA-Z0-9_\./]+)")) {
				_flags[m.Groups[1].ToString().ToLower()] = false;
			}
			foreach (Match m in Regex.Matches(data, @"ENTRY ([a-zA-Z0-9_\./]+)")) {
				_entry = m.Groups[1].ToString();
			}
			foreach (Match m in Regex.Matches(data, @"VERSION ([a-zA-Z0-9_\./]+)")) {
				if (Version.Parse(m.Groups[1].ToString()) > Version.Parse(_VERSION)) {
					PushError("Attempt to run a code created for a greater version of the interperter/compiler! Script's Version: "+Version.Parse(m.Groups[1].ToString())+" Interperter's Version: "+Version.Parse(_VERSION));
				}
			}
			foreach (Match m in Regex.Matches(data, @"THREAD ([a-zA-Z0-9_\./]+)")) {
				def._newThread(this, m.Groups[1].ToString());
			}
			data = data + "\n";
			var match = Regex.Matches(data, "\\[(.+)\\][\r\n]*?\\{([^\0]+?)\\}\r?\n");
			var count = 0;
			foreach (Match m in match) {
				string Blck = m.Groups[1].ToString();
				string Cont = m.Groups[2].ToString();
				int loc = Blck.IndexOf(":", StringComparison.Ordinal);
				if (loc != -1) {
					_chunks[Blck.Substring(0, loc)] = new chunk(Blck.Substring(0, loc), Cont, Blck.Substring(loc + 1));
					Blck = Blck.Substring(0, loc);
				} else {
					_chunks[Blck] = new chunk(Blck, Cont);
				}
				count++;
				_chunks[Blck].SetHostFile(hFile);
				if (_lastChunk != null)
					_lastChunk.SetNextChunk(_chunks[Blck]);
				_lastChunk = _chunks[Blck];
			}
		}
		void Parse()
		{
			try {
				StreamReader sr = File.OpenText(_filepath);
				_Parse(sr.ReadToEnd(), _filepath);
				sr.Close();
			} catch (FileNotFoundException) {
				PushError("File '" + _filepath + "' does not exist!");
			}
		}
		void Parse(string code, bool c)
		{
			_Parse(code, "Internally Parsed Code!");
		}
		void Parse(string filename)
		{
			try {
				StreamReader sr = File.OpenText(filename);
				_Parse(sr.ReadToEnd(), filename);
				sr.Close();
			} catch (FileNotFoundException) {
				PushError("Could not load '" + _filepath + "' it does not exist!");
			}
		}
		public void ParseCode(string code)
		{
			_Parse(code, "Internally Parsed Code!");
		}
		public chunk[] GetChunks()
		{
			var chunks = _chunks.Values;
			var temp = new chunk[_chunks.Count];
			var i = 0;
			foreach (var item in _chunks) {
				temp[i] = item.Value;
				i++;
			}
			return temp;
		}
		public chunk GetCurrentChunk()
		{
			return _currentChunk;
		}
		public void Deactivate()
		{
			_active = false;
		}
		public bool GetLogic(parseManager PM, string log)
		{
			var test2 = PM.Logic(log);
			var te = evaluater.Evaluate(PM, test2);
			if (te > 0) {
				return true;
			} else {
				return false;
			}
		}
		public bool isRegisteredFunction(string method, out chunk o)
		{
			if (_chunks.TryGetValue(method, out o)) {
				if (o.isFunction()) {
					return true;
				}
				return false;
			}
			return false;
		}
		public object InvokeI(string method, object[] argsV, chunk c, bool rets)
		{
			var ccP = _currentChunk.GetPos();
			var ccN = _currentChunk.GetName();
			var argsN = c.GetArgs();
			var fEnv = new ENV();
			fEnv.SetParent(_defualtENV);
			if (!(argsN.Length == 1 && argsN[0] == "")) {
				for (int i = 0; i < argsN.Length; i++) {
					fEnv[argsN[i]] = argsV[i];
				}
			}
			var tempEnv = new ENV();
			tempEnv[0] = ccN;
			tempEnv[1] = ccP;
			tempEnv[2] = _defualtENV;
			_fStack.Push(tempEnv);
			if (_fStack.Count > 1024) {
				PushError("Stack Overflow!");
			}
			_defualtENV = fEnv;
			def.JUMP(this, method);
			if (rets) {
				return fEnv; // TODO Handle returns
			} else {
				return null;
			}
		}
		public object InvokeR(string method, object[] args)
		{
			chunk c;
			if (isRegisteredFunction(method, out c)) {
				return InvokeI(method, args, c, true);
			}
			try {
				_defineMethod = _defineType.GetMethod(method);
				return _defineMethod.Invoke(_defineClassObject, tackBArgs(this, args));
			} catch (Exception e) {
				var tests = e.ToString();
				if (tests.Contains("Null")) {
					PushError("Invalid method: " + method + " (Method does not exist! Check your spelling!)");
				} else if (tests.Contains("ArgumentException")) {
					PushError("Invalid method: " + method + " (Check your arguments! Ensure the types are correct!)");
				}
				PushError("Invalid method: " + method + " (Unknown Error! It just doesn't work!)\n\n" + e);
				return null;
			}
		}
		public int InvokeNR(string method, object[] args)
		{
			chunk c;
			if (isRegisteredFunction(method, out c)) {
				InvokeI(method, args, c, false);
				return 0;
			}
			try {
				_defineMethod = _defineType.GetMethod(method);
				_defineMethod.Invoke(_defineClassObject, tackBArgs(this, args));
				return 0;
			} catch (Exception e) {
				var tests = e.ToString();
				if (tests.Contains("Null")) {
					PushError("Invalid method: " + method + " (Method does not exist! Check your spelling!)");
				} else if (tests.Contains("ArgumentException")) {
					PushError("Invalid method: " + method + " (Check your arguments! Ensure the types are correct!)");
				}
				PushError("Invalid method: " + method + " (Unknown Error! It just doesn't work!)\n\n" + e);
				return -1;
			}
		}
		public object[] tackBArgs(object o, object[] args)
		{
			var len = args.Length;
			var newargs = new object[len + 1];
			for (int i = 0; i < len; i++) {
				newargs[i + 1] = args[i];
			}
			newargs[0] = o;
			return newargs;
		}
		public void SetBlock(string BLOCK)
		{
			chunk cchunk;
			if (_chunks.TryGetValue(BLOCK, out cchunk)) {
				_currentChunk = cchunk;
				_currentChunk.ResetPos();
			} else {
				PushError("Attempt to JUMP to a non existing block: \""+BLOCK+"\"");
			}
		}
		public ENV GetENV()
		{
			if (_defualtENV == null) {
				return _mainENV;
			}
			return _defualtENV;
		}
		public ENV GetDENV()
		{
			return _mainENV;
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
				_currentChunk.ResetPos();
			} else {
				PushError("Entrypoint is Invalid!");
			}
		}
		public void PushError(string err)
		{
			if (_currentChunk==null){
				Console.WriteLine(err+"\nPress Enter");
				Console.ReadLine();
				Environment.Exit(0);
			}
			var line = _currentChunk.GetCurrentLine();
			var file = _currentChunk.GetHostFile();
			var sr = File.OpenText(file);
			var code = sr.ReadToEnd();
			var haschunk = false;
			var chunk = _currentChunk.GetName();
			code = Regex.Replace(code, @"^\t+", "", RegexOptions.Multiline);
			var lines = code.Split(new [] { "\r\n", "\n" }, StringSplitOptions.None);
			var pos = 0;
			for (int i = 0; i < lines.Length; i++) {
				if (!haschunk && lines[i].StartsWith("[" + chunk)) {
					haschunk = true;
				}
				if (lines[i].StartsWith(line) && haschunk) {
					pos = i + 1;
					break;
				}
			}
			Console.WriteLine(string.Format("Error in File: {0} on Line: {1}\nLIQ: {2}\n\nERROR: {3}\nPress Enter!", file, pos, line, err));
			Console.ReadLine();
			Environment.Exit(0);
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
				if (_flags["leaking"] && _active) {
					var test = _currentChunk.GetNextChunk();
					if (test != null) {
						SetBlock(_currentChunk.GetNextChunk());
						return Next();
					}
				}
				tempReturn.SetCMDType("EOF");
				tempReturn.SetText("Reached the end of the file!");
				return tempReturn;
			}
			if (!_active) {
				tempReturn.SetCMDType("EOF");
				tempReturn.SetText("Reached the end of the file!");
				return tempReturn;
			}
			var type = cCMD.GetCMDType();
			stuff = cCMD.GetArgs();
			if (type == "LOGIC") {//{conds,andors,_funcif,_resultif,_funcelse,_resultelse}
				var conds = (string)stuff[0];
				var funcif = (string)stuff[1];
				var argsif = (string[])stuff[2];
				var funcelse = (string)stuff[3];
				var argselse = (string[])stuff[4];
				var truth = GetLogic(this, conds);
				if (truth) {
					InvokeNR(funcif, ResolveVar(argsif));
				} else {
					InvokeNR(funcelse, ResolveVar(argselse));
				}

				tempReturn.SetCMDType("conditional");
				tempReturn.SetText("test turned out to be: " + truth);
				return tempReturn;
			} else if (type == "LABEL") {
				tempReturn.SetCMDType("label");
				tempReturn.SetText("Jumped to a label!");
				return tempReturn;
			}
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
				return tempReturn;
			} else if (type == "LINE") {
				tempReturn.SetCMDType("line");
				var test = parseHeader((string)stuff[0]);
				tempReturn.SetText(test.Substring(1, test.Length - 2));
				return tempReturn;
			} else if (type == "FUNC_R") {
				var retargs = (string[])stuff[0];
				var func = (string)stuff[1];
				var args = (string[])stuff[2];
				object data;
				var env = GetENV();
				if (args.Length == 1 && args[0] == "") { // assume no args inserted!
					data = InvokeR(func, new object[]{ });
				} else {
					data = InvokeR(func, ResolveVar(args));
				}
				env[retargs[0]] = data;
				GLOBALS.Add_Var(retargs[0]);
				tempReturn.SetCMDType("method");
				tempReturn.SetText("INVOKED METHOD: " + func);
				return tempReturn;
			} else if (type == "ASSIGN") { // TODO add lists/dictonaries support
				var vars = (string[])stuff[0];
				var vals = (string[])stuff[1];
				var env = GetENV();
				var types = ResolveVar(vals);
				AssignmentHandler(vars, types);
				tempReturn.SetCMDType("assignment");
				tempReturn.SetText(_currentChunk.GetLine());
				return tempReturn;
			} else {
				var b = GetFlag("strictsyntax");
				if (b){
					PushError("INVALID SYNTAX!");
				}
			}
			return tempReturn;
		}
		public void AssignmentHandler(string[] vars, object[] types)
		{
			var env = GetENV();
			for (int i = 0; i < types.Length; i++) {
				var test = vars[i];
				if (test.EndsWith("]")) {
					var dict = Regex.Match(test, @"(.*)\[(.+)\]");
					var _var = dict.Groups[1].Value;
					var _val = dict.Groups[2].Value;
					var val = ResolveVar(new []{ _val });
					var _e = env[_var];
					if (!_e.GetType().ToString().Contains("ENV")) {
						PushError("Attempted to index a object that isn't a table!");
					} else {
						var e = (ENV)_e;
						if (val[0].GetType().ToString().Contains("Double")) {
							e[int.Parse(val[0].ToString())] = types[i];
						} else if (val[0].GetType().ToString().Contains("String")) {
							e[(string)val[0]] = types[i];
						} else {
							PushError("Invalid index type: " + val[0]);
						}
					}
				} else {
					env[vars[i]] = types[i];
					GLOBALS.Add_Var(vars[i]);
				}
			}
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
			double ex;
			for (int i = 0; i < len; i++) {
				if (v[i] == "true") {
					args[i] = true;
					continue;
				}
				if (v[i] == "false") {
					args[i] = false;
					continue;
				}
				if (!v[i].StartsWith("[") && !v[i].StartsWith("\""))
					ex = evaluater.Evaluate(this, v[i]);
				else
					ex = double.NaN;
				if (v[i].Length == 0 && len == 1) {
					return new object[]{ };
				}
				if (v[i] == "[]") {
					args[i] = new ENV();
					continue;
				}
				if (v[i].StartsWith("[")) {
					var result = GLOBALS.Split(v[i].Substring(1, v[i].Length - 2)); // TODO make ENV
					var res = ResolveVar(result);
					var env = new ENV();
					for (int g = 0; g < res.Length; g++) {
						env[g] = res[g];
					}
					args[i] = env;
					debug("TABLE: " + env);
				} else if (v[i].EndsWith("]")) {
					var dict = Regex.Match(v[i], @"(.*)\[(.+)\]");
					var _var = dict.Groups[1].Value;
					var _val = dict.Groups[2].Value;
					var val2 = ResolveVar(new []{ _val });
					var env = GetENV();
					var _e = env[_var];
					if (!_e.GetType().ToString().Contains("ENV")) {
						PushError("Attempted to index a object that isn't a table!");
					} else {
						var e = (ENV)_e;
						if (val2[0].GetType().ToString().Contains("Double")) {
							args[i] = e[int.Parse(val2[0].ToString())];
						} else if (val2[0].GetType().ToString().Contains("String")) {
							args[i] = e[(string)val2[0]];
						} else {
							PushError("Invalid index type: " + val2[0]);
						}
					}
				} else if (isVar(v[i], out val)) {
					args[i] = val;
					debug("RETREVING SAVED VAL");
				} else if (double.TryParse(v[i], out num)) {
					args[i] = num;
					debug("NUMBER: " + num);
				} else if (v[i][0] == '"' && v[i][v[i].Length - 1] == '"') {
					args[i] = parseHeader(v[i].Replace("\"", ""));
					debug("STRING: " + args[i]);
				} else if (bool.TryParse(v[i], out boo)) {
					args[i] = boo;
					debug("BOOL: " + boo);
				} else if (!double.IsNaN(ex)) {
					args[i] = ex;
				} else {
					args[i] = null;
				}
			}
			return args;
		}
		public int resolveLogic(bool b)
		{
			if (b) {
				return 1;
			} else {
				return 0;
			}
		}
		public bool resolveLogic(int b)
		{
			if (b == 1) {
				return true;
			} else {
				return true;
			}
		}
		public string Logic(string log)
		{
			log = Regex.Replace(log, "and", "*");
			log = Regex.Replace(log, "or", "+");
			foreach (Match m in Regex.Matches(log,@"\(?\s*(\S+?)\s*([=!><]+)\s*(\S+)\s*\)?")) {
				var a = m.Groups[1].Value;
				var b = m.Groups[2].Value;
				var c = m.Groups[3].Value;
				a = Regex.Replace(a, @"\(", "");
				c = Regex.Replace(c, @"\)", "");
				if (b == "==") {
					debug("==");
					log = Regex.Replace(log, a + "\\s*" + b + "\\s*" + c, resolveLogic(ResolveVar(new []{ a })[0].ToString() == ResolveVar(new []{ c })[0].ToString()).ToString());
				} else if (b == ">=") {
					debug(">=");
					log = Regex.Replace(log, a + "\\s*" + b + "\\s*" + c, resolveLogic(double.Parse(ResolveVar(new []{ a })[0].ToString()) >= double.Parse(ResolveVar(new []{ c })[0].ToString())).ToString());
				} else if (b == "<=") {
					debug("<=");
					log = Regex.Replace(log, a + "\\s*" + b + "\\s*" + c, resolveLogic(double.Parse(ResolveVar(new []{ a })[0].ToString()) <= double.Parse(ResolveVar(new []{ c })[0].ToString())).ToString());
				} else if (b == ">") {
					debug(">");
					log = Regex.Replace(log, a + "\\s*" + b + "\\s*" + c, resolveLogic(double.Parse(ResolveVar(new []{ a })[0].ToString()) > double.Parse(ResolveVar(new []{ c })[0].ToString())).ToString());
				} else if (b == "<") {
					debug("<");
					log = Regex.Replace(log, a + "\\s*" + b + "\\s*" + c, resolveLogic(double.Parse(ResolveVar(new []{ a })[0].ToString()) < double.Parse(ResolveVar(new []{ c })[0].ToString())).ToString());
				} else if (b == "!=") {
					debug("!=");
					log = Regex.Replace(log, a + "\\s*" + b + "\\s*" + c, resolveLogic(ResolveVar(new []{ a })[0].ToString() != ResolveVar(new []{ c })[0].ToString()).ToString());
				}
			}
			return log;
		}
		public bool isVar(string val, out object v)
		{
			debug("TESTING: " + val);
			if (_defualtENV == null) {
				_defualtENV = _mainENV;
			}
			debug("GETTING VAL FROM ENV: " + _defualtENV[val]);
			if (_defualtENV[val] != null) {
				v = _defualtENV[val];
				return true;
			}
			if (val.EndsWith("]")) {
				var dict = Regex.Match(val, @"(.*)\[(.+)\]");
				var _var = dict.Groups[1].Value;
				var _val = dict.Groups[2].Value;
				var val2 = ResolveVar(new []{ _val });
				var env = GetENV();
				var _e = env[_var];
				if (!_e.GetType().ToString().Contains("ENV")) {
					PushError("Attempted to index a object that isn't a table!");
				} else {
					var e = (ENV)_e;
					if (val2[0].GetType().ToString().Contains("Double")) {
						v = e[int.Parse(val2[0].ToString())];
						return true;
					} else if (val2[0].GetType().ToString().Contains("String")) {
						v = e[(string)val2[0]];
						return true;
					} else {
						PushError("Invalid index type: " + val2[0]);
					}
				}
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
		string[] args;
		bool isFunc;
		string _hostfile;
		Dictionary<string, int> _labels = new Dictionary<string, int>();
		List<CMD> _compiledlines = new List<CMD>();
		int _pos = 0;
		chunk _next = null;
		public string[] GetArgs()
		{
			return args;
		}
		public chunk(string name, string cont, string type)
		{
			_BLOCK = name;
			_type = type;
			_clean(cont);
		}
		public bool isFunction()
		{
			return isFunc;
		}
		public chunk(string name, string cont)
		{
			_BLOCK = name;
			_type = "CODEBLOCK";
			_clean(cont);
		}
		public void SetHostFile(string file)
		{
			_hostfile = file;
		}
		public string GetHostFile()
		{
			return _hostfile;
		}
		void _clean(string cont)
		{
			var m = Regex.Match(_type, @"([a-zA-Z0-9_]+)");
			_pureType = m.Groups[1].ToString();
			var tCont = Regex.Replace(cont, @"\-\-\[\[[\S\s]+\]\]", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"^\t+", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\-\-.+\r\n", "\r\n", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\-\-.+\n", "\n", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\n\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\r\n\r\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\-\-\[\[[\S\s]+\]\]", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
			_lines = tCont.Split(new [] { "\r\n", "\n" }, StringSplitOptions.None);
			compile();
		}
		void compile()
		{
			string temp;
			for (int i = 0; i < _lines.Length - 1; i++) {
				temp = _lines[i];
				var pureLine = Regex.Match(temp, "^\"(.+)\"");
				var FuncTest = Regex.Match(temp, @"([a-zA-Z0-9_]+)\s?\((.*)\)");
				var assignment = Regex.Match(temp, "^([a-zA-Z0-9_,\\[\\]\"]+)\\s*=([a-zA-Z\\s\\|&\\^\\+\\-\\*/%0-9_\",\\[\\]]*)");
				var FuncWReturn = Regex.Match(temp, "([\\[\\]\"a-zA-Z0-9_,]+)\\s?=\\s?([a-zA-Z0-9_]+)\\s?\\((.*)\\)");
				var FuncWOReturn = Regex.Match(temp, @"^([a-zA-Z0-9_]+)\s?\((.*)\)");
				var label = Regex.Match(temp, "::(.*)::");
				var logic = Regex.Match(temp, @"if\s*(.+)\s*then\s*(.+?\))\s*\|\s*(.+?\))");
				if (logic.ToString() != "") {
					var condition = logic.Groups[1].ToString();
					var tempif = logic.Groups[2].ToString();
					var tempelse = logic.Groups[3].ToString();
					var argsif = Regex.Match(tempif, @"^([a-zA-Z0-9_]+)\s?\((.*)\)");
					var argselse = Regex.Match(tempelse, @"^([a-zA-Z0-9_]+)\s?\((.*)\)");
					string _funcif = (argsif.Groups[1]).ToString();
					var _argsif = (argsif.Groups[2]).ToString();
					string[] _resultif = GLOBALS.Split(_argsif);
					string _funcelse = (argselse.Groups[1]).ToString();
					var _argselse = (argselse.Groups[2]).ToString();
					string[] _resultelse = GLOBALS.Split(_argselse);
					var mm = Regex.Matches(condition, "(.+?)([and ]+?[or ]+)");
					var conds = new string[(mm.Count + 1) * 3];
					var andors = new string[mm.Count];
					var count = 0;
					var p = 0;
					var p2 = 0;
					foreach (Match m in mm) {
						var s1 = m.Groups[1].ToString();
						var s1p = Regex.Match(s1, "(.+?)([~!><=]+)+(.+)");
						var s1a = s1p.Groups[1].ToString();
						var s1b = s1p.Groups[2].ToString();
						var s1c = s1p.Groups[3].ToString();
						var s2 = m.Groups[2].ToString();
						conds[p++] = s1a;
						conds[p++] = s1b;
						conds[p++] = s1c;
						andors[p2++] = s2.Substring(1, 1);
						count += s1.Length + s2.Length;
					}
					var s1p2 = Regex.Match(condition.Substring(count, condition.Length - count - 1), "(.+?)([~!><=]+)+(.+)");
					var s1a2 = s1p2.Groups[1].ToString();
					var s1b2 = s1p2.Groups[2].ToString();
					var s1c2 = s1p2.Groups[3].ToString();
					conds[p++] = s1a2;
					conds[p++] = s1b2;
					conds[p++] = s1c2;
					_compiledlines.Add(new CMD("LOGIC", new object[] {
						condition,
						_funcif,
						_resultif,
						_funcelse,
						_resultelse
					}));
				} else if (label.ToString() != "") {
					_labels[label.Groups[1].ToString()] = i;
					_compiledlines.Add(new CMD("LABEL", new object[]{ }));
				} else if (FuncWReturn.ToString() != "") {
					var var1 = (FuncWReturn.Groups[1]).ToString();
					var func = (FuncWReturn.Groups[2]).ToString();
					var args2 = (FuncWReturn.Groups[3]).ToString();
					var retargs = GLOBALS.Split(var1);
					var result = GLOBALS.Split(args2);
					_compiledlines.Add(new CMD("FUNC_R", new object[] {
						retargs,
						func,
						result
					}));
				} else if (FuncWOReturn.ToString() != "") {
					var func = (FuncWOReturn.Groups[1]).ToString();
					var args2 = (FuncWOReturn.Groups[2]).ToString();
					var result = GLOBALS.Split(args2);
					_compiledlines.Add(new CMD("FUNC", new object[]{ func, result }));
				} else if (pureLine.ToString() != "") {
					_compiledlines.Add(new CMD("LINE", new object[]{ pureLine.ToString() }));
				} else if (assignment.ToString() != "") {
					var vars = GLOBALS.Split(assignment.Groups[1].ToString());
					var tabTest = assignment.Groups[2].ToString();
					string[] vals = GLOBALS.Split(tabTest);
					//vals = Regex.Split(assignment.Groups[2].ToString(), ",(?=(?:[^\"'\\[\\]]*[\"'\\[\\]][^\"'\\[\\]]*[\"'\\[\\]])*[^\"'\\[\\]]*$)");
					_compiledlines.Add(new CMD("ASSIGN", new object[]{ vars, vals }));
				} else {
					_compiledlines.Add(new CMD("UNKNOWN", new object[]{ }));
				}
			}
			if (_type.Contains("function")) {
				isFunc = true;
				var func = Regex.Match(_type, @"\((.+)\)");
				args = GLOBALS.Split(func.Groups[1].Value);
				_compiledlines.Add(new CMD("FUNC", new object[] {
					"TRACEBACK",
					new string[]{ }
				})); // Append the traceback method to the chunk
			}
		}
		public string GetName()
		{
			return _BLOCK;
		}
		public int GetLabel(string lab)
		{
			return _labels[lab];
		}
		public bool TryGetLabel(string lab, out int pos)
		{
			int p;
			if (_labels.TryGetValue(lab, out p)) {
				pos = p;
				return true;
			}
			pos = -1;
			return false;
		}
		public void RemoveNextChunk()
		{
			_next = null;
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
		public string GetCurrentLine()
		{
			string temp="";
			if (_pos == 0) {
				temp = _lines[_pos+2];
			} else {
				temp = _lines[_pos - 1];
			}
			if (_pos - 1 == _lines.Length) {
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
	public static class evaluater
	{
		public static double Evaluate(parseManager PM, string str)
		{
			string oldstr = str;
			object temp;
			double temp2;
			foreach (Match m in Regex.Matches(str, "([a-zA-Z0-9_]+)")) {
				if (!double.TryParse(m.Groups[0].Value, out temp2)) {
					temp2 = double.NaN;
				}
				if (PM.isVar(m.Groups[0].Value, out temp)) {
					if (temp.GetType().ToString().Contains("Double")) {
						str = str.Replace(m.Groups[0].Value, ((double)temp).ToString());
					} else {
						return double.NaN;
					}
				} else if (!double.IsNaN(temp2)) {
					str = str.Replace(m.Groups[0].Value, temp2.ToString());
				} else {
					PM.PushError("Variable \""+m.Groups[0].Value+"\" does not exist: ");
				}
			}
			double result;
			try {
				result = Convert.ToDouble(new DataTable().Compute(str, null));
			} catch {
				result = double.NaN;
			}
			return result;
		}
	}
	[Serializable]
	public class ENV
	{
		ENV _Parent;
		Dictionary<string, object> _vars = new Dictionary<string, object>();
		Dictionary<int, object> _varsI = new Dictionary<int, object>();
		public void SetParent(ENV other)
		{
			_Parent = other;
		}
		public object[] GetList()
		{
			var temp = new object[_varsI.Count];
			var count = 0;
			foreach (KeyValuePair<int, object> entry in _varsI) {
				temp[count] = entry.Value;
				count++;
			}
			return temp;
		}
		public override string ToString()
		{
			string str = "(";
			foreach (KeyValuePair<string, object> entry in _vars) {
				var val = entry.Value;
				if (val.GetType().ToString().Contains("ENV")) {
					val = "ENV";
				}
				str += "[\"" + entry.Key + "\"] = " + val + ", ";
			}
			foreach (KeyValuePair<int, object> entry in _varsI) {
				str += "[" + entry.Key + "] = " + entry.Value + ", ";
			}
			return str + ")";
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
		public bool TryGetValue(int ind, out object obj)
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
				if (!GLOBALS.GetFlag("casesensitive")) {
					ind = ind.ToLower();
				}
				if (_vars.TryGetValue(ind, out obj)) {
					return obj;
				}
				if (_Parent != null) {
					return _Parent[ind];
				}
				return null;
			}
			set {
				if (!GLOBALS.GetFlag("casesensitive")) {
					ind = ind.ToLower();
				}
				_vars[ind] = value;
			}
		}
		public object this[int ind] {
			get {
				object obj;
				if (_varsI.TryGetValue(ind, out obj)) {
					return obj;
				}
				if (_Parent != null) {
					return _Parent[ind];
				}
				return null;
			}
			set {
				_varsI[ind] = value;
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
		static standardDefine _define = new standardDefine();
		static parseManager _current;
		static parseManager _main;
		static readonly ENV _env = new ENV();
		static List<string> _numvars = new List<string>();
		static List<parseManager> _Threads = new List<parseManager>();
		public static void AddThread(parseManager PM)
		{
			_Threads.Add(PM);
		}
		public static void FixThreads(parseManager PM)
		{
			var PMS = _Threads.ToArray();
			var env = PM.GetDENV();
			for (int i = 0; i < PMS.Length; i++) {
				PMS[i]._SetDENV(env);
				PMS[i].SetENV(env);
			}
		}
		public static void WriteToBinaryFile(string filePath, ENV objectToWrite, bool append = false)
		{
			using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create)) {
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				binaryFormatter.Serialize(stream, objectToWrite);
			}
		}
		public static ENV ReadFromBinaryFile(string filePath)
		{
			using (Stream stream = File.Open(filePath, FileMode.Open)) {
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				return (ENV)binaryFormatter.Deserialize(stream);
			}
		}
		public static object GetData(string ind)
		{
			return _env[ind];
		}
		public static standardDefine GetDefine()
		{
			return _define;
		}
		public static void AddData(string ind, object data)
		{
			_env[ind] = data;
		}
		public static void SetPM(parseManager o)
		{
			_current = o;
		}
		public static void SetMainPM(parseManager o)
		{
			_main = o;
		}
		public static parseManager GetPM()
		{
			return _current;
		}
		public static parseManager GetMainPM(){
			return _main;
		}
		public static bool GetFlag(string flag)
		{
			var PM = GetPM();
			return PM.GetFlag(flag);
		}
		public static void Add_Var(string var)
		{
			if (!_numvars.Contains(var)) {
				_numvars.Add(var);
			}
		}
		public static void Remove_Var(string var)
		{
			_numvars.Remove(var);
		}
		public static string[] GetVars()
		{
			return _numvars.ToArray();
		}
		public static string[] Split(string split)
		{
			var res = new List<string>();
			var state = 0;
			var c = ".";
			var elem = "";
			for (int gg = 0; gg < split.Length; gg++) {
				c = split.Substring(gg, 1);
				if (state == 3 || state == 0) {
					if (state == 3 && c == " ") {
						state = 0;
					} else {
						state = 0;
						if (c == "\"" || c == "'") {
							state = 1;
							elem += "\"";
						} else if (c == "[") {
							state = 1;
							elem += "[";
						} else if (c == ",") {
							res.Add(elem);
							elem = "";
							state = 3;
						} else {
							elem += c;
						}
					}
				} else if (state == 1) {
					if (c == "\"" || c == "'") {
						state = 0;
						elem += "\"";
					} else if (c == "]") {
						state = 0;
						elem += "]";
					} else if (c == "\\") {
						state = 2;
					} else {
						elem += c;
					}
				} else if (state == 2) {
					elem += c;
					state = 1;
				}
			}
			res.Add(elem);
			return res.ToArray();
		}
	}
}
public class standardDefine
{
	List<IWavePlayer> devices = new List<IWavePlayer>();
	int count = -1;
	Random rnd = new Random();
	int origRow = Console.CursorTop;
	int origCol = Console.CursorLeft;
	public void newThread(parseManager PM, string Block)
	{
		var thread = new Thread(() => _THREAD(Block, PM));
		thread.Start();
	}
	public void _THREAD(string block, parseManager _PM)
	{
		var define = _PM.GetDefine();
		var path = _PM.GetFilepath();
		parseManager PM;
		if (_PM.IsInternal()) {
			if (define == "NO_DEFINE") {
				PM = new parseManager(path, true);
			} else {
				PM = new parseManager(path, define, true);
			}
		} else {
			if (define == "NO_DEFINE") {
				PM = new parseManager(path);
			} else {
				PM = new parseManager(path, define);
			}
		}
		GLOBALS.AddThread(PM);
		PM.makeThread();
		PM._SetDENV(_PM.GetDENV());
		PM.SetENV(_PM.GetENV());
		nextType next = PM.Next(block);
		string type;
		while (next.GetCMDType() != "EOF") {
			type = next.GetCMDType();
			next = PM.Next();
		}
	}
	public void _newThread(parseManager PM, string filename)
	{
		var thread = new Thread(() => _THREAD(PM,filename));
		thread.Start();
	}
	public void _THREAD(parseManager _PM, string filename)
	{
		parseManager PM = new parseManager(filename);
		GLOBALS.AddThread(PM);
		PM.makeThread();
		PM._SetDENV(_PM.GetDENV());
		PM.SetENV(_PM.GetENV());
		nextType next = PM.Next();
		string type;
		while (next.GetCMDType() != "EOF") {
			type = next.GetCMDType();
			next = PM.Next();
		}
	}
	public void SAVE(parseManager PM)
	{
		if (PM.isAThread()) {
			GLOBALS.GetMainPM().PushError("Cannot Call SAVE() in a thread!");
		}
		var env = PM.GetDENV();
		var c = PM.GetCurrentChunk();
		env["__CurrentChunkName"] = c.GetName();
		env["__CurrentChunkPos"] = c.GetPos();
		env["__DefualtENV"] = PM.GetENV();
		GLOBALS.WriteToBinaryFile("savedata.dat", env);
	}
	public bool LOAD(parseManager PM)
	{
		if (PM.isAThread()) {
			GLOBALS.GetMainPM().PushError("Cannot Call LOAD() in a thread!");
		}
		try {
			ENV env = GLOBALS.ReadFromBinaryFile("savedata.dat");
			var name = (string)env["__CurrentChunkName"];
			var pos = (int)env["__CurrentChunkPos"];
			var denv = (ENV)env["__DefualtENV"];
			PM._SetDENV(env);
			PM.SetENV(denv);
			PM.SetBlock(name);
			PM.GetCurrentChunk().SetPos(pos);
			GLOBALS.FixThreads(PM);
			return true;
		} catch {
			return false;
		}
	}
	public void TRACEBACK(parseManager PM)
	{
		ENV env = PM.Pop();
		PM.SetBlock((string)env[0]);
		var c = PM.GetCurrentChunk();
		c.SetPos((int)env[1]);
		setENV(PM, (ENV)env[3]);
	}
	public void EXIT(parseManager PM)
	{
		cleanUpAudio();
		PM.Deactivate();
	}
	public void QUIT(parseManager PM)
	{
		cleanUpAudio();
		Environment.Exit(0);
	}
	public void setENV(parseManager PM, ENV env)
	{
		PM.SetENV(env);
	}
	public ENV getENV(parseManager PM)
	{
		return PM.GetENV();
	}
	public ENV getDefualtENV(parseManager PM)
	{
		return PM.GetDENV();
	}
	public ENV createENV(parseManager PM)
	{
		var temp = new ENV();
		temp.SetParent(PM.GetENV());
		return temp;
	}
	public string getInput(parseManager PM)
	{
		return Console.ReadLine();
	}
	public void setCC(parseManager PM)
	{
		Console.SetCursorPosition(0, Console.CursorTop - 1);
	}
	public void whiteOut(parseManager PM)
	{
		for (int i = 0; i < Console.BufferWidth; i++) {
			Console.Write(" ");
		}
	}
	public void setBG(parseManager PM, ConsoleColor BG)
	{
		Console.BackgroundColor = BG;
	}
	public void setFG(parseManager PM, ConsoleColor FG)
	{
		Console.ForegroundColor = FG;
	}
	public void resetColor(parseManager PM)
	{
		Console.ResetColor();
	}
	public int GOTO(parseManager PM, string label)
	{
		var c = PM.GetCurrentChunk();
		int pos;
		if (c.TryGetLabel(label, out pos)) {
			c.SetPos(pos);
			return 0;
		} else if (PM.GetFlag("forseelabels")) {
			var chunks = PM.GetChunks();
			for (int i = 0; i < chunks.Length; i++) {
				if (chunks[i].TryGetLabel(label, out pos)) {
					PM.SetBlock(chunks[i].GetName());
					chunks[i].SetPos(pos);
					return 0;
				}
			}
		}
		PM.PushError("Unable to GOTO a non existing label: \"" + label + "\"");
		return 0;
	}
	public double len(parseManager PM, object o)
	{
		string type = o.GetType().ToString();
		if (type.Contains("String")) {
			return (double)((string)o).Length;
		}
		if (type.Contains("ENV")) {
			return (double)((ENV)o).GetList().Length;
		}
		return 0;
	}
	public void JUMP(parseManager PM, string block)
	{
		var c = PM.GetCurrentChunk();
		c.ResetPos();
		PM.SetBlock(block);
	}
	public void SKIP(parseManager PM, double n)
	{
		var c = PM.GetCurrentChunk();
		var pos = c.GetPos();
		c.SetPos(pos + (int)n);
	}
	public double tonumber(parseManager PM, string strn)
	{
		double d;
		if (double.TryParse(strn, out d)) {
			return d;
		}
		PM.debug("Cannot convert to a number!");
		return double.NaN;
	}
	public void sleep(parseManager PM, double n)
	{
		Thread.Sleep((int)n);
	}
	public void setVar(parseManager PM, string var, object value)
	{
		var env = PM.GetDENV();
		env[var] = value;
	}
	public double ADD(parseManager PM, double a, double b)
	{
		return a + b;
	}
	public double SUB(parseManager PM, double a, double b)
	{
		return a - b;
	}
	public double MUL(parseManager PM, double a, double b)
	{
		return a * b;
	}
	public double DIV(parseManager PM, double a, double b)
	{
		return a / b;
	}
	public double MOD(parseManager PM, double a, double b)
	{
		return a % b;
	}
	public double CALC(parseManager PM, string ex)
	{
		return evaluater.Evaluate(PM, ex);
	}
	public void pause(parseManager PM)
	{
		Console.ReadLine();
	}
	public void print(parseManager PM, object o)
	{
		Console.WriteLine(o);
	}
	public double random(parseManager PM, double s, double e)
	{
		return (double)rnd.Next((int)s, (int)e);
	}
	public double rand(parseManager PM)
	{
		return rnd.NextDouble();
	}
	public double round(parseManager PM, double num, double n)
	{
		return Math.Round(num, (int)n);
	}
	public void clear(parseManager PM)
	{
		Console.Clear();
	}
	public void write(parseManager PM, object o)
	{
		Console.Write(o);
	}
	public void backspace(parseManager PM)
	{
		Console.Write("\b");
	}
	public void beep(parseManager PM)
	{
		Console.Beep();
	}
	public void fancy(parseManager PM, string msg)
	{
		Fancy.Print(msg);
	}
	public void setFancyForm(parseManager PM, string form)
	{
		Fancy.SetForm(form);
	}
	public void setFancyType(parseManager PM, double f)
	{
		Fancy.SetFVar((int)f);
	}
	public void stopSong(parseManager PM, double id)
	{
		devices[(int)id].Stop();
	}
	public void playSong(parseManager PM, double id)
	{
		devices[(int)id].Play();
	}
	public void resumeSong(parseManager PM, double id)
	{
		devices[(int)id].Play();
	}
	public void setSongVolume(parseManager PM, double id, double vol)
	{
		devices[(int)id].Volume = (float)vol;
	}
	public void pauseSong(parseManager PM, double id)
	{
		devices[(int)id].Pause();
	}
	public void replaySong(parseManager PM, double id)
	{
		devices[(int)id].Stop();
		devices[(int)id].Play();
	}
	public double loadSong(parseManager PM, string filepath)
	{
		count++;
		devices.Add(new WaveOut());
		var temp = new AudioFileReader(filepath);
		devices[count].Init(temp);
		return count;
	}
	void cleanUpAudio()
	{
		for (int i = 0; i < devices.Count; i++) {
			devices[i].Stop();
			devices[i].Dispose();
		}
	}
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
			PM.PushError(e.Message);
		}
	}
	public void setWindowSize(parseManager PM, double x,double y){
		Console.SetWindowSize((int)x,(int)y);
	}
	public double getConsoleWidth(parseManager PM){
		return Console.WindowWidth;
	}
	public double getConsoleHeight(parseManager PM){
		return Console.WindowHeight;
	}
	public bool isDown(parseManager PM, string key)
	{
		if (!ApplicationIsActivated()) {
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
		} else if (k == "{LSHIFT}") {
			kk = Key.LeftShift;
		} else if (k == "{RSHIFT}") {
			kk = Key.RightShift;
		} else if (k == "0") {
			kk = Key.D0;
		} else if (k == "1") {
			kk = Key.D1;
		} else if (k == "2") {
			kk = Key.D2;
		} else if (k == "3") {
			kk = Key.D3;
		} else if (k == "4") {
			kk = Key.D4;
		} else if (k == "5") {
			kk = Key.D5;
		} else if (k == "6") {
			kk = Key.D6;
		} else if (k == "7") {
			kk = Key.D7;
		} else if (k == "8") {
			kk = Key.D8;
		} else if (k == "9") {
			kk = Key.D9;
		} else if (k == "{SPACE}") {
			kk = Key.Space;
		}
		return Keyboard.IsKeyDown(kk);
	}
	public void error(parseManager PM, string msg)
	{
		PM.PushError(msg);
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