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
            try
            {
                var compiler = new Compiler();
                var path = Path.Combine(Environment.CurrentDirectory, @"samples/main.rb");
                string source = File.ReadAllText( path, Encoding.UTF8);
                compiler.Compile(Path.GetFileNameWithoutExtension(path), source);
                /**foreach (var compilerToken in compiler.Tokens)
                {
                    Console.WriteLine(compilerToken);
                }**/
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }
    }
}