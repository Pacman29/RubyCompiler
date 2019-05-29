using LLVMSharp;

namespace RubyCompiler.rubyAntlrCompiler
{
    public class Parameter
    {
        public string name;
        public LLVMValueRef method;
        public uint index;

        public Parameter(string name, LLVMValueRef method, uint index)
        {
            this.name = name;
            this.method = method;
            this.index = index;
        }

        public LLVMValueRef Load()
        {
            return LLVM.GetParam(method, index);
        }    
    }
}