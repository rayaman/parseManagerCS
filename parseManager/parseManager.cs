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
		chunk _lastChunk=null;
		readonly ENV _mainENV = new ENV();
		ENV _defualtENV = _mainENV;
		Dictionary<string, bool> _flags = new Dictionary<string, bool>();
		Dictionary<string, chunk> _chunks = new Dictionary<string, chunk>();
		Dictionary<string, string> _methods = new Dictionary<string, string>();
		void InitFlags(){
			_flags.Add("leaking",false);
			_flags.Add("forseelabels",true);
			_flags.Add("debugging",false);
			_flags.Add("topdown",true);
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
					Blck=Blck.Substring(0, loc);
				} else {
					_chunks[Blck] = new chunk(Blck, Cont);
				}
				if(_lastChunk!=null){
					_lastChunk.SetNextChunk(_chunks[Blck]);
				}
				_lastChunk=_chunks[Blck];
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
		public ENV GetENV(){
			return _defualtENV;
		}
		public void SetENV(){
			_defualtENV=_mainENV;
		}
		public void SetENV(ENV o){
			_defualtENV=o;
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
		/*
		 * THE NEXT METHOD
		 */ 
		public nextType Next()
		{
			nextType tempReturn = new nextType();
			if (_currentChunk == null) {
				SetBlock();
			}
			// TODO Add commands lol
			string currentline = _currentChunk.GetLine();
			if (currentline == null) {
				if (_flags["leaking"]) {
					SetBlock(_currentChunk.GetNextChunk());
					return Next();
				} else {
					tempReturn.SetCMDType("EOF");
					tempReturn.SetText("Reached the end of the file!");
					return tempReturn;
				}
			}
			var FuncWReturn = Regex.Match(currentline, "([\\[\\]\"a-zA-Z0-9_,]+)\\s?=\\s?([a-zA-Z0-9_]+)\\s?\\((.+)\\)");
			var FuncWOReturn = Regex.Match(currentline, @"^([a-zA-Z0-9_]+)\s?\((.+)\)");
			// FuncWOReturn. // TODO Fix This stuff
			return tempReturn;
		}
	}
	/*
	 * Helper Classes
	 */
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
		string _line;
		parseManager _parse;
		public CMD(string line,parseManager parse){
			_line=line;
			_parse=parse;
		}
		public void Run(){
			// TODO Finish this
		}
	}
	public class chunk
	{
		string _BLOCK;
		string _type;
		string _pureType;
		string[] lines;
		int _pos = 0;
		chunk _next=null;
		void _clean(string cont)
		{
			var m = Regex.Match(_type, @"([a-zA-Z0-9_]+)");
			_pureType = m.Groups[1].ToString();
			string tCont = Regex.Replace(cont, @"\-\-\[\[[\S\s]+\]\]", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\-\-.+\r\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\-\-.+\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\t", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\n\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\r\n\r\n", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"\-\-\[\[[\S\s]+\]\]", "", RegexOptions.Multiline);
			tCont = Regex.Replace(tCont, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
			lines = tCont.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
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
		public void SetNextChunk(chunk next){
			_next=next;
		}
		public chunk GetNextChunk(){
			return _next;
		}
		public chunk SetNextChunk(){
			return _next;
		}
		public string[] GetLines()
		{
			return lines;
		}
		public string GetLine()
		{
			string temp = lines[_pos++];
			if (_pos == lines.Length) {
				return null;
			}
			return temp;
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
		public void SetParent(ENV other){
			_Parent=other;
		}
		object this[string ind]{
			get{
				object obj;
				if(_vars.TryGetValue(ind, out obj)){
					return obj;
				} else {
					if(_Parent!=null){
						return _Parent[ind];
					} else {
						return null;
					}
				}
			}
			set{
				_vars[ind] = value;
			}
		}
	}
}
/*
 * The Standard Methods!
 */
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
	public int[] TEST(){
		return new int[]{1,2,3};
	}
}