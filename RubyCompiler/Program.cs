using System;
using System.IO;
using System.Text;
using RubyCompiler.rubyAntlrCompiler;

namespace RubyCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var compiler = new Compiler();
            string source = File.ReadAllText( Path.Combine(Environment.CurrentDirectory,  @"samples/num.lua"), Encoding.UTF8);
            compiler.Compile("aaa", source);
            foreach (var compilerToken in compiler.Tokens)
            {
                Console.WriteLine(compilerToken);
            }
        }
    }
}