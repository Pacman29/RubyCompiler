using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace LuaCompiler.luaAntlrCompiler
{
    public class Compiler
    {
        
        
        public void Compile(string fileName, string source)
        {
            Tokens = new List<IToken>();
            
            LuaLexer lexer = null;
            LuaParser parser = null;

            try
            {
                var stream = new AntlrInputStream(source);
                lexer = new LuaLexer(stream);
                var token = lexer.NextToken();
                while (token.Type != LuaLexer.Eof)
                {
                    Tokens.Add(token);
                    token = lexer.NextToken();
                }
                lexer.Reset();
                lexer.Line = 0;
                lexer._tokenStartCharPositionInLine = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                var tokenStream = new CommonTokenStream(lexer);
                parser = new LuaParser(tokenStream);
                Tree = parser.chunk();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public IParseTree Tree { get; set; }

        public List<IToken> Tokens { get; set; }
    }
}