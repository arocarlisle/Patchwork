﻿using ParadoxNotion.Serialization.FullSerializer;
using static Patchwork;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using MessagePack;
using System.Collections.Generic;

public partial class ScriptEnv
{
	public static string subdir = "";
	public static void dumpassets()
	{
		print("Dumping all text serializable assets");
		var dumpdir = Dir.mod + "!base/";
		var mainass = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Assembly-CSharp");
		var dumpables = mainass.GetExportedTypes().Where(t => typeof(IDumpable).IsAssignableFrom(t));
		var basedir = Dir.abdata;
		print($"Found {dumpables.Count()} types.");
		LoadedAssetBundle.GCBundles();
		foreach (var bpath in Directory.GetFiles(Dir.abdata + subdir, "*.unity3d", SearchOption.AllDirectories))
		{
			var bundle = bpath.Replace("\\", "/");
			var abname = bundle.Substring(Dir.abdata.Length);
			if (abname.Contains("--"))
				continue;
			var lab = AssetBundle.LoadFromFile(bundle);
			if (lab == null)
				continue;
			foreach (var longname in lab.GetAllAssetNames())
			{
				var dir = bundle.Remove(bundle.LastIndexOf('.')).Substring(Dir.abdata.Length);
				byte[] bytes = null;
				var name = Path.GetFileName(longname).ToLower();
				var bname = Path.GetFileNameWithoutExtension(name);
				string ext = name.Substring(bname.Length).ToLower();
				if (ext != ".asset" && ext != ".txt" && ext != ".bytes" && ext != "")
				{
					//print("skip");
					continue;
				}
				var ass = lab.LoadAsset(bname);
				if (ass == null)
				{
					print("Failed to load ", ass);
					continue;
				}
				var ta = ass as UnityEngine.TextAsset;
				var da = ass as IDumpable;
				//print(ass.GetType());
				if (ta != null)
				{
					if (bname.StartsWith("cf_anmshape"))
					{
						print(name);
						var si = new AnimationKeyInfo();
						si.LoadInfo(new MemoryStream(ta.bytes));
						bytes = si.Marshal().ToBytes();
						ext = ".csv";
					} else
					if (abname.Contains("list/characustom/"))
					{
						var chd = MessagePackSerializer.Deserialize<ChaListData>(ta.bytes);
						bytes = chd.Marshal().ToBytes();
						bname = chd.categoryNo + "_" + bname;
						var disa = $"{chd.distributionNo:00}";
						var disb = Path.GetFileName(dir);
						dir = dir.Remove(dir.LastIndexOf('/')+1) + disa + ((disa!=disb)?("_" + disb):"");
						ext = ".csv";
					} else 
					if (name.ToLower().EndsWith(".txt"))
					{
						bytes = ta.text.StripBOM().ToBytes();
						ext = ".lst";
					} else
					{
						var test = ta.text.Replace("\r", "").Split('\n');
						if (test.Length > 2 && (test[0].Split('\t').Length == test[1].Split('\t').Length) && test[0].Split('\t').Length>=2)
						{
							bytes = ta.text.StripBOM().ToBytes();
							ext = ".lst";
						} else
						{
							if (abname.Contains("h/list") && bname.StartsWith("kh") && ext == ".bytes")
							{
								var ikd = new MotionIKData();
								ikd.Read(new MemoryStream(ta.bytes));
								bytes = ikd.Marshal().ToBytes();
								ext = "."+ikd.GetFileExt();
							} else
							{
								print("Uknown " + bname);
							}
						}
					}
				}
				else if (ass is IDumpable)
				{
					ext = "." + da.GetFileExt();
					try {
						bytes = da.Marshal().ToBytes();
					} catch (Exception ex)
					{
						print("Something went wrong");
						print(bpath);
						print(bname);
						print(ex);
					}
				}

				if (bytes != null)
				{				
					var ddir = dumpdir + dir;
					Directory.CreateDirectory(ddir);
					var dst = ddir + "/" + bname + ext;
					File.WriteAllBytes(dst, bytes);
					//print("Dumping " + dst.Substring(Dir.root.Length) + " of type " + ass.GetType().Name);
				}
			}
			lab.Unload(true);
		}
	}
}
