/*
 * Created by SharpDevelop.
 * User: Ryan
 * Date: 8/17/2017
 * Time: 11:53 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Threading;
using CSCore;
using CSCore.Codecs;
using CSCore.SoundOut;
using parseManagerCS;
public class define : standardDefine // If you want the standard methods you must include this, Also this class cannot be static!
{
	double count;
	ISoundOut GetSoundOut()
	{
		if (WasapiOut.IsSupportedOnCurrentPlatform)
			return new WasapiOut();
		else
			return new DirectSoundOut();
	}
	IWaveSource GetSoundSource(string path)
	{
		return CodecFactory.Instance.GetCodec(path);
	}
	public void _play()
	{
		string path = (string)GLOBALS.GetData("__MUSIC");
		double id = (double)GLOBALS.GetData("__MUSICH");
		using (IWaveSource soundSource = GetSoundSource(path)) {
			using (ISoundOut soundOut = GetSoundOut()) {
				soundOut.Initialize(soundSource);
				GLOBALS.AddData("__MUSICH" + id, soundOut);
				soundOut.Play();
				soundOut.WaitForStopped();
			}
		}
	}
	public void STOP(parseManager PM, double id)
	{
		var sound = (ISoundOut)GLOBALS.GetData("__MUSICH" + id);
		sound.Stop();
	}
	public void RESUME(parseManager PM, double id)
	{
		var sound = (ISoundOut)GLOBALS.GetData("__MUSICH" + id);
		sound.Resume();
	}
	public void SETV(parseManager PM, double id, double vol)
	{
		var sound = (ISoundOut)GLOBALS.GetData("__MUSICH" + id);
		sound.Volume = (float)vol;
	}
	public void PAUSE(parseManager PM, double id)
	{
		var sound = (ISoundOut)GLOBALS.GetData("__MUSICH" + id);
		sound.Pause();
	}
	public double PLAY(parseManager PM, string filepath)
	{
		GLOBALS.AddData("__MUSIC", filepath);
		GLOBALS.AddData("__MUSICH", count++);
		var oThread = new Thread(new ThreadStart(_play));
		oThread.Start();
		return count - 1;
	}
}
namespace parseManagerCS
{
	class Program
	{
		public static void Main(string[] args)
		{
			if (args.Length == 0) {
				Console.Write("Please Include a file path!");
				Console.ReadLine();
				Environment.Exit(0);
			}
			parseManager PM = new parseManager(args[0], "define");
			nextType next = PM.Next();
			string type;
			while (next.GetCMDType() != "EOF") {
				type = next.GetCMDType();
				if (type == "line") {
					Console.Write(next.GetText());
					Console.ReadLine();
				}
				next = PM.Next();
			}
		}
	}
}