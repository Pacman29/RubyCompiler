using LLVMSharp;

namespace RubyCompiler.rubyAntlrCompiler
{
    public partial class CompilerResult
    {

    }
    class NullCompilerResult : CompilerResult
    {
        public static NullCompilerResult INSTANCE = new NullCompilerResult();

        private NullCompilerResult()
        {

        }
    }

    public class Int32CompilerResult : CompilerResult
    {
        public LLVMSharp.LLVMValueRef reference;

        public Int32CompilerResult(LLVMValueRef reference)
        {
            this.reference = reference;
        }
    }
}