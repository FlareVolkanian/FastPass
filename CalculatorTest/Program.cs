using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculatorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            while(true)
            {
                Console.Write(">");
                string input = Console.ReadLine();
                if(input == "")
                {
                    break;
                }
                List<Token> tokens = Tokenise(input);
                Parser p = new Parser();
                double? result = p.Parse(tokens);
                Console.WriteLine(result.HasValue ? "" + result.Value : "null");
            }
        }

        private static List<Token> Tokenise(string Input)
        {
            int count = 0;
            List<Token> tokens = new List<Token>();
            while (count < Input.Length)
            {
                if (Input[count] == '+')
                {
                    tokens.Add(new Token("+", "+"));
                    count++;
                }
                else if (Input[count] == '-')
                {
                    tokens.Add(new Token("-", "-"));
                    count++;
                }
                else if (Input[count] == '*')
                {
                    tokens.Add(new Token("*", "*"));
                    count++;
                }
                else if (Input[count] == '/')
                {
                    tokens.Add(new Token("/", "/"));
                    count++;
                }
                else if(Input[count] == '(')
                {
                    tokens.Add(new Token("(", "("));
                    count++;
                }
                else if (Input[count] == ')')
                {
                    tokens.Add(new Token(")", ")"));
                    count++;
                }
                else if (Input[count] == '^')
                {
                    tokens.Add(new Token("^", "^"));
                    count++;
                }
                else if(Input[count] == 'e' || Input[count] == 'E')
                {
                    tokens.Add(new Token("E", "E"));
                    count++;
                }
                else if(Input[count] >= '0' && Input[count] <= '9')
                {
                    string chars = "";
                    while(count < Input.Length && (Input[count] == '.' || (Input[count] >= '0' && Input[count] <= '9')))
                    {
                        chars += Input[count++];
                    }
                    tokens.Add(new Token("NUM", chars));
                }
                else
                {
                    //skip this character
                    count++;
                }
            }
            return tokens;
        }
    }

    /*class Token
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public Token(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
    }*/
}
