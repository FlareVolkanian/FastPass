using System;
using System.Collections.Generic;

namespace FastPass
{
    class Parser
    {
        private int TokenIndex;
        private List<Token> Tokens;

        public object Parse(List<Token> Tokens)
        {
            this.Tokens = Tokens;
            TokenIndex = 0;
            object retVal = null;
            if((retVal = Matches_root()) != null)
            {
                return retVal;
            }
            return null;
        }

        private object Matches_root()
        {
            //root => lst
            int ti = TokenIndex;
            object[] matches = new object[1];
            if((matches[0] = Matches_lst()) != null)
            {
                return GetResult(x => x[0], matches);
            }
            TokenIndex = ti;
            return null;
        }

        private object Matches_lst_1()
        {
            //lst => line lst
            int ti = TokenIndex;
            object[] matches = new object[2];
            if((matches[0] = Matches_line()) != null && (matches[1] = Matches_lst()) != null)
            {
                return GetResult(x => { var lst = new List<Rule>(); lst.Add(x[0] as Rule); lst.AddRange(x[1] as List<Rule>); return lst; }, matches);
            }
            TokenIndex = ti;
            return null;
        }

        private object Matches_lst(int RuleIndex=0)
        {
            object matches = null;
            //lst => line lst
            if(RuleIndex != 1 && (matches = Matches_lst_1()) != null)
            {
                return matches;
            }
            //lst => line
            if(RuleIndex != 2 && (matches = Matches_line()) != null)
            {
                return matches;
            }
            return null;
        }

        private object Matches_line_1()
        {
            //line => TEXT => TEXT => TEXT LOOK TEXT
            int ti = TokenIndex;
            object[] matches = new object[7];
            if((matches[0] = TokenMatches("TEXT")) != null && (matches[1] = TokenMatches("=>")) != null && (matches[2] = TokenMatches("TEXT")) != null && (matches[3] = TokenMatches("=>")) != null && (matches[4] = TokenMatches("TEXT")) != null && (matches[5] = TokenMatches("LOOK")) != null && (matches[6] = TokenMatches("TEXT")) != null)
            {
                return GetResult(x => new Rule((x[0] as Token).Value, (x[2] as Token).Value, (x[4] as Token).Value, (x[6] as Token).Value), matches);
            }
            TokenIndex = ti;
            return null;
        }

        private object Matches_line_2()
        {
            //line => TEXT => TEXT => TEXT
            int ti = TokenIndex;
            object[] matches = new object[5];
            if((matches[0] = TokenMatches("TEXT")) != null && (matches[1] = TokenMatches("=>")) != null && (matches[2] = TokenMatches("TEXT")) != null && (matches[3] = TokenMatches("=>")) != null && (matches[4] = TokenMatches("TEXT")) != null)
            {
                return GetResult(x => new Rule((x[0] as Token).Value, (x[2] as Token).Value, (x[4] as Token).Value), matches);
            }
            TokenIndex = ti;
            return null;
        }

        private object Matches_line(int RuleIndex=0)
        {
            object matches = null;
            //line => TEXT => TEXT => TEXT LOOK TEXT
            if(RuleIndex != 1 && (matches = Matches_line_1()) != null)
            {
                return matches;
            }
            //line => TEXT => TEXT => TEXT
            if(RuleIndex != 2 && (matches = Matches_line_2()) != null)
            {
                return matches;
            }
            return null;
        }

        private Token TokenMatches(string TestString)
        {
            if(TokenIndex < Tokens.Count && Tokens[TokenIndex].Name == TestString)
            {
                return Tokens[TokenIndex++];
            }
            return null;
        }

        private object GetResult(Func<object[], object> RF, object[] Objs)
        {
            return RF(Objs);
        }
    }

    class Token
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Token(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

        public override string ToString()
        {
            return Name + ": " + Value;
        }
    }
}
