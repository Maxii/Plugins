using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Pathfinding {
	/**
	 * Helper for enabling or disabling compiler directives.
	 * Used only in the editor.
	 * \astarpro
	 */
	public static class OptimizationHandler {
		public class DefineDefinition {
			public string name;
			public string description;
			public bool enabled;
			public bool consistent;
		}

		static string GetAstarPath () {
			var paths = Directory.GetDirectories(Application.dataPath, "AstarPathfindingProject", SearchOption.AllDirectories);

			if (paths.Length > 0) {
				return paths[0];
			}

			Debug.LogError("Could not find AstarPathfindingProject root folder");
			return Application.dataPath + "/AstarPathfindingProject";
		}

		public static void EnableDefine (string name) {
			name = name.Trim();

			var buildTypes = System.Enum.GetValues(typeof(BuildTargetGroup)) as int[];

			for (int i = 0; i < buildTypes.Length; i++) {
				if (buildTypes[i] == (int)BuildTargetGroup.Unknown) continue;

				string defineString = PlayerSettings.GetScriptingDefineSymbolsForGroup((BuildTargetGroup)buildTypes[i]);
				if (defineString == null) continue;

				var defines = defineString.Split(';').Select(s => s.Trim()).ToList();

				// Already enabled
				if (defines.Contains(name)) {
					continue;
				}

				defineString = defineString+";"+name;
				PlayerSettings.SetScriptingDefineSymbolsForGroup((BuildTargetGroup)buildTypes[i], defineString);
			}
		}

		public static void DisableDefine (string name) {
			name = name.Trim();

			var buildTypes = System.Enum.GetValues(typeof(BuildTargetGroup)) as int[];

			for (int i = 0; i < buildTypes.Length; i++) {
				if (buildTypes[i] == (int)BuildTargetGroup.Unknown) continue;

				string defineString = PlayerSettings.GetScriptingDefineSymbolsForGroup((BuildTargetGroup)buildTypes[i]);

				if (defineString == null) continue;

				var defines = defineString.Split(';').Select(s => s.Trim()).ToList();

				if (defines.Remove(name)) {
					defineString = string.Join(";", defines.Distinct().ToArray());
					PlayerSettings.SetScriptingDefineSymbolsForGroup((BuildTargetGroup)buildTypes[i], defineString);
				}
			}
		}

		public static void IsDefineEnabled (string name, out bool enabled, out bool consistent) {
			name = name.Trim();

			var buildTypes = System.Enum.GetValues(typeof(BuildTargetGroup)) as int[];

			int foundEnabled = 0;
			int foundDisabled = 0;

			for (int i = 0; i < buildTypes.Length; i++) {
				if (buildTypes[i] == (int)BuildTargetGroup.Unknown) continue;

				string defineString = PlayerSettings.GetScriptingDefineSymbolsForGroup((BuildTargetGroup)buildTypes[i]);

				if (defineString == null) continue;

				var defines = defineString.Split(';').Select(s => s.Trim()).ToList();

				if (defines.Contains(name)) {
					foundEnabled++;
				} else {
					foundDisabled++;
				}
			}

			enabled = foundEnabled > foundDisabled;
			consistent = (foundEnabled > 0) != (foundDisabled > 0);
		}

		public static List<DefineDefinition> FindDefines () {
			var path = GetAstarPath()+"/defines.csv";

			if (File.Exists(path)) {
				// Read a file consisting of lines with the format
				// NAME;Description
				// Ignore empty lines and lines which do not contain exactly 1 ';'
				var definePairs = File.ReadAllLines(path)
								  .Select(line => line.Trim())
								  .Where(line => line.Length > 0)
								  .Select(line => line.Split(';'))
								  .Where(opts => opts.Length == 2);

				return definePairs.Select(opts => {
					var def = new DefineDefinition { name = opts[0].Trim(), description = opts[1].Trim() };
					IsDefineEnabled(def.name, out def.enabled, out def.consistent);
					return def;
				}).ToList();
			}

			Debug.LogError("Could not find file '"+path+"'");
			return new List<DefineDefinition>();
		}

		public static void ApplyDefines (List<DefineDefinition> defines) {
			foreach (var define in defines) {
				if (define.enabled) {
					EnableDefine(define.name);
				} else {
					DisableDefine(define.name);
				}
			}
		}
	}
}
