using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using rubyAntlrCompiler;

namespace RubyCompiler.rubyAntlrCompiler
{
    public class Compiler
    {
        
        
        public void Compile(string fileName, string source)
        {
            Tokens = new List<IToken>();
            
            RubyLexer lexer = null;
            RubyParser parser = null;

            try
            {
                var stream = new AntlrInputStream(source);
                lexer = new RubyLexer(stream);
                var token = lexer.NextToken();
                while (token.Type != RubyLexer.Eof)
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
                parser = new RubyParser(tokenStream);
                Tree = parser.prog();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                new RubyCompilerVisitor(Path.Combine(Environment.CurrentDirectory,  @"test_bytecode/num.bc")).Visit(Tree);
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