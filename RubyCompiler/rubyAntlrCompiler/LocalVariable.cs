using LLVMSharp;

namespace RubyCompiler.rubyAntlrCompiler
{
    public class LocalVariable
    {
        public string name;
        public LLVMValueRef @ref;

        public LocalVariable(LLVMBuilderRef builder, string name, LLVMValueRef method)
        {
            this.name = name;
            this.@ref = LLVM.BuildAlloca(builder, LLVM.Int32Type(), name);
        }

        public LLVMValueRef Load(LLVMBuilderRef builder) => LLVM.BuildLoad(builder, @ref, "L_" + name + "tmp");

        public LLVMValueRef Assign(LLVMBuilderRef builder, Int32CompilerResult compilerResult)
        {
            LLVM.BuildStore(builder, compilerResult.reference, @ref);

            return Load(builder);
        }
    }
}