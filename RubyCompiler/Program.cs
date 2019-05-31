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
                if (args.Length == 0)
                {
                    Console.WriteLine("input ruby file");
                    return;
                }

                var path = Path.Combine(Environment.CurrentDirectory, args[0]);
                if (!File.Exists(path))
                {
                    Console.WriteLine("file not found");
                    return;
                }
                var compiler = new Compiler();
                string source = File.ReadAllText( path, Encoding.UTF8);
                compiler.Compile(Path.GetFileNameWithoutExtension(path), source);
                Console.WriteLine($"create file: ./test_bytecode/{Path.GetFileNameWithoutExtension(path)}.pir");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.ReadLine();
            
        }
    }
}