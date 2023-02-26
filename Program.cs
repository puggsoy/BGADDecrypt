using Ionic.Zlib;

const int BGAD_MAGIC = 0x44414742;

if (args.Length <= 0 || args.Contains("-h") || args.Contains("--help"))
{
	PrintHelp();
	return;
}

string inputPath = args[0];
string outputDirectory = Path.Combine(Path.GetDirectoryName(inputPath), Path.GetFileName(inputPath) + "_output");

if (args.Length > 1)
	outputDirectory = args[1];

FileStream fs = new FileStream(inputPath, FileMode.Open);
BinaryReader br = new BinaryReader(fs);

Console.WriteLine(string.Format("Reading {0}...", Path.GetFileName(inputPath)));

while (br.BaseStream.Position < br.BaseStream.Length)
{
	BGADHeader header = new BGADHeader();
	header.Magic = br.ReadInt32();

	if (header.Magic != BGAD_MAGIC)
	{
		Console.Error.WriteLine(string.Format("Invalid BGAD magic at location: 0x{0}", br.BaseStream.Position.ToString("X")));
		return;
	}

	header.KeyType = br.ReadInt16();
	header.Unk = br.ReadInt16();
	header.HeaderSize = br.ReadInt16();
	header.NameLength = br.ReadInt16();
	header.DataType = br.ReadInt16();
	header.IsCompressed = br.ReadInt16();
	header.DataSize = br.ReadInt32();
	header.DecompressedSize = br.ReadInt32();

	BGAD bgad = new BGAD();
	bgad.Header = header;
	bgad.Name = br.ReadBytes(header.NameLength);
	bgad.Data = br.ReadBytes(header.DataSize);

	byte[] decryptedName = khux_decrypt(bgad.Name, header.DataSize);
	string outName = System.Text.Encoding.UTF8.GetString(decryptedName);

	byte[] decryptedData = khux_decrypt(bgad.Data, header.NameLength);

	if (header.IsCompressed != 0)
		decryptedData = ZlibStream.UncompressBuffer(decryptedData);

	string outputPath = Path.Combine(outputDirectory, outName);
	Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
	FileStream outStream = File.Create(outputPath);
	outStream.Write(decryptedData);
	outStream.Close();

	Console.WriteLine(string.Format("Saved {0}", outputPath.Replace('\\', '/')));
}

fs.Close();

void PrintHelp()
{
	string message = string.Format("usage: {0} input_file [output_folder]" +
		"\n    input_file: The input file" +
		"\n    output_folder: The output folder. Will be created if it does not yet exist. If omitted, will created a subfolder called \"<input_file>_output\"",
		System.Diagnostics.Process.GetCurrentProcess().ProcessName);

	Console.Error.WriteLine(message);
}

int khux_random(int seed)
{
	return 0x19660D * seed + 0x3C6EF35F;
}

byte[] khux_decrypt(byte[] data, int key)
{
	byte[] decryptedData = new byte[data.Length];

	for (int i = 0; i < data.Length; i++)
	{
		key = khux_random(key);
		decryptedData[i] = (byte)(data[i] ^ key);
	}

	return decryptedData;
}

struct BGADHeader
{
	public int Magic;
	public short KeyType;
	public short Unk;
	public short HeaderSize;
	public short NameLength;
	public short DataType;
	public short IsCompressed;
	public int DataSize;
	public int DecompressedSize;
}

struct BGAD
{
	public BGADHeader Header;
	public byte[] Name;
	public byte[] Data;
}
