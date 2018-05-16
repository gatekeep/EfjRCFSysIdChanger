using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;

namespace EfjRCFSysIdChange
{
    class Program
    {
        static void Usage(OptionSet p)
        {
            Console.WriteLine("usage: EfjRCFSysIdChange <extra arguments ...>");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
        }

        static int GetNumber(string s)
        {
            int n = 0x0;
            if (s.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) ||
                s.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase))
            {
                s = s.Substring(2);
            }

            if (!int.TryParse(s, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out n))
            {
                if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.CurrentCulture, out n))
                    Console.WriteLine("could not processed passed number");
            }

            return n;
        }

        static void Main(string[] args)
        {
            List<string> extraArgs = new List<string>();
            string rcfFile = string.Empty;
            int toWacn = 0xBEE00, toSysId = 0x001;
            int fromWacn = 0xBEE00, fromSysId = 0x001;
            bool showHelp = false, p25Trunking = false, smartZone = false;

            // command line parameters
            OptionSet options = new OptionSet()
            {
                { "to-wacn=", "WACN", v => toWacn = GetNumber(v) },
                { "to-sys=", "System ID", v => toSysId = GetNumber(v) },
                { "from-wacn=", "WACN", v => fromWacn = GetNumber(v) },
                { "from-sys=", "System ID", v => fromSysId = GetNumber(v) },
                { "f=", "EFJ RCF", v => rcfFile = v },

                { "p25t", "P25 Trunking System", v => p25Trunking = v != null },
                { "smartzone", "SmartZone Trunking System", v => smartZone = v != null },

                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            // attempt to parse the commandline
            try
            {
                extraArgs = options.Parse(args);
            }
            catch (OptionException)
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("error: invalid arguments");
                    Usage(options);
                    Environment.Exit(-1);
                }
            }

            Console.WriteLine(">>> " + AssemblyVersion._VERSION_STRING + " (Built " + AssemblyVersion._BUILD_DATE + ")");
            Console.WriteLine(">>> " + AssemblyVersion._COPYRIGHT + " All Rights Reserved.");
            Console.WriteLine("OSVersion: " + Environment.OSVersion.ToString());
            Console.WriteLine("OSPlatform: " + Environment.OSVersion.Platform.ToString());

            if (showHelp)
            {
                Usage(options);
                return;
            }

            if (rcfFile == string.Empty)
            {
                Console.WriteLine("error: invalid arguments");
                Usage(options);
                Environment.Exit(-1);
            }

            using (FileStream stream = File.Open(rcfFile, FileMode.Open))
            {
                BinaryReader reader = new BinaryReader(stream);
                byte[] rcfData = reader.ReadBytes((int)stream.Length);

                if (p25Trunking)
                {
                    for (int i = 0; i < rcfData.Length - 1; i++)
                    {
                        // this is a good indication of a P25 trunking record
                        if (rcfData[i] == 0x00 && rcfData[i + 1] == 0x00 &&
                            rcfData[i + 2] == ((byte)(fromWacn >> 16) & 0xFF) &&
                            rcfData[i + 3] == ((byte)(fromWacn >> 8) & 0xFF) &&
                            rcfData[i + 4] == ((byte)(fromWacn >> 0) & 0xFF) &&
                            rcfData[i + 5] == ((byte)(fromSysId >> 8) & 0xFF) &&
                            rcfData[i + 6] == ((byte)(fromSysId >> 0) & 0xFF))
                        {
                            int foundWacn = (rcfData[i + 2] << 16) |
                                            (rcfData[i + 3] << 8) |
                                            (rcfData[i + 4] << 0);
                            int foundSysId = (rcfData[i + 5] << 8) |
                                             (rcfData[i + 6] << 0);

                            Console.WriteLine(string.Format("Found WACN: {0} and System ID: {1}", foundWacn.ToString("X5"), foundSysId.ToString("X3")));
                            Console.WriteLine(string.Format("Replacing WACN: {0} and System ID: {1}", toWacn.ToString("X5"), toSysId.ToString("X3")));

                            // modify WACN
                            rcfData[i + 2] = (byte)((toWacn >> 16) & 0xFF);
                            rcfData[i + 3] = (byte)((toWacn >> 8) & 0xFF);
                            rcfData[i + 4] = (byte)((toWacn >> 0) & 0xFF);

                            // modify sysID
                            rcfData[i + 5] = (byte)((toSysId >> 8) & 0xFF);
                            rcfData[i + 6] = (byte)((toSysId >> 0) & 0xFF);

                            break;
                        }
                    }

                    stream.Position = 0;
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(rcfData, 0, rcfData.Length);
                }

                if (smartZone)
                {
                    for (int i = 0; i < rcfData.Length - 1; i++)
                    {
                        // this is a good indication of a P25 trunking record
                        if (rcfData[i] == 0x00 && rcfData[i + 1] == 0x80 &&
                            rcfData[i + 2] == ((byte)(fromSysId >> 8) & 0xFF) &&
                            rcfData[i + 3] == ((byte)(fromSysId >> 0) & 0xFF))
                        {
                            int foundSysId = (rcfData[i + 2] << 8) |
                                             (rcfData[i + 3] << 0);

                            Console.WriteLine(string.Format("Found System ID: {0}", foundSysId.ToString("X3")));
                            Console.WriteLine(string.Format("Replacing System ID: {0}", toSysId.ToString("X3")));

                            // modify sysID
                            rcfData[i + 2] = (byte)((toSysId >> 8) & 0xFF);
                            rcfData[i + 3] = (byte)((toSysId >> 0) & 0xFF);

                            break;
                        }
                    }

                    stream.Position = 0;
                    BinaryWriter writer = new BinaryWriter(stream);
                    writer.Write(rcfData, 0, rcfData.Length);
                }
            }
        }
    }
}
