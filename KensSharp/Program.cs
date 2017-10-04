using System;
using System.IO;

namespace SonicRetro.KensSharp.KensSharp
{
    static class Program
    {
        static void Main(string[] args)
        {
            LongOpt[] opts = new[] {
                new LongOpt("help", Argument.No, null, 'h'),
                new LongOpt("compress", Argument.Required, null, 'c'),
                new LongOpt("decompress", Argument.Required, null, 'd'),
                new LongOpt("recompress", Argument.Required, null, 'r'),
                new LongOpt("same-filename", Argument.No, null, 's'),
                new LongOpt("little-endian", Argument.No, null, 'l'),
                new LongOpt("no-size", Argument.No, null, 'n')
            };
            Getopt getopt = new Getopt("KensSharp", args, Getopt.digest(opts), opts);
            Mode? mode = null;
            CompressionType? type = null;
            Endianness endian = Endianness.BigEndian;
            bool size = true;
            bool samefilename = false;
            int opt = getopt.getopt();
            while (opt != -1)
            {
                switch (opt)
                {
                    case 'h':
                        ShowHelp();
                        return;
                    case 'c':
                        mode = Mode.Compress;
                        type = (CompressionType)Enum.Parse(typeof(CompressionType), getopt.Optarg, true);
                        break;
                    case 'd':
                        mode = Mode.Decompress;
                        type = (CompressionType)Enum.Parse(typeof(CompressionType), getopt.Optarg, true);
                        break;
                    case 'r':
                        mode = Mode.Recompress;
                        type = (CompressionType)Enum.Parse(typeof(CompressionType), getopt.Optarg, true);
                        break;
                    case 's':
                        samefilename = true;
                        break;
                    case 'l':
                        endian = Endianness.LittleEndian;
                        break;
                    case 'n':
                        size = false;
                        break;
                }
                opt = getopt.getopt();
            }
            if (mode == null || type == null || getopt.Optind + (mode == Mode.Recompress | samefilename ? 0 : 1) >= args.Length)
            {
                ShowHelp();
                return;
            }
            string input = args[getopt.Optind];
            string output;
            if (getopt.Optind + 1 == args.Length)
                output = input;
            else
                output = args[getopt.Optind + 1];
            if (samefilename && input != "-")
                switch (mode)
                {
                    case Mode.Compress:
                        switch (type)
                        {
                            case CompressionType.Kosinski:
                                output = Path.ChangeExtension(input, "kos");
                                break;
                            case CompressionType.Enigma:
                                output = Path.ChangeExtension(input, "eni");
                                break;
                            case CompressionType.Nemesis:
                                output = Path.ChangeExtension(input, "nem");
                                break;
                            case CompressionType.Saxman:
                                output = Path.ChangeExtension(input, "sax");
                                break;
                            case CompressionType.KosinskiModuled:
                                output = Path.ChangeExtension(input, "kosm");
                                break;
                            case CompressionType.Comper:
                                output = Path.ChangeExtension(input, "comp");
                                break;
                            case CompressionType.KosinskiPlus:
                                output = Path.ChangeExtension(input, "kosp");
                                break;
                            case CompressionType.KosinskiPlusModuled:
                                output = Path.ChangeExtension(input, "kospm");
                                break;
                        }
                        break;
                    case Mode.Decompress:
                        output = Path.ChangeExtension(input, "unc");
                        break;
                }
			byte[] indata = ReadInput(input);
			byte[] outdata = null;
            switch (mode)
            {
                case Mode.Compress:
                    switch (type)
                    {
                        case CompressionType.Kosinski:
                            outdata = Kosinski.Compress(indata);
                            break;
                        case CompressionType.Enigma:
							outdata = Enigma.Compress(indata, endian);
                            break;
                        case CompressionType.Nemesis:
							outdata = Nemesis.Compress(indata);
                            break;
                        case CompressionType.Saxman:
							outdata = Saxman.Compress(indata, size);
                            break;
                        case CompressionType.ModuledKosinski:
                            outdata = ModuledKosinski.Compress(indata, endian);
                            break;
                        case CompressionType.Comper:
                            outdata = Comper.Compress(indata);
                            break;
                        case CompressionType.KosinskiPlus:
                            outdata = KosinskiPlus.Compress(indata);
                            break;
                        case CompressionType.ModuledKosinskiPlus:
                            outdata = ModuledKosinskiPlus.Compress(indata, endian);
                            break;
                    }
                    break;
                case Mode.Decompress:
                    switch (type)
                    {
                        case CompressionType.Kosinski:
							outdata = Kosinski.Decompress(indata);
                            break;
                        case CompressionType.Enigma:
							outdata = Enigma.Decompress(indata, endian);
                            break;
                        case CompressionType.Nemesis:
							outdata = Nemesis.Decompress(indata);
                            break;
                        case CompressionType.Saxman:
							outdata = Saxman.Decompress(indata);
                            break;
                        case CompressionType.ModuledKosinski:
                            outdata = ModuledKosinski.Decompress(indata, endian);
                            break;
                        case CompressionType.Comper:
                            outdata = Comper.Decompress(indata);
                            break;
                        case CompressionType.KosinskiPlus:
                            outdata = KosinskiPlus.Decompress(indata);
                            break;
                        case CompressionType.ModuledKosinskiPlus:
                            outdata = ModuledKosinskiPlus.Decompress(indata, endian);
                            break;
                    }
                    break;
                case Mode.Recompress:
                    switch (type)
                    {
                        case CompressionType.Kosinski:
							outdata = Kosinski.Compress(Kosinski.Decompress(indata));
                            break;
                        case CompressionType.Enigma:
							outdata = Enigma.Compress(Enigma.Decompress(indata, endian), endian);
                            break;
                        case CompressionType.Nemesis:
							outdata = Nemesis.Compress(Nemesis.Decompress(indata));
                            break;
                        case CompressionType.Saxman:
							outdata = Saxman.Compress(Saxman.Decompress(indata), size);
                            break;
                        case CompressionType.ModuledKosinski:
                            outdata = ModuledKosinski.Compress(ModuledKosinski.Decompress(indata, endian), endian);
                            break;
                        case CompressionType.Comper:
                            outdata = Comper.Compress(Comper.Decompress(indata));
                            break;
                        case CompressionType.KosinskiPlus:
                            outdata = KosinskiPlus.Compress(KosinskiPlus.Decompress(indata));
                            break;
                        case CompressionType.ModuledKosinskiPlus:
                            outdata = ModuledKosinskiPlus.Compress(ModuledKosinskiPlus.Decompress(indata, endian), endian);
                            break;
                    }
                    break;
            }
			WriteOutput(output, outdata);
        }

        static void ShowHelp()
        {
            Console.Write(Properties.Resources.HelpText);
        }

		static byte[] ReadInput(string filename)
		{
			if (filename == "-")
			{
				System.Collections.Generic.List<byte> result = new System.Collections.Generic.List<byte>();
				using (Stream stdin = Console.OpenStandardInput())
				using (BinaryReader read = new BinaryReader(stdin))
				{
					byte[] buf = read.ReadBytes(1024);
					do
					{
						result.AddRange(buf);
						buf = read.ReadBytes(1024);
					}
					while (buf.Length > 0);
				}
				return result.ToArray();
			}
			else
				return File.ReadAllBytes(filename);
		}

		static void WriteOutput(string filename, byte[] data)
		{
			if (filename == "-")
				using (Stream stdout = Console.OpenStandardOutput())
					stdout.Write(data, 0, data.Length);
			else
				File.WriteAllBytes(filename, data);
		}
    }

    enum Mode
    {
        Compress,
        Decompress,
        Recompress
    }

    enum CompressionType
    {
        Kosinski,
        kos = Kosinski,
        k = Kosinski,
        Enigma,
        eni = Enigma,
        e = Enigma,
        Nemesis,
        nem = Nemesis,
        n = Nemesis,
        Saxman,
        sax = Saxman,
        s = Saxman,
        KosinskiModuled,
        ModuledKosinski = KosinskiModuled,
        kosm = KosinskiModuled,
        mkos = KosinskiModuled,
        km = KosinskiModuled,
        mk = KosinskiModuled,
        Comper,
        comp = Comper,
        c = Comper,
        KosinskiPlus,
        kosp = KosinskiPlus,
        kp = KosinskiPlus,
        KosinskiPlusModuled,
        ModuledKosinskiPlus = KosinskiPlusModuled,
        kosmp = KosinskiPlusModuled,
        mkosp = KosinskiPlusModuled,
        kpm = KosinskiPlusModuled,
        mkp = KosinskiPlusModuled
    }
}