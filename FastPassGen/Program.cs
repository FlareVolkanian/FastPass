using FastPass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastPassGen
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputName = null;
            string outputName = null;
            string nameSpace = null;
            string returnType = "object";
            string prefix = null;
            string rootRules = null;
            bool Public = false;
            bool addToken = false;
            bool includeInterface = false;
            string baseClass = null;
            bool strict = false;
            bool useMultiTokens = false;

            if(args.Length == 0)
            {
                PrintHelp();
                Environment.Exit(1);
            }

            for(int i = 0;i < args.Length;++i)
            {
                string arg = args[i];
                string nextArg = "";
                if(args.Length > i + 1)
                {
                    nextArg = args[i + 1];
                }
                if(arg == "-f" && !nextArg.StartsWith("-") && nextArg != "")
                {
                    inputName = nextArg;
                }
                else if(arg == "-o" && !nextArg.StartsWith("-") && nextArg != "")
                {
                    outputName = nextArg;
                }
                else if(arg == "-n" && !nextArg.StartsWith("-") && nextArg != "")
                {
                    nameSpace = nextArg;
                }
                else if(arg == "-t" && !nextArg.StartsWith("-") && nextArg != "")
                {
                    returnType = nextArg;
                }
                else if(arg == "-p" && !nextArg.StartsWith("-") && nextArg != "")
                {
                    prefix = nextArg;
                }
                else if(arg == "-r" && !nextArg.StartsWith("-") && nextArg != "")
                {
                    rootRules = nextArg;
                }
                else if(arg == "-P")
                {
                    Public = true;
                }
                else if(arg == "-at")
                {
                    addToken = true;
                }
                else if(arg == "-ai")
                {
                    includeInterface = true;
                }
                else if (arg == "-b" && !nextArg.StartsWith("-") && nextArg != "")
                {
                    baseClass = nextArg;
                }
                else if(arg == "-s")
                {
                    strict = true;
                }
                else if(arg == "-mt")
                {
                    useMultiTokens = true;
                }
            }

            if(inputName == null)
            {
                PrintHelp();
                Environment.Exit(1);
            }
            if(outputName == null)
            {
                outputName = Path.GetFileNameWithoutExtension(inputName) + ".cs";
            }
            if(nameSpace == null)
            {
                nameSpace = Path.GetFileNameWithoutExtension(inputName) + "ns";
            }
            if(rootRules == null)
            {
                PrintHelp();
                Environment.Exit(1);
            }

            List<string> rootRuleList = rootRules.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            string input = null;
            try
            {
                input = File.ReadAllText(inputName);
            }
            catch(Exception)
            {
                Console.WriteLine("Unable to load input file");
                Environment.Exit(1);
            }

            FastPassCompiler fpc = new FastPassCompiler();
            string output = null;
            try
            {
                output = fpc.GenerateParser(input, nameSpace, rootRuleList, returnType, prefix, Public, addToken, includeInterface, baseClass, strict, useMultiTokens);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }

            try
            {
                File.WriteAllText(outputName, output);
            }
            catch(Exception)
            {
                Console.WriteLine("Unable to write to output file");
                Environment.Exit(1);
            }

        }

        private static void PrintHelp()
        {
            Console.WriteLine("usage: FastPassGen.exe -f InputFile [-o OutputFile] [-n NameSpace] [-t ReturnType] [-p Prefix] [-b baseclass] -r rootrules [-P -at -ai -s]");
        }
    }
}
