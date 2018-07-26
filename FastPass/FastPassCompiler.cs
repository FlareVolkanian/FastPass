using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastPass
{
    public class FastPassCompiler
    {
        private Dictionary<string, List<Rule>> RuleMap;
        private Dictionary<string, RuleReference> RuleReferenceCheck;
        private List<RuleFunction> Functions;
        private bool HasLookAhead;
        private List<string> Includes;

        public FastPassCompiler()
        {
            
        }

        public string GenerateParser(string Text, string NameSpace, List<string> RootRules, string ReturnType, string Prefix=null, bool Public=false, bool AddToken=false, bool IncludeInterface=false, string BaseClass=null, bool Strict=false, bool UseMultiTokens=false)
        {
            RuleMap = new Dictionary<string, List<Rule>>();
            RuleReferenceCheck = new Dictionary<string, RuleReference>();
            Functions = new List<RuleFunction>();
            Includes = new List<string>();
            HasLookAhead = false;
            LoadRules(Text);
            GenerateFunctions(ReturnType);
            string content = "\r\n" + GenerateReturnFunction(ReturnType, RootRules);
            foreach(RuleFunction func in Functions)
            {
                if (!Strict || RuleReferenceCheck[func.RuleName].Referenced)
                {
                    content += func.FunctionText;
                    content += "\r\n";
                }
            }

            if (Strict)
            {
                foreach (string ruleName in RuleReferenceCheck.Keys)
                {
                    if (!RuleReferenceCheck[ruleName].Referenced)
                    {
                        Console.WriteLine("Warning: rule \"" + ruleName + "\" is defined but never referenced, line: " + RuleReferenceCheck[ruleName].LineNumber);
                    }
                }
            }

            return FillMainTemplate(content, NameSpace, ReturnType, Prefix, Public, AddToken, HasLookAhead, Includes, IncludeInterface, BaseClass, UseMultiTokens);
        }

        private void LoadRules(string Text)
        {
            string[] lines = Text.Split(new char[] { '\n' }, StringSplitOptions.None);
            bool ignoreStrict = false;
            for (int i = 0; i < lines.Length; ++i)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    //skip empty lines
                    continue;
                }
                if (line.StartsWith("//"))
                {
                    //skip comments
                    continue;
                }
                if(line.StartsWith("using"))
                {
                    string[] usingParts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if(usingParts.Length != 2)
                    {
                        throw new ParserGenerateException("invalid using, on line: " + (i + 1));
                    }
                    Includes.Add(usingParts[1]);
                    continue;
                }
                if(line.StartsWith("#ignore"))
                {
                    string[] lineParts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if(lineParts.Length != 2)
                    {
                        throw new ParserGenerateException("invalid ignore statement, on line: " + (i + 1));
                    }
                    if(lineParts[1].ToLower() == "strict")
                    {
                        if(ignoreStrict == true)
                        {
                            Console.WriteLine("Warning: already ignoring strict, on line: " + (i + 1));
                        }
                        ignoreStrict = true;
                    }
                    else
                    {
                        throw new ParserGenerateException("invalid ignore property: \"" + lineParts[1] + "\", on line: " + (i + 1));
                    }
                    continue;
                }
                if(line.StartsWith("#endignore"))
                {
                    string[] lineParts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineParts.Length != 2)
                    {
                        throw new ParserGenerateException("invalid endignore statement, on line: " + (i + 1));
                    }
                    if (lineParts[1].ToLower() == "strict")
                    {
                        if (ignoreStrict == false)
                        {
                            Console.WriteLine("Warning: already not ignoring strict, on line: " + (i + 1));
                        }
                        ignoreStrict = false;
                    }
                    else
                    {
                        throw new ParserGenerateException("invalid ignore property: \"" + lineParts[1] + "\", on line: " + (i + 1));
                    }
                    continue;
                }
                string[] parts = SplitLine(line, "=>");
                if (parts.Length != 3)
                {
                    throw new ParserGenerateException("Syntax error on line: " + (i + 1));
                }
                string ruleName = TrimEscape(parts[0].Trim());
                string rule = TrimEscape(parts[1].Trim());
                string action = TrimEscape(parts[2].Trim());
                string lookAhead = null;
                parts = SplitLine(action, "LOOK");
                if(parts.Length > 1)
                {
                    lookAhead = TrimEscape(parts[1].Trim());
                    action = TrimEscape(parts[0].Trim());
                }
                Rule r = new Rule(ruleName, rule, action, lookAhead);
                if(!RuleMap.ContainsKey(r.Name))
                {
                    List<Rule> rules = new List<Rule>();
                    rules.Add(r);
                    RuleMap.Add(r.Name, rules);
                    RuleReferenceCheck.Add(r.Name, new RuleReference() { LineNumber = i + 1, Referenced = ignoreStrict });
                }
                else
                {
                    RuleMap[r.Name].Add(r);
                }
            }
        }

        private void GenerateFunctions(string ReturnType)
        {
            foreach(string ruleName in RuleMap.Keys)
            {
                List<Rule> subRules = RuleMap[ruleName];
                if(subRules.Count == 1)
                {
                    GenerateSingleSubRule(subRules[0], ReturnType);
                }
                else
                {
                    GenerateRule(subRules, ReturnType);
                }
            }
        }

        private string GenerateReturnFunction(string ReturnType, List<string> RootRules)
        {
            string func = t(2) + "public " + ReturnType + " Parse(List<Token> Tokens)\r\n";
            func += t(2) + "{\r\n";
            func += t(3) + "this.Tokens = Tokens;\r\n";
            func += t(3) + "TokenIndex = 0;\r\n";
            func += t(3) + "HighestLine = 0;\r\n";
            func += t(3) + ReturnType + " retVal = null;\r\n";
            foreach(string rule in RootRules)
            {
                func += t(3) + "if((retVal = " + GetFName(rule) + "()) != null)\r\n";
                func += t(3) + "{\r\n";
                func += t(4) + "return retVal;\r\n";
                func += t(3) + "}\r\n";
                if (RuleReferenceCheck.ContainsKey(rule))
                {
                    RuleReferenceCheck[rule].Referenced = true;
                }
            }
            func += t(3) + "return null;\r\n";
            func += t(2) + "}\r\n\r\n";
            return func;
        }

        private void GenerateRule(List<Rule> SubRules, string ReturnType)
        {
            string fName = GetFName(SubRules[0].Name);
            string body = t(2) + "private " + ReturnType + " " + fName + "(int RuleIndex=0)\r\n";
            body += t(2) + "{\r\n";
            body += t(3) + ReturnType + " matches = null;\r\n";
            for(int i = 0;i < SubRules.Count;++i)
            {
                Rule r = SubRules[i];
                int ruleIndex = i + 1;
                string subName = fName + "_" + ruleIndex;
                body += t(3) + "//" + r.Name + " => " + r.RuleGrammer + "\r\n";
                if (r.RuleParts.Length == 1 && RuleMap.ContainsKey(r.RuleParts[0]))
                {
                    body += t(3) + "if(RuleIndex != " + ruleIndex + " && (matches = " + GetFName(r.RuleParts[0]) + "()) != null)\r\n";
                    body += t(3) + "{\r\n";
                    body += t(4) + "Func<object[], " + ReturnType + "> f = x => " + r.Action + ";\r\n";
                    body += t(4) + "return f(new object[]{ matches });\r\n";
                    RuleReferenceCheck[r.RuleParts[0]].Referenced = true;
                }
                else
                {
                    string lhCode = GenerateLHCode(r);
                    body += t(3) + "if(RuleIndex != " + ruleIndex + (lhCode != null ? " " + lhCode : "") + " && (matches = " + subName + "()) != null)\r\n";
                    body += t(3) + "{\r\n";
                    body += t(4) + "return matches;\r\n";
                    GenerateSubRule(subName, ruleIndex, r, ReturnType, SubRules[0].Name);
                }
                body += t(3) + "}\r\n";
            }
            body += t(3) + "return null;\r\n";
            body += t(2) + "}\r\n";
            RuleFunction func = new RuleFunction(fName, body, SubRules[0].Name);
            Functions.Add(func);
        }

        private string GenerateLHCode(Rule R)
        {
            if (R.LookAhead != null)
            {
                string result = "";
                foreach (string part in R.RuleParts)
                {
                    if (!RuleMap.ContainsKey(part))
                    {
                        HasLookAhead = true;
                        result += "&& IsValid((tokens, tokenIndex, tokenVal) => " + R.LookAhead + ", \"" + part + "\")";
                    }
                }
                if(result.Length > 0)
                {
                    return result;
                }
                return null;
            }
            return null;
        }

        private void GenerateSubRule(string fName, int RuleIndex, Rule R, string ReturnType, string RuleName)
        {
            string body = t(2) + "private " + ReturnType + " " + fName + "()\r\n";
            body += t(2) + "{\r\n";
            body += t(3) + "//" + R.Name + " => " + R.RuleGrammer + "\r\n";
            body += t(3) + "int ti = TokenIndex;\r\n";
            body += t(3) + "object[] matches = new object[" + R.RuleParts.Length + "];\r\n";
            body += t(3) + "if(";
            bool first = true;
            int matchIndex = 0;
            foreach (string part in R.RuleParts)
            {
                if (!first)
                {
                    body += " && ";
                }
                if (RuleMap.ContainsKey(part))
                {
                    bool addIndex = first && part == R.Name;
                    body += "(matches[" + matchIndex++ + "] = " + GetFName(part) + "(" + (addIndex ? "" + RuleIndex : "") +")) != null";
                    RuleReferenceCheck[part].Referenced = true;
                }
                else
                {
                    string tokenVal = part;
                    tokenVal = tokenVal.Replace("\"", "\\\"");
                    body += "(matches[" + matchIndex++ + "] = TokenMatches(\"" + tokenVal + "\")) != null";
                }
                first = false;
            }
            body += ")\r\n";
            body += t(3) + "{\r\n";
            //body += t(4) + "return GetResult(x => " + R.Action + ", matches);\r\n";
            body += t(4) + "Func<object[], " + ReturnType + "> f = x => " + R.Action + ";\r\n";
            body += t(4) + "return f(matches);\r\n";
            body += t(3) + "}\r\n";
            body += t(3) + "TokenIndex = ti;\r\n";
            body += t(3) + "return null;\r\n";
            body += t(2) + "}\r\n";
            RuleFunction rf = new RuleFunction(fName, body, RuleName);
            Functions.Add(rf);
        }

        private void GenerateSingleSubRule(Rule R, string ReturnType)
        {
            string fName = GetFName(R.Name);
            string body = t(2) + "private " + ReturnType + " " + fName + "()\r\n";
            body += t(2) + "{\r\n";
            body += t(3) + "//" + R.Name + " => " + R.RuleGrammer + "\r\n";
            body += t(3) + "int ti = TokenIndex;\r\n";
            body += t(3) + "object[] matches = new object[" + R.RuleParts.Length + "];\r\n";
            body += t(3) + "if(";
            bool first = true;
            int matchIndex = 0;
            foreach(string part in R.RuleParts)
            {
                if(!first)
                {
                    body += " && ";
                }
                first = false;
                if(RuleMap.ContainsKey(part))
                {
                    body += "(matches[" + matchIndex++ + "] = " + GetFName(part) + "()) != null";
                    RuleReferenceCheck[part].Referenced = true;
                }
                else
                {
                    string tokenVal = part;
                    tokenVal = tokenVal.Replace("\"", "\\\"");
                    body += "(matches[" + matchIndex++ + "] = TokenMatches(\"" + tokenVal + "\")) != null";
                }
            }
            body += ")\r\n";
            body += t(3) + "{\r\n";
            //body += t(4) + "return GetResult(x => " + R.Action + ", matches);\r\n";
            body += t(4) + "Func<object[], " + ReturnType + "> f = x => " + R.Action + ";\r\n";
            body += t(4) + "return f(matches);\r\n";
            body += t(3) + "}\r\n";
            body += t(3) + "TokenIndex = ti;\r\n";
            body += t(3) + "return null;\r\n";
            body += t(2) + "}\r\n";
            RuleFunction rf = new RuleFunction(fName, body, R.Name);
            Functions.Add(rf);
        }

        private static string t(int Tabs)
        {
            string res = "";
            for(int i = 0;i < Tabs;++i)
            {
                res += "    ";
            }
            return res;
        }

        private static string GetFName(string RuleName)
        {
            return "Matches_" + RuleName;
        }

        private static string FillMainTemplate(string Content, string NameSpace, string ReturnType, string Prefix, bool Public, bool AddToken, bool HasLookAhead, List<string> Usings, bool IncludeInterface, string BaseClass, bool UseMutliTokens)
        {
            string text = "using System;\r\n";
            text += "using System.Collections.Generic;\r\n";
            if(UseMutliTokens)
            {
                text += "using System.Text;\r\n";
                text += "using System.Linq;\r\n";
            }
            foreach (string use in Usings)
            {
                text += "using " + use + ";\r\n";
            }
            string baseText = "";
            if (IncludeInterface || BaseClass != null)
            {
                baseText = ": ";
                if (BaseClass != null)
                {
                    baseText += BaseClass;
                    if (IncludeInterface)
                    {
                        baseText += ", ";
                    }
                }
                if (IncludeInterface)
                {
                    baseText += (Prefix != null ? Prefix + "_" : "") + "IParser";
                }
            }
            text += "\r\n";
            text += "/* Warning: this file is generated, changes made here may be be overwritten. */\r\n";
            text += "\r\n";
            text += "namespace " + NameSpace + "\r\n";
            text += "{\r\n";
            text += t(1) + (Public ? "public " : "") + "class " + (Prefix != null ? Prefix + "_" : "") + "Parser" + baseText + "\r\n";
            text += t(1) + "{\r\n";
            text += t(2) + "private int TokenIndex;\r\n";
            text += t(2) + "private List<Token> Tokens;\r\n";
            text += t(2) + "public int HighestLine { get; private set; }\r\n";
            text += Content;
            text += t(2) + "private Token TokenMatches(string TestString)\r\n";
            text += t(2) + "{\r\n";
            if (UseMutliTokens)
            {
                text += t(3) + "if(TokenIndex < Tokens.Count && Tokens[TokenIndex].Names.Contains(TestString))\r\n";
            }
            else
            {
                text += t(3) + "if(TokenIndex < Tokens.Count && Tokens[TokenIndex].Name == TestString)\r\n";
            }
            text += t(3) + "{\r\n";
            text += t(4) + "Token t = Tokens[TokenIndex++];\r\n";
            text += t(4) + "if(t.LineNumber > HighestLine)\r\n";
            text += t(4) + "{\r\n";
            text += t(5) + "HighestLine = t.LineNumber;\r\n";
            text += t(4) + "}\r\n";
            text += t(4) + "return t;\r\n";
            text += t(3) + "}\r\n";
            text += t(3) + "return null;\r\n";
            text += t(2) + "}\r\n\r\n";
            /*text += t(2) + "private " + ReturnType + " GetResult(Func<object[], " + ReturnType + "> RF, object[] Objs)\r\n";
            text += t(2) + "{\r\n";
            text += t(3) + "return RF(Objs);\r\n";
            text += t(2) + "}\r\n";*/
            if (HasLookAhead)
            {
                text += "\r\n" + t(2) + "private bool IsValid(Func<List<Token>, int, string, bool> LH, string TokenVal)\r\n";
                text += t(2) + "{\r\n";
                text += t(3) + "return LH(Tokens, TokenIndex, TokenVal);\r\n";
                text += t(2) + "}\r\n";
            }
            text += t(1) + "}\r\n";
            if(AddToken)
            {
                text += "\r\n" + t(1) + (Public ? "public " : "") + "class " + (Prefix != null ? Prefix + "_" : "") + "Token\r\n";
                text += t(1) + "{\r\n";
                if (UseMutliTokens)
                {
                    text += t(2) + "public string[] Names { get; set; }\r\n";
                }
                else
                {
                    text += t(2) + "public string Name { get; set; }\r\n";
                }
                text += t(2) + "public string Value { get; set; }\r\n";
                text += t(2) + "public int LineNumber { get; set; }\r\n\r\n";
                if (UseMutliTokens)
                {
                    text += t(2) + "public " + (Prefix != null ? Prefix + "_" : "") + "Token(string[] Names, string Value)\r\n";
                }
                else
                {
                    text += t(2) + "public " + (Prefix != null ? Prefix + "_" : "") + "Token(string Name, string Value)\r\n";
                }
                text += t(2) + "{\r\n";
                if (UseMutliTokens)
                {
                    text += t(3) + "this.Names = Names;\r\n";
                }
                else
                {
                    text += t(3) + "this.Name = Name;\r\n";
                }
                text += t(3) + "this.Value = Value;\r\n";
                text += t(3) + "LineNumber = 0;\r\n";
                text += t(2) + "}\r\n\r\n";
                if (UseMutliTokens)
                {
                    text += t(2) + "public " + (Prefix != null ? Prefix + "_" : "") + "Token(string[] Names, string Value, int LineNumber) : this(Names, Value) \r\n";
                }
                else
                {
                    text += t(2) + "public " + (Prefix != null ? Prefix + "_" : "") + "Token(string Name, string Value, int LineNumber) : this(Name, Value) \r\n";
                }
                text += t(2) + "{\r\n";
                text += t(3) + "this.LineNumber = LineNumber;\r\n";
                text += t(2) + "}\r\n\r\n";
                if (UseMutliTokens)
                {
                    text += t(2) + "public " + (Prefix != null ? Prefix + "_" : "") + "Token(string Name, string Value) : this(new string[]{ Name }, Value) { }\r\n\r\n";
                    text += t(2) + "public " + (Prefix != null ? Prefix + "_" : "") + "Token(string Name, string Value, int LineNumber) : this(new string[]{ Name }, Value, LineNumber) { }\r\n\r\n";
                    text += t(2) + "public " + (Prefix != null ? Prefix + "_" : "") + "Token(string Name, string AltName, string Value) : this(new string[]{ Name, AltName }, Value) { }\r\n\r\n";
                    text += t(2) + "public " + (Prefix != null ? Prefix + "_" : "") + "Token(string Name, string AltName, string Value, int LineNumber) : this(new string[]{ Name, AltName }, Value, LineNumber) { }\r\n\r\n";
                }
                text += t(2) + "public override string ToString()\r\n";
                text += t(2) + "{\r\n";
                if (UseMutliTokens)
                {
                    text += t(3) + "if(Names.Length > 0)\r\n";
                    text += t(3) + "{\r\n";
                    text += t(4) + "return Names[0] + \": \" + Value;\r\n";
                    text += t(3) + "}\r\n";
                    text += t(3) + "return string.Empty;\r\n";
                }
                else
                {
                    text += t(3) + "return Name + \": \" + Value;\r\n";
                }
                text += t(2) + "}\r\n";
                text += t(1) + "}\r\n";
            }
            if(IncludeInterface)
            {
                text += "\r\n" + t(1) + (Public ? "public " : "") + "interface " + (Prefix != null ? Prefix + "_" : "") + "IParser\r\n";
                text += t(1) + "{\r\n";
                text += t(2) + ReturnType + " " + "Parse(List<Token> Tokens);\r\n";
                text += t(2) + "int HighestLine { get; }\r\n";
                text += t(1) + "}\r\n";
            }
            text += "}\r\n";
            return text;
        }

        private string[] SplitLine(string Line, string Split)
        {
            if(Split.Length == 0 || Line.Length == 0)
            {
                return new string[0];
            }
            List<string> parts = new List<string>();
            string current = "";
            for(int i = 0;i < Line.Length;++i)
            {
                bool split = true;
                if (i > 0 && Line[i - 1] != '\\')
                {
                    for (int n = 0; n < Split.Length && n + i < Line.Length; ++n)
                    {
                        if (Line[i + n] != Split[n])
                        {
                            split = false;
                            break;
                        }
                    }
                    if (split)
                    {
                        i += Split.Length - 1;
                    }
                }
                else
                {
                    split = false;
                }
                if (split)
                {
                    parts.Add(current);
                    current = "";
                }
                else
                {
                    current += Line[i];
                }
            }

            if(current.Length > 0)
            {
                parts.Add(current);
            }

            return parts.ToArray();
        }

        private string TrimEscape(string Text)
        {
            string result = "";
            for(int i = 0;i < Text.Length;++i)
            {
                if(Text[i] == '\\' && i + 1 < Text.Length && Text[i + 1] != '\\')
                {
                    continue;
                }
                result += Text[i];
            }
            return result;
        }
    }

    internal class Rule
    {
        public string Name { get; set; }
        public string RuleGrammer { get; set; }
        public string Action { get; set; }
        public string LookAhead { get; set; }

        public string[] RuleParts { get; private set; }

        public Rule(string Name, string Rule, string Action, string LookAhead) : this(Name, Rule, Action)
        {
            this.LookAhead = LookAhead;
        }

        public Rule(string Name, string Rule, string Action)
        {
            this.Name = Name;
            this.RuleGrammer = Rule;
            this.Action = Action;
            RuleParts = RuleGrammer.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    internal class RuleFunction
    {
        public string FunctionName { get; set; }
        public string FunctionText { get; set; }
        public string RuleName { get; set; }

        public RuleFunction(string Name, string Text, string RuleName)
        {
            FunctionName = Name;
            FunctionText = Text;
            this.RuleName = RuleName;
        }
    }

    internal class RuleReference
    {
        public int LineNumber { get; set; }
        public bool Referenced { get; set; }
    }

    public class ParserGenerateException : Exception
    {
        public ParserGenerateException(string Message) : base(Message) { }
    }
}
