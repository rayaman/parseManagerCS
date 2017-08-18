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
		Dictionary<string, bool> _flags = new Dictionary<string, bool>();
		Dictionary<string, chunk> _chunks = new Dictionary<string, chunk>();
		Dictionary<string, string> _methods = new Dictionary<string, string>();
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
				} else {
					_chunks[Blck] = new chunk(Blck, Cont);
				}
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
			_filepath = filepath;
			_hasDefine = false;
			Parse();
		}
		public parseManager(string filepath, string define)
		{
			_define = define;
			_hasDefine = true;
			_filepath = filepath;
			_defineType = Type.GetType(define);
			ConstructorInfo defineConstructor = _defineType.GetConstructor(Type.EmptyTypes);
			_defineClassObject = defineConstructor.Invoke(new object[]{ });
			Parse();
		}
		public int InvokeR(string method, object[] args)
		{ // TODO collect the returned arguments if any
			if (!_hasDefine)
				return -1;
			_defineMethod = _defineType.GetMethod(method);
			object rets = _defineMethod.Invoke(_defineClassObject, args);
			Console.WriteLine(rets);
			return 0;
		}
		public long InvokeNR(string method, object[] args)
		{ // Simple Invoking!
			if (!_hasDefine)
				return -1;
			_defineMethod = _defineType.GetMethod(method);
			_defineMethod.Invoke(_defineClassObject, args);
			return 0;
		}
		public void SetBlock(string BLOCK){
			chunk cchunk;
			if(_chunks.TryGetValue(BLOCK, out cchunk)){
				_currentChunk=cchunk;
			} else {
				PushError("Attempt to JUMP to a non existing block!");
			}
		}
		public void SetBlock(){
			chunk cchunk;
			if(_chunks.TryGetValue(_entry, out cchunk)){
				_currentChunk=cchunk;
			} else {
				PushError("Entrypoint is Invalid!");
			}
		}
		public void PushError(string err){
			Console.WriteLine(err);
		}
		public nextType Next(string BLOCK)
		{
			if(_currentChunk==null){
				SetBlock(BLOCK);
			}
			return Next();
		}
		public nextType Next()
		{
			nextType tempReturn = new nextType("method");
			if(_currentChunk==null){
				SetBlock();
			}
			// TODO Add commands lol 
			var FuncWReturn = Regex.Match(_currentChunk.GetLine(), "([\\[\\]\"a-zA-Z0-9_,]+)\\s?=\\s?([a-zA-Z0-9_]+)\\s?\\((.+)\\)");
			var FuncWOReturn = Regex.Match(_currentChunk.GetLine(), @"^([a-zA-Z0-9_]+)\s?\((.+)\)");
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
		public nextType(string type)
		{
			_type = type;
		}
		public string GetNextType()
		{
			return _type;
		}
		public void SetText(string text)
		{
			_text = text;
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
	public class chunk
	{
		string _BLOCK;
		string _type;
		string _pureType;
		string[] lines;
		int _pos = 0;
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
		public string[] GetLines()
		{
			return lines;
		}
		public string GetLine()
		{
			return lines[_pos++];
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
}
/*
 * The Standard Methods!
 */
public class standardParseDefine
{
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