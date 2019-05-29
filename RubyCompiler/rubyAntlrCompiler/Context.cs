using System.Collections.Generic;
using LLVMSharp;

namespace RubyCompiler.rubyAntlrCompiler
{
    public class Context
    {
        private Context parent;
        private List<Parameter> parameterList = new List<Parameter>();
        private List<LocalVariable> localVariables = new List<LocalVariable>();

        public Context(Context parent)
        {
            this.parent = parent;
        }

        public void registerParameter(LLVMValueRef method, string name, uint index)
        {
            parameterList.Add(new Parameter(name, method, index));
        }

        public LocalVariable registerLocalVariable(LLVMBuilderRef builder, LLVMValueRef method, string name)
        {
            LocalVariable lv = new LocalVariable(builder, name, method);

            localVariables.Add(lv);

            return lv;
        }

        public LocalVariable lookupLocalVar(string name)
        {
            foreach (var item in localVariables)
            {
                if (item.name.Equals(name)) return item;
            }

            return parent != null ? parent.lookupLocalVar(name) : null;
        }

        public Parameter lookupParameter(string name)
        {
            foreach (var item in parameterList)
            {
                if (item.name.Equals(name)) return item;
            }

            return parent != null ? parent.lookupParameter(name) : null;
        }
    }
}