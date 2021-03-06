﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NDepend.Path
{
	partial class PathHelpers
	{
		private abstract class VariablePathBase : PathBase, IVariablePath
		{
			protected VariablePathBase(string pathString)
				: base(pathString)
			{
				Debug.Assert(pathString != null);
				Debug.Assert(pathString.Length > 0);
				Debug.Assert(pathString.IsNormalized());

				Debug.Assert(m_PathString != null);
				Debug.Assert(m_PathString.Length > 0);
				Debug.Assert(m_PathString.IsNormalized());

				// It is important to use m_PathString and not pathString in IsAVariablePath() !
				// Indeed, since InnerSpecialDir have been resolved, some variable might have disappeared
				// like if pathString was "$(v1)\$(v2)\.." and m_PathString became "$(v1)"
				IReadOnlyList<string> variables;
				string failureReasonUnused;
				var b = VariablePathHelpers.IsAVariablePath(m_PathString, out variables, out failureReasonUnused);
				Debug.Assert(b);
				Debug.Assert(variables != null);
				Debug.Assert(variables.Count > 0);
				Debug.Assert(variables.All(v => v != null));
				Debug.Assert(variables.All(v => v.Length > 0));
				AllVariables = variables;
			}

			public IReadOnlyList<string> AllVariables { get; }

			public override bool HasParentDirectory => VariablePathHelpers.HasParentDirectory(m_PathString);


			public override bool IsAbsolutePath => false;

			public override bool IsEnvVarPath => false;

			public override bool IsRelativePath => false;

			public override bool IsVariablePath => true;

			public override IDirectoryPath ParentDirectoryPath
			{
				get
				{
					var parentPath = VariablePathHelpers.GetParentDirectory(m_PathString);
					return parentPath.ToVariableDirectoryPath();
				}
			}

			public override PathMode PathMode => PathMode.Variable;


			//
			// Resolving op
			//
			public string PrefixVariable => AllVariables[0];


			//
			// ParentDirectoryPath 
			// Special impls in VariablePathHelpers !
			//
			IVariableDirectoryPath IVariablePath.ParentDirectoryPath
			{
				get
				{
					var parentPath = VariablePathHelpers.GetParentDirectory(m_PathString);
					return parentPath.ToVariableDirectoryPath();
				}
			}


			// This methods are implemented in VariableFilePath and VariableDirectoryPath.
			public abstract VariablePathResolvingStatus TryResolve(IEnumerable<KeyValuePair<string, string>> values, out IAbsolutePath resolvedPath);

			public abstract VariablePathResolvingStatus TryResolve(IEnumerable<KeyValuePair<string, string>> values, out IAbsolutePath resolvedPath,
				out IReadOnlyList<string> unresolvedVariables);

			public abstract bool TryResolve(IEnumerable<KeyValuePair<string, string>> values, out IAbsolutePath resolvedPath, out string failureMessage);


			protected string GetVariableResolvedButCannotConvertToAbsolutePathFailureReason(IEnumerable<KeyValuePair<string, string>> variablesValues,
				string fileOrDirectory)
			{
				Debug.Assert(variablesValues != null);
				Debug.Assert(fileOrDirectory != null);
				Debug.Assert(fileOrDirectory.Length > 0);

				// Need to obtain again pathStringResolved to include it into the failureReason!
				string pathStringResolved;
				IReadOnlyList<string> unresolvedVariablesUnused;
				var b = TryResolve(variablesValues, out pathStringResolved, out unresolvedVariablesUnused);
				Debug.Assert(b);
				return @"All variable(s) have been resolved, but the resulting string {" + pathStringResolved + "} cannot be converted to an absolute "
					   + fileOrDirectory + " path.";
			}

			protected bool TryResolve(IEnumerable<KeyValuePair<string, string>> values, out string pathStringResolved,
				out IReadOnlyList<string> unresolvedVariables)
			{
				Debug.Assert(values != null);

				var pathString = m_PathString;

				var unresolvedVariablesList = new List<string>();
				var nbVariablesToResolve = AllVariables.Count;
				Debug.Assert(nbVariablesToResolve > 0);
				for (var i = 0; i < nbVariablesToResolve; i++)
				{
					var variableNameToResolve = AllVariables[i];
					Debug.Assert(variableNameToResolve != null);
					Debug.Assert(variableNameToResolve.Length > 0);
					var resolved = false;
					foreach (var pair in values)
					{
						var pairVariableName = pair.Key;

						// Support these two cases!
						if (pairVariableName == null)
						{
							continue;
						}
						if (pairVariableName.Length == 0)
						{
							continue;
						} // 
						if (String.Compare(pairVariableName, variableNameToResolve, true) != 0)
						{
							continue;
						} // true for ignore case! variable names are case insensitive
						resolved = true;
						var variableValue = pair.Value;
						if (variableValue == null)
						{
							// Treat null variableValue as empty string.
							variableValue = "";
						}
						pathString = VariablePathHelpers.ReplaceVariableWithValue(pathString, variableNameToResolve, variableValue);
					}
					if (!resolved)
					{
						unresolvedVariablesList.Add(variableNameToResolve);
					}
				}

				if (unresolvedVariablesList.Count > 0)
				{
					unresolvedVariables = unresolvedVariablesList.ToReadOnlyWrappedList();
					pathStringResolved = null;
					return false;
				}

				unresolvedVariables = null;
				pathStringResolved = pathString;
				return true;
			}
		}
	}
}
