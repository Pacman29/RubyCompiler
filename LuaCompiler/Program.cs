using System;
using System.IO;
using System.Text;
using LuaCompiler.luaAntlrCompiler;

namespace LuaCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var compiler = new Compiler();
            string source = File.ReadAllText( Path.Combine(Environment.CurrentDirectory,  @"samples/factorial.lua"), Encoding.UTF8);
            compiler.Compile("aaa", source);
        }
    }
}