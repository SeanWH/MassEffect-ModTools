﻿using System;
using System.Collections.Generic;
using System.IO;
using Gammtek.Conduit.IO;

namespace MassEffect3.Coalesce.IO
{
	public class BinaryCoalesceFile : CoalesceFile
	{
		public BinaryCoalesceFile(string source = "", string name = "", string id = "", IList<CoalesceAsset> assets = null,
			CoalesceSettings settings = null, ByteOrder byteOrder = ByteOrder.LittleEndian)
			: base(source, name, id, assets, settings, byteOrder)
		{}

		public static BinaryCoalesceFile Load(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException("path");
			}

			var sourcePath = Path.GetFullPath(path);

			if (!File.Exists(sourcePath))
			{
				return null;
			}

			return new BinaryCoalesceFile();
		}

		public void Save(string path)
		{
			
		}
	}
}