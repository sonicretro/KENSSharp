using System;

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
            if (samefilename)
                switch (mode)
                {
                    case Mode.Compress:
                        switch (type)
                        {
                            case CompressionType.Kosinski:
                                output = System.IO.Path.ChangeExtension(input, "kos");
                                break;
                            case CompressionType.Enigma:
                                output = System.IO.Path.ChangeExtension(input, "eni");
                                break;
                            case CompressionType.Nemesis:
                                output = System.IO.Path.ChangeExtension(input, "nem");
                                break;
                            case CompressionType.Saxman:
                                output = System.IO.Path.ChangeExtension(input, "sax");
                                break;
                            case CompressionType.KosinskiModuled:
                                output = System.IO.Path.ChangeExtension(input, "kosm");
                                break;
                        }
                        break;
                    case Mode.Decompress:
                        output = System.IO.Path.ChangeExtension(input, "unc");
                        break;
                }
            switch (mode)
            {
                case Mode.Compress:
                    switch (type)
                    {
                        case CompressionType.Kosinski:
                            Kosinski.Compress(input, output);
                            break;
                        case CompressionType.Enigma:
                            Enigma.Compress(input, output, endian);
                            break;
                        case CompressionType.Nemesis:
                            Nemesis.Compress(input, output);
                            break;
                        case CompressionType.Saxman:
                            Saxman.Compress(input, output, size);
                            break;
                        case CompressionType.ModuledKosinski:
                            ModuledKosinski.Compress(input, output, endian);
                            break;
                    }
                    break;
                case Mode.Decompress:
                    switch (type)
                    {
                        case CompressionType.Kosinski:
                            Kosinski.Decompress(input, output);
                            break;
                        case CompressionType.Enigma:
                            Enigma.Decompress(input, output, endian);
                            break;
                        case CompressionType.Nemesis:
                            Nemesis.Decompress(input, output);
                            break;
                        case CompressionType.Saxman:
                            Saxman.Decompress(input, output);
                            break;
                        case CompressionType.ModuledKosinski:
                            ModuledKosinski.Decompress(input, output, endian);
                            break;
                    }
                    break;
                case Mode.Recompress:
                    switch (type)
                    {
                        case CompressionType.Kosinski:
                            Kosinski.Compress(Kosinski.Decompress(input), output);
                            break;
                        case CompressionType.Enigma:
                            Enigma.Compress(Enigma.Decompress(input, endian), output, endian);
                            break;
                        case CompressionType.Nemesis:
                            Nemesis.Compress(Nemesis.Decompress(input), output);
                            break;
                        case CompressionType.Saxman:
                            Saxman.Compress(Saxman.Decompress(input), output, size);
                            break;
                        case CompressionType.ModuledKosinski:
                            ModuledKosinski.Compress(ModuledKosinski.Decompress(input, endian), output, endian);
                            break;
                    }
                    break;
            }
        }

        static void ShowHelp()
        {
            Console.Write(Properties.Resources.HelpText);
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
        mk = KosinskiModuled
    }
}