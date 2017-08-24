/*
 * User: Ryan
 * Date: 8/17/2017
 * Time: 11:54 AM
 */
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using parseManager;
namespace parseManager
{
	/// The parseManager is an Advance Config Script
	/// It allows the user to run code while also definine variables
	/// This also has very flexible flow control meaning you can use it for chat logic and such
	public class parseManager
	{
		standardDefine _invoke = new standardDefine();
		string _filepath;
		bool _active = true;
		string _define;
		string _entry = "START";
		Type _defineType;
		standardDefine def = new standardDefine();
		MethodInfo _defineMethod;
		object _defineClassObject;
		chunk _currentChunk;
		chunk _lastChunk = null;
		readonly ENV _mainENV = new ENV();
		public ENV _defualtENV;
		Stack<ENV> _fStack = new Stack<ENV>();
		Dictionary<string, bool> _flags = new Dictionary<string, bool>();
		Dictionary<string, chunk> _chunks = new Dictionary<string, chunk>();
		Dictionary<string, string> _methods = new Dictionary<string, string>();
		public parseManager(string filepath)
		{
			InitFlags();
			_filepath = filepath;
			_defineType = Type.GetType("standardDefine");
			ConstructorInfo defineConstructor = _defineType.GetConstructor(Type.EmptyTypes);
			_defineClassObject = defineConstructor.Invoke(new object[]{ });
			_defualtENV = _mainENV;
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
			Parse();
		}
		void InitFlags()
		{
			_flags.Add("leaking", false);
			_flags.Add("forseelabels", true);
			_flags.Add("debugging", false);
			_flags.Add("topdown", true);
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
		void debug(object msg)
		{
			if (_flags["debugging"])
				Console.WriteLine("DEBUGGING: " + msg);
		}
		void _Parse(string data)
		{
			foreach (Match m in Regex.Matches(data, @"LOAD ([a-zA-Z0-9_\./]+)")) {
				Parse(m.Groups[1].ToString());
			}
			foreach (Match m in Regex.Matches(data, @"ENABLE ([a-zA-Z0-9_\./]+)")) {
				_flags[m.Groups[1].ToString()] = true;
			}
			foreach (Match m in Regex.Matches(data, @"ENTRY ([a-zA-Z0-9_\./]+)")) {
				_entry = m.Groups[1].ToString();
			}
			var match = Regex.Matches(data, @"\[(.+)\][\r\n]*?\{([^\0]+?)\}");
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
				if (_lastChunk != null)
					_lastChunk.SetNextChunk(_chunks[Blck]);
				_lastChunk = _chunks[Blck];
			}
		}
		void Parse()
		{
			try {
				StreamReader sr = File.OpenText(_filepath);
				_Parse(sr.ReadToEnd());
				sr.Close();
			} catch (FileNotFoundException) {
				Console.WriteLine("File '" + _filepath + "' does not exist! Loading failled!");
			}
		}
		void Parse(string filename)
		{
			try {
				StreamReader sr = File.OpenText(filename);
				_Parse(sr.ReadToEnd());
				sr.Close();
			} catch (FileNotFoundException) {
				Console.WriteLine("Load '" + filename + "' File not found. Loading failled!");
			}
		}
		public void ParseCode(string code)
		{
			_Parse(code);
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
		public bool isRegisteredFunction(string method, out chunk o)
		{
			if (_chunks.TryGetValue(method, out o)) {
				return true;
			}
			return false;
		}
		public object InvokeI(string method, object[] argsV, chunk c)
		{
			var ccP = _currentChunk.GetPos();
			var ccN = _currentChunk.GetName();
			var argsN = c.GetArgs();
			var fEnv = new ENV();
			fEnv.SetParent(_defualtENV);
			for (int i = 0; i < argsN.Length; i++) {
				fEnv[argsN[i]] = argsV[i];
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
			def.JUMP(method);
			return fEnv; // TODO Handle returns
		}
		public object InvokeR(string method, object[] args)
		{
			chunk c;
			if (isRegisteredFunction(method, out c)) {
				return InvokeI(method, args, c);
			}
			try {
				_defineMethod = _defineType.GetMethod(method);
				return _defineMethod.Invoke(_defineClassObject, args);
			} catch {
				PushError("Invalid method: " + method);
				return null;
			}
		}
		public int InvokeNR(string method, object[] args)
		{
			chunk c;
			if (isRegisteredFunction(method, out c)) {
				InvokeI(method, args, c);
				return 0;
			} else {
				try {
					_defineMethod = _defineType.GetMethod(method);
					_defineMethod.Invoke(_defineClassObject, args);
					return 0;
				} catch {
					PushError("Invalid method: " + method);
				}
			}
			return -1;
		}
		public void SetBlock(string BLOCK)
		{
			chunk cchunk;
			if (_chunks.TryGetValue(BLOCK, out cchunk)) {
				_currentChunk = cchunk;
				_currentChunk.ResetPos();
			} else {
				PushError("Attempt to JUMP to a non existing block!");
			}
		}
		public ENV GetENV()
		{
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
			Console.WriteLine(err);
			def.EXIT();
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
			var type = cCMD.GetCMDType();
			stuff = cCMD.GetArgs();
			if (type == "LOGIC") {//{conds,andors,_funcif,_resultif,_funcelse,_resultelse}
				var conds = (string[])stuff[0];
				var andors = (string[])stuff[1];
				var funcif = (string)stuff[2];
				var argsif = (string[])stuff[3];
				var funcelse = (string)stuff[4];
				var argselse = (string[])stuff[5];
				var objs = new object[conds.Length]; // contain the actual values of what is in the env
				var truths = new bool[conds.Length / 3];
				var c = 0;
				for (int i = 0; i < conds.Length; i += 3) {
					var condA = (object)ResolveVar(new []{ conds[i] })[0];
					var e = conds[i + 1];
					var condB = (object)ResolveVar(new []{ conds[i + 2] })[0];
					if (e == "==") {
						truths[c] = condA.ToString() == condB.ToString();
					} else if (e == ">=") {
						truths[c] = (double)condA >= (double)condB;
					} else if (e == "<=") {
						truths[c] = (double)condA <= (double)condB;
					} else if (e == "!=" || e == "~=") {
						truths[c] = condA.ToString() != condB.ToString();
					} else if (e == ">") {
						truths[c] = (double)condA > (double)condB;
					} else if (e == "<") {
						truths[c] = (double)condA < (double)condB;
					} else {
						PushError("Invalid conditional test! " + e + " is not valid!");
					}
					c++;
				}
				var truth = truths[0];
				if (truths.Length == 1 && truth) {
					//Console.WriteLine(funcif+"|"+ResolveVar(argsif)[0]);
					InvokeNR(funcif, ResolveVar(argsif));
				} else if (truths.Length == 1) {
					//Console.WriteLine(funcelse + "|"+ResolveVar(argselse)[0]);
					InvokeNR(funcelse, ResolveVar(argselse));
				} else {
					for (int i = 1; i < andors.Length; i++) {
						if (andors[i - 1] == "a") {
							truth = truth && truths[i];
						} else if (andors[i - 1] == "o") {
							truth = truth || truths[i];
						} else {
							PushError("Invalid conditional test! " + andors[i - 1] + " is not valid!");
						}
					}
					if (truth) {
						//Console.WriteLine(funcif);
						InvokeNR(funcif, ResolveVar(argsif));
					} else {
						//Console.WriteLine("|" + funcelse + "|");
						InvokeNR(funcelse, ResolveVar(argselse));
					}
				}
				tempReturn.SetCMDType("conditional");
				tempReturn.SetText("test turned out to be: " + truth);
				return tempReturn;
			} else if (type == "LABEL") {
				cCMD = _currentChunk.GetCLine();
				if (cCMD == null) {
					if (_flags["leaking"] && _active) {
						SetBlock(_currentChunk.GetNextChunk());
						return Next();
					}
					tempReturn.SetCMDType("EOF");
					tempReturn.SetText("Reached the end of the file!");
					return tempReturn;
				}
				type = cCMD.GetCMDType();
				stuff = cCMD.GetArgs();
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
				if (!v[i].StartsWith("["))
					ex = evaluater.Evaluate(v[i]);
				else
					ex = double.NaN;
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
			compile();
		}
		void compile()
		{
			string temp;
			for (int i = 0; i < _lines.Length - 1; i++) {
				temp = _lines[i];
				var pureLine = Regex.Match(temp, "^\"(.+)\"");
				var FuncTest = Regex.Match(temp, @"([a-zA-Z0-9_]+)\s?\((.*)\)");
				var assignment = Regex.Match(temp, "^([a-zA-Z0-9_,\\[\\]\"]+)=([a-zA-Z\\|&\\^\\+\\-\\*/%0-9_\",\\[\\]]+)");
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
					string[] _resultif = Regex.Split(_argsif, ",(?=(?:[^\"'\\[\\]]*[\"'\\[\\]][^\"'\\[\\]]*[\"'\\[\\]])*[^\"'\\[\\]]*$)");
					string _funcelse = (argselse.Groups[1]).ToString();
					var _argselse = (argselse.Groups[2]).ToString();
					string[] _resultelse = Regex.Split(_argselse, ",(?=(?:[^\"'\\[\\]]*[\"'\\[\\]][^\"'\\[\\]]*[\"'\\[\\]])*[^\"'\\[\\]]*$)");
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
						conds,
						andors,
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
					var retargs = var1.Split(',');
					var result = Regex.Split(args2, ",(?=(?:[^\"'\\[\\]]*[\"'\\[\\]][^\"'\\[\\]]*[\"'\\[\\]])*[^\"'\\[\\]]*$)");
					_compiledlines.Add(new CMD("FUNC_R", new object[] {
						retargs,
						func,
						result
					}));
				} else if (FuncWOReturn.ToString() != "") {
					var func = (FuncWOReturn.Groups[1]).ToString();
					var args2 = (FuncWOReturn.Groups[2]).ToString();
					var result = Regex.Split(args2, ",(?=(?:[^\"'\\[\\]]*[\"'\\[\\]][^\"'\\[\\]]*[\"'\\[\\]])*[^\"'\\[\\]]*$)");
					_compiledlines.Add(new CMD("FUNC", new object[]{ func, result }));
				} else if (pureLine.ToString() != "") {
					_compiledlines.Add(new CMD("LINE", new object[]{ pureLine.ToString() }));
				} else if (assignment.ToString() != "") {
					var vars = Regex.Split(assignment.Groups[1].ToString(), ",(?=(?:[^\"']*[\"'][^\"']*[\"'])*[^\"']*$)");
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
					new string[]{}
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
		static double helper(ENV env, parseManager PM, string o, object _v, object _r)
		{
			double v = 0;
			double r = 0;
			if (_v.GetType().ToString() == "System.String") {
				object temp;
				if (env.TryGetValue((string)_v, out temp)) {
					v = (double)temp;
				} else if (double.TryParse((string)_v, out v)) {
					// We good!
				} else {
					PM.PushError("Attempt to do arithmetic on a null value: " + _v);
				}
			} else {
				v = (double)_v;
			}
			if (_r.GetType().ToString() == "System.String") {
				object temp;
				if (env.TryGetValue((string)_r, out temp)) {
					r = (double)temp;
				} else if (double.TryParse((string)_r, out r)) {
					// We good!
				} else {
					PM.PushError("Attempt to do arithmetic on a null value: " + _r);
				}
			} else {
				v = (double)_v;
			}
			// Constructs?
			if (o == "+") {
				return r + v;
			} else if (o == "-") {
				return r - v;
			} else if (o == "/") {
				return r / v;
			} else if (o == "*") {
				return r * v;
			} else if (o == "^") {
				return Math.Pow(r, v);
			} else if (o == "%") {
				return r % v;
			}
			return 0;
		}
		public static double Evaluate(string cmd)
		{
			return Evaluate(cmd, 0);
		}
		public static double Evaluate(string cmd, double v)
		{
			double test;
			if (double.TryParse(cmd, out test)) {
				return test;
			}
			var loop = false;
			if (cmd.Substring(0, 1) == "-") {
				cmd = "0" + cmd;
			}
			var PM = GLOBALS.GetPM();
			var env = PM.GetENV();
			var count = 0;
			var para = Regex.Match(cmd, @"(\(.+\))");
			if (para.ToString() != "") {
				return Evaluate(para.Groups[1].Value.Substring(1, para.Groups[1].Value.Length - 2));
			}
			foreach (Match m in Regex.Matches(cmd,@"(.*)([/%\+\^\-\*])(.*)")) {
				loop = true;
				count++;
				var l = m.Groups[1].Value;
				var o = m.Groups[2].Value;
				var r = m.Groups[3].Value;
				if (Regex.Match(l, @"([/%\+\^\-\*])").ToString() != "") {
					v = Evaluate(l, v);
					v = helper(env, PM, o, r, v);
				} else if (count == 1) {
					v = helper(env, PM, o, r, l);
				}
			}
			if (!loop) {
				object t;
				if (env != null)
				if (env.TryGetValue(cmd, out t)) {
					double te;
					if (t.GetType().ToString().Contains("Double"))
						return (double)t;
					if (t.GetType().ToString().Contains("String"))
					if (double.TryParse((string)t, out te)) {
						return te;
					}
					return double.NaN;
				} else {
					t = double.NaN; // It couldn't be done :'(
				}
			}
			return v; // TODO grab current ENV and does the calculation
		}
	}
	public class ENV
	{
		ENV _Parent;
		Dictionary<string, object> _vars = new Dictionary<string, object>();
		Dictionary<int, object> _varsI = new Dictionary<int, object>();
		public void SetParent(ENV other)
		{
			_Parent = other;
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
				if (_vars.TryGetValue(ind, out obj)) {
					return obj;
				}
				if (_Parent != null) {
					return _Parent[ind];
				}
				return null;
			}
			set {
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
		static readonly ENV _env = new ENV();
		static List<string> _numvars = new List<string>();
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
		public static parseManager GetPM()
		{
			return _current;
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
	public void TRACEBACK()
	{
		var test = GLOBALS.GetPM();
		ENV env = test.Pop();
		test.SetBlock((string)env[0]);
		var c = test.GetCurrentChunk();
		c.SetPos((int)env[1]);
		SetENV((ENV)env[3]);
	}
	public void EXIT()
	{
		GLOBALS.GetPM().Deactivate();
	}
	public void QUIT()
	{
		Environment.Exit(0);
	}
	public void SetENV(ENV env)
	{
		var test = GLOBALS.GetPM();
		test.SetENV(env);
	}
	public ENV GetENV()
	{
		var test = GLOBALS.GetPM();
		return test.GetENV();
	}
	public ENV GetDefualtENV()
	{
		var test = GLOBALS.GetPM();
		return test.GetDENV();
	}
	public ENV CreateENV()
	{
		var temp = new ENV();
		var PM = GLOBALS.GetPM();
		temp.SetParent(PM.GetENV());
		return temp;
	}
	public int GOTO(string label)
	{
		var test = GLOBALS.GetPM();
		var c = test.GetCurrentChunk();
		int pos;
		if (c.TryGetLabel(label, out pos)) {
			c.SetPos(pos);
			return 0;
		} else if (test.GetFlag("forseelabels")) {
			var chunks = test.GetChunks();
			for (int i = 0; i < chunks.Length; i++) {
				if (chunks[i].TryGetLabel(label, out pos)) {
					test.SetBlock(chunks[i].GetName());
					chunks[i].SetPos(pos);
					return 0;
				}
			}
		}
		test.PushError("Unable to GOTO a non existing label: " + label + "!");
		return 0;
	}
	public void JUMP(string block)
	{
		var test = GLOBALS.GetPM();
		var c = test.GetCurrentChunk();
		c.ResetPos();
		test.SetBlock(block);
	}
	public void SKIP(double n)
	{
		var test = GLOBALS.GetPM();
		var c = test.GetCurrentChunk();
		var pos = c.GetPos();
		c.SetPos(pos + (int)n);
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
	public double CALC(string ex)
	{
		return evaluater.Evaluate(ex);
	}
}