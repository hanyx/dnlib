﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using dot10.IO;
using dot10.PE;
using dot10.DotNet.MD;

namespace dot10.DotNet.Writer {
	/// <summary>
	/// <see cref="MetaDataHeader"/> options
	/// </summary>
	public class MetaDataHeaderOptions {
		/// <summary>
		/// MS CLR 1.0 version string (.NET 1.0)
		/// </summary>
		public const string MS_CLR_10 = "v1.0.3705";

		/// <summary>
		/// MS CLR 1.1 version string (.NET 1.1)
		/// </summary>
		public const string MS_CLR_11 = "v1.1.4322";

		/// <summary>
		/// MS CLR 2.0 version string (.NET 2.0-3.5)
		/// </summary>
		public const string MS_CLR_20 = "v2.0.50727";

		/// <summary>
		/// MS CLR 4.0 version string (.NET 4.0-4.5)
		/// </summary>
		public const string MS_CLR_40 = "v4.0.30319";

		/// <summary>
		/// ECMA 2002 version string
		/// </summary>
		public const string ECMA_2002 = "Standard CLI 2002";

		/// <summary>
		/// ECMA 2005 version string
		/// </summary>
		public const string ECMA_2005 = "Standard CLI 2005";

		/// <summary>
		/// Default version string
		/// </summary>
		public const string DEFAULT_VERSION_STRING = MS_CLR_20;

		/// <summary>
		/// Default header signature
		/// </summary>
		public const uint DEFAULT_SIGNATURE = 0x424A5342;

		/// <summary>
		/// MD header signature. Default value is <see cref="DEFAULT_SIGNATURE"/>
		/// </summary>
		public uint? Signature;

		/// <summary>
		/// Major version. Default is 1. MS' CLR supports v0.x (x >= 19) and v1.1, nothing else.
		/// </summary>
		public ushort? MajorVersion;

		/// <summary>
		/// Minor version. Default is 1.
		/// </summary>
		public ushort? MinorVersion;

		/// <summary>
		/// Reserved and should be 0.
		/// </summary>
		public uint? Reserved1;

		/// <summary>
		/// Version string. Default is <see cref="DEFAULT_VERSION_STRING"/>. It's stored as a
		/// zero-terminated UTF-8 string. Length should be &lt;= 255 bytes.
		/// </summary>
		public string VersionString;

		/// <summary>
		/// Storage flags should be 0
		/// </summary>
		public StorageFlags? StorageFlags;

		/// <summary>
		/// Reserved and should be 0
		/// </summary>
		public byte? Reserved2;
	}

	/// <summary>
	/// Meta data header. IMAGE_COR20_HEADER.MetaData points to this header.
	/// </summary>
	public sealed class MetaDataHeader : IChunk {
		IList<IHeap> heaps;
		MetaDataHeaderOptions options;
		uint length;
		FileOffset offset;
		RVA rva;

		/// <inheritdoc/>
		public FileOffset FileOffset {
			get { return offset; }
		}

		/// <inheritdoc/>
		public RVA RVA {
			get { return rva; }
		}

		/// <summary>
		/// Gets/sets the heaps
		/// </summary>
		public IList<IHeap> Heaps {
			get { return heaps; }
			set { heaps = value; }
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		public MetaDataHeader()
			: this(null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		public MetaDataHeader(MetaDataHeaderOptions options) {
			this.options = options ?? new MetaDataHeaderOptions();
		}

		/// <inheritdoc/>
		public void SetOffset(FileOffset offset, RVA rva) {
			this.offset = offset;
			this.rva = rva;

			length = 16;
			length += (uint)GetVersionString().Length;
			length = Utils.AlignUp(length, 4);
			length += 4;
			foreach (var heap in heaps) {
				length += 8;
				length += (uint)GetAsciizName(heap.Name).Length;
				length = Utils.AlignUp(length, 4);
			}
		}

		/// <inheritdoc/>
		public uint GetLength() {
			return length;
		}

		/// <inheritdoc/>
		public void WriteTo(BinaryWriter writer) {
			writer.Write(options.Signature ?? MetaDataHeaderOptions.DEFAULT_SIGNATURE);
			writer.Write(options.MajorVersion ?? 1);
			writer.Write(options.MinorVersion ?? 1);
			writer.Write(options.Reserved1 ?? 0);
			var s = GetVersionString();
			writer.Write(s.Length);
			writer.Write(s);
			writer.WriteZeros(Utils.AlignUp(s.Length, 4) - s.Length);
			writer.Write((byte)(options.StorageFlags ?? 0));
			writer.Write(options.Reserved2 ?? 0);
			writer.Write((ushort)heaps.Count);
			foreach (var heap in heaps) {
				writer.Write(0);	// Offset
				writer.Write(0);	// Size
				writer.Write(s = GetAsciizName(heap.Name));
				if (s.Length > 32)
					throw new ModuleWriterException(string.Format("Heap name '{0}' is > 32 bytes", heap.Name));
				writer.WriteZeros(Utils.AlignUp(s.Length, 4) - s.Length);
			}
		}

		byte[] GetVersionString() {
			return Encoding.UTF8.GetBytes((options.VersionString ?? MetaDataHeaderOptions.DEFAULT_VERSION_STRING) + "\0");
		}

		byte[] GetAsciizName(string s) {
			return Encoding.ASCII.GetBytes(s + "\0");
		}
	}
}