﻿#region ================== Copyright (c) 2020 Boris Iwanski

/*
 * This program is free software: you can redistribute it and/or modify
 *
 * it under the terms of the GNU General Public License as published by
 * 
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 * 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.If not, see<http://www.gnu.org/licenses/>.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Plugins;
using CodeImp.DoomBuilder.Types;
using Jint;

namespace CodeImp.DoomBuilder.UDBScript
{
	public class BuilderPlug : Plug
	{
		#region ================== Constants

		static private string SCRIPT_FOLDER = "udbscript";

		#endregion

		#region ================== Variables

		private static BuilderPlug me;
		private ScriptDockerControl panel;
		private Docker docker;
		private string currentscriptfile;
		private ScriptRunner scriptrunner;

		#endregion

		#region ================== Properties

		public static BuilderPlug Me { get { return me; } }
		public string CurrentScriptFile { get { return currentscriptfile; } set { currentscriptfile = value; } }
		internal ScriptRunner ScriptRunner { get { return scriptrunner; } }

		#endregion

		public override void OnInitialize()
		{
			base.OnInitialize();

			me = this;

			panel = new ScriptDockerControl(SCRIPT_FOLDER);
			docker = new Docker("udbscript", "Scripts", panel);
			General.Interface.AddDocker(docker);

			General.Actions.BindMethods(this);
		}

		// This is called when the plugin is terminated
		public override void Dispose()
		{
			base.Dispose();

			// This must be called to remove bound methods for actions.
			General.Actions.UnbindMethods(this);
		}

		public string GetScriptPathHash()
		{
			SHA256 hash = SHA256.Create();
			byte[] data = hash.ComputeHash(Encoding.UTF8.GetBytes(currentscriptfile));

			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < data.Length; i++)
			{
				sb.Append(data[i].ToString("x2"));
			}

			return sb.ToString();
		}

		public ExpandoObject GetScriptOptions()
		{
			return panel.GetScriptOptions();
		}

		/// <summary>
		/// Gets the name of the script file. This is either read from the .cfg file of the script or taken from the file name
		/// </summary>
		/// <param name="filename">Full path with file name of the script</param>
		/// <returns></returns>
		public static string GetScriptName(string filename)
		{
			string configfile = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename)) + ".cfg";

			if (File.Exists(configfile))
			{
				Configuration cfg = new Configuration(configfile, true);
				string name = cfg.ReadSetting("name", string.Empty);

				if (!string.IsNullOrEmpty(name))
					return name;
			}

			return Path.GetFileNameWithoutExtension(filename);
		}

		public void EndOptionEdit()
		{
			panel.EndEdit();
		}

		internal object GetVectorFromObject(object data, bool allow3d)
		{
			if (data is Vector2D)
				return (Vector2D)data;
			else if (data.GetType().IsArray)
			{
				object[] vals = (object[])data;

				// Make sure all values in the array are doubles
				foreach (object v in vals)
					if (!(v is double))
						throw new CantConvertToVectorException("Values in array must be numbers.");

				if (vals.Length == 2)
					return new Vector2D((double)vals[0], (double)vals[1]);
				if (vals.Length == 3)
					return new Vector3D((double)vals[0], (double)vals[1], (double)vals[2]);
			}
			else if (data is ExpandoObject)
			{
				IDictionary<string, object> eo = data as IDictionary<string, object>;
				double x = double.NaN;
				double y = double.NaN;
				double z = double.NaN;

				if (eo.ContainsKey("x"))
				{
					try
					{
						x = Convert.ToDouble(eo["x"]);
					}
					catch (Exception e)
					{
						throw new CantConvertToVectorException("Can not convert 'x' property of data: " + e.Message);
					}
				}

				if (eo.ContainsKey("y"))
				{
					try
					{
						y = Convert.ToDouble(eo["y"]);
					}
					catch (Exception e)
					{
						throw new CantConvertToVectorException("Can not convert 'y' property of data: " + e.Message);
					}
				}

				if (eo.ContainsKey("z"))
				{
					try
					{
						z = Convert.ToDouble(eo["z"]);
					}
					catch (Exception e)
					{
						throw new CantConvertToVectorException("Can not convert 'z' property of data: " + e.Message);
					}
				}

				if (allow3d)
				{
					if (x != double.NaN && y != double.NaN && z == double.NaN)
						return new Vector2D(x, y);
					else if (x != double.NaN && y != double.NaN && z != double.NaN)
						return new Vector3D(x, y, z);
				}
				else
				{
					if (x != double.NaN && y != double.NaN)
						return new Vector2D(x, y);
				}
			}

			if (allow3d)
				throw new CantConvertToVectorException("Data must be a Vector2D, Vector3D, or an array of numbers.");
			else
				throw new CantConvertToVectorException("Data must be a Vector2D, or an array of numbers.");
		}

		internal object GetConvertedUniValue(UniValue uv)
		{
			switch ((UniversalType)uv.Type)
			{
				case UniversalType.AngleRadians:
				case UniversalType.AngleDegreesFloat:
				case UniversalType.Float:
					return Convert.ToDouble(uv.Value);
				case UniversalType.AngleDegrees:
				case UniversalType.AngleByte: //mxd
				case UniversalType.Color:
				case UniversalType.EnumBits:
				case UniversalType.EnumOption:
				case UniversalType.Integer:
				case UniversalType.LinedefTag:
				case UniversalType.LinedefType:
				case UniversalType.SectorEffect:
				case UniversalType.SectorTag:
				case UniversalType.ThingTag:
				case UniversalType.ThingType:
					return Convert.ToInt32(uv.Value);
				case UniversalType.Boolean:
					return Convert.ToBoolean(uv.Value);
				case UniversalType.Flat:
				case UniversalType.String:
				case UniversalType.Texture:
				case UniversalType.EnumStrings:
				case UniversalType.ThingClass:
					return Convert.ToString(uv.Value);
			}

			return null;
		}

		internal Type GetTypeFromUniversalType(int type)
		{
			switch ((UniversalType)type)
			{
				case UniversalType.AngleRadians:
				case UniversalType.AngleDegreesFloat:
				case UniversalType.Float:
					return typeof(double);
				case UniversalType.AngleDegrees:
				case UniversalType.AngleByte: //mxd
				case UniversalType.Color:
				case UniversalType.EnumBits:
				case UniversalType.EnumOption:
				case UniversalType.Integer:
				case UniversalType.LinedefTag:
				case UniversalType.LinedefType:
				case UniversalType.SectorEffect:
				case UniversalType.SectorTag:
				case UniversalType.ThingTag:
				case UniversalType.ThingType:
					return typeof(int);
				case UniversalType.Boolean:
					return typeof(bool);
				case UniversalType.Flat:
				case UniversalType.String:
				case UniversalType.Texture:
				case UniversalType.EnumStrings:
				case UniversalType.ThingClass:
					return typeof(string);
			}

			return null;
		}

		#region ================== Actions

		[BeginAction("udbscriptexecute")]
		public void ScriptExecute()
		{
			if (string.IsNullOrEmpty(currentscriptfile))
				return;

			scriptrunner = new ScriptRunner(currentscriptfile);
			scriptrunner.Run();
		}

		#endregion
	}
}
