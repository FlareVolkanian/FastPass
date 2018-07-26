using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace CalculatorTest
{
    class Parser
    {
        private int TokenIndex;
        private List<Token> Tokens;
        public int HighestLine { get; private set; }

        public double? Parse(List<Token> Tokens)
        {
            this.Tokens = Tokens;
            TokenIndex = 0;
            HighestLine = 0;
            double? retVal = null;
            if ((retVal = Matches_root()) != null)
            {
                return retVal;
            }
            return null;
        }

        private double? Matches_root()
        {
            //root => add
            int ti = TokenIndex;
            object[] matches = new object[1];
            if ((matches[0] = Matches_add()) != null)
            {
                Func<object[], double?> f = x => x[0] as double?;
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_add_1()
        {
            //add => mult + add
            int ti = TokenIndex;
            object[] matches = new object[3];
            if ((matches[0] = Matches_mult()) != null && (matches[1] = TokenMatches("+")) != null && (matches[2] = Matches_add()) != null)
            {
                Func<object[], double?> f = x => (x[0] as double?).Value + (x[2] as double?).Value;
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_add_2()
        {
            //add => mult - add
            int ti = TokenIndex;
            object[] matches = new object[3];
            if ((matches[0] = Matches_mult()) != null && (matches[1] = TokenMatches("-")) != null && (matches[2] = Matches_add()) != null)
            {
                Func<object[], double?> f = x => (x[0] as double?).Value - (x[2] as double?).Value;
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_add(int RuleIndex = 0)
        {
            double? matches = null;
            //add => mult + add
            if (RuleIndex != 1 && (matches = Matches_add_1()) != null)
            {
                return matches;
            }
            //add => mult - add
            if (RuleIndex != 2 && (matches = Matches_add_2()) != null)
            {
                return matches;
            }
            //add => mult
            if (RuleIndex != 3 && (matches = Matches_mult()) != null)
            {
                Func<object[], double?> f = x => x[0] as double?;
                return f(new object[] { matches });
            }
            return null;
        }

        private double? Matches_mult_1()
        {
            //mult => pow * mult
            int ti = TokenIndex;
            object[] matches = new object[3];
            if ((matches[0] = Matches_pow()) != null && (matches[1] = TokenMatches("*")) != null && (matches[2] = Matches_mult()) != null)
            {
                Func<object[], double?> f = x => (x[0] as double?).Value * (x[2] as double?).Value;
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_mult_2()
        {
            //mult => pow / mult
            int ti = TokenIndex;
            object[] matches = new object[3];
            if ((matches[0] = Matches_pow()) != null && (matches[1] = TokenMatches("/")) != null && (matches[2] = Matches_mult()) != null)
            {
                Func<object[], double?> f = x => (x[0] as double?).Value / (x[2] as double?).Value;
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_mult(int RuleIndex = 0)
        {
            double? matches = null;
            //mult => pow * mult
            if (RuleIndex != 1 && (matches = Matches_mult_1()) != null)
            {
                return matches;
            }
            //mult => pow / mult
            if (RuleIndex != 2 && (matches = Matches_mult_2()) != null)
            {
                return matches;
            }
            //mult => pow
            if (RuleIndex != 3 && (matches = Matches_pow()) != null)
            {
                Func<object[], double?> f = x => x[0] as double?;
                return f(new object[] { matches });
            }
            return null;
        }

        private double? Matches_pow_1()
        {
            //pow => atom ^ pow
            int ti = TokenIndex;
            object[] matches = new object[3];
            if ((matches[0] = Matches_atom()) != null && (matches[1] = TokenMatches("^")) != null && (matches[2] = Matches_pow()) != null)
            {
                Func<object[], double?> f = x => Math.Pow((x[0] as double?).Value, (x[2] as double?).Value);
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_pow(int RuleIndex = 0)
        {
            double? matches = null;
            //pow => atom ^ pow
            if (RuleIndex != 1 && (matches = Matches_pow_1()) != null)
            {
                return matches;
            }
            //pow => atom
            if (RuleIndex != 2 && (matches = Matches_atom()) != null)
            {
                Func<object[], double?> f = x => x[0] as double?;
                return f(new object[] { matches });
            }
            return null;
        }

        private double? Matches_atom_1()
        {
            //atom => atom E atom
            int ti = TokenIndex;
            object[] matches = new object[3];
            if ((matches[0] = Matches_atom(1)) != null && (matches[1] = TokenMatches("E")) != null && (matches[2] = Matches_atom()) != null)
            {
                Func<object[], double?> f = x => (x[0] as double?).Value * Math.Pow(10, (x[2] as double?).Value);
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_atom_2()
        {
            //atom => - atom
            int ti = TokenIndex;
            object[] matches = new object[2];
            if ((matches[0] = TokenMatches("-")) != null && (matches[1] = Matches_atom()) != null)
            {
                Func<object[], double?> f = x => -((x[1] as double?).Value);
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_atom_3()
        {
            //atom => + atom
            int ti = TokenIndex;
            object[] matches = new object[2];
            if ((matches[0] = TokenMatches("+")) != null && (matches[1] = Matches_atom()) != null)
            {
                Func<object[], double?> f = x => Math.Abs((x[1] as double?).Value);
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_atom_4()
        {
            //atom => ( add )
            int ti = TokenIndex;
            object[] matches = new object[3];
            if ((matches[0] = TokenMatches("(")) != null && (matches[1] = Matches_add()) != null && (matches[2] = TokenMatches(")")) != null)
            {
                Func<object[], double?> f = x => x[1] as double?;
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_atom_5()
        {
            //atom => NUM
            int ti = TokenIndex;
            object[] matches = new object[1];
            if ((matches[0] = TokenMatches("NUM")) != null)
            {
                Func<object[], double?> f = x => double.Parse((x[0] as Token).Value);
                return f(matches);
            }
            TokenIndex = ti;
            return null;
        }

        private double? Matches_atom(int RuleIndex = 0)
        {
            double? matches = null;
            //atom => atom E atom
            if (RuleIndex != 1 && (matches = Matches_atom_1()) != null)
            {
                return matches;
            }
            //atom => - atom
            if (RuleIndex != 2 && (matches = Matches_atom_2()) != null)
            {
                return matches;
            }
            //atom => + atom
            if (RuleIndex != 3 && (matches = Matches_atom_3()) != null)
            {
                return matches;
            }
            //atom => ( add )
            if (RuleIndex != 4 && (matches = Matches_atom_4()) != null)
            {
                return matches;
            }
            //atom => NUM
            if (RuleIndex != 5 && (matches = Matches_atom_5()) != null)
            {
                return matches;
            }
            return null;
        }

        private Token TokenMatches(string TestString)
        {
            if (TokenIndex < Tokens.Count && Tokens[TokenIndex].Names.Contains(TestString))
            {
                Token t = Tokens[TokenIndex++];
                if (t.LineNumber > HighestLine)
                {
                    HighestLine = t.LineNumber;
                }
                return t;
            }
            return null;
        }

    }

    class Token
    {
        public string[] Names { get; set; }
        public string Value { get; set; }
        public int LineNumber { get; set; }

        public Token(string[] Names, string Value)
        {
            this.Names = Names;
            this.Value = Value;
            LineNumber = 0;
        }

        public Token(string[] Names, string Value, int LineNumber) : this(Names, Value)
        {
            this.LineNumber = LineNumber;
        }

        public Token(string Name, string Value) : this(new string[] { Name }, Value) { }

        public Token(string Name, string Value, int LineNumber) : this(new string[] { Name }, Value, LineNumber) { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Names: ");
            foreach (string name in Names)
            {
                sb.Append(name);
                sb.Append(", ");
            }
            string result = sb.ToString();
            if (Names.Length > 0)
            {
                result = result.Substring(0, result.Length - 2);
            }
            return result;
        }
    }
}
