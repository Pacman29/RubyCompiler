using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace RubyCompiler.rubyAntlrCompiler
{
    public class Compiler
    {
        public void Compile(string fileName, string source)
        {
            Tokens = new List<IToken>();

            RubyLexer lexer = null;
            RubyParser parser = null;


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


            var tokenStream = new CommonTokenStream(lexer);
            parser = new RubyParser(tokenStream);
            Tree = parser.prog();


            var walker = new ParseTreeWalker();
            var listener = new RubyCompilerListener();
            walker.Walk(listener, Tree);

            if (listener.HasSemanticError())
                throw new Exception("Semantic error", new Exception(listener.GetErrors()));
            else
                listener.CreateIRFile(Path.Combine(Environment.CurrentDirectory, $"test_bytecode/{fileName}.pir"));
        }

        public IParseTree Tree { get; set; }

        public List<IToken> Tokens { get; set; }
    }
}