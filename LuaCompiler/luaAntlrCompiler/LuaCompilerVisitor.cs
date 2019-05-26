using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime.Tree;
using LLVMSharp;

namespace LuaCompiler.luaAntlrCompiler
{
    public class LuaCompilerVisitor : LuaBaseVisitor<CompilerResult>
    {
        private LLVMModuleRef module;
        private LLVMValueRef method;
        private LLVMBuilderRef builder;
        private Stack<Context> contexts;
        private LLVMPassManagerRef passManager;

        public LuaCompilerVisitor(string bytecodePath)
        {
            this.BytecodePath = bytecodePath;
        }

        public string BytecodePath { get; set; }

        public override CompilerResult VisitChunk(LuaParser.ChunkContext context)
        {
            module = LLVM.ModuleCreateWithName("Lua");
            contexts = new Stack<Context>();

            LLVMPassManagerRef passManager = LLVM.CreateFunctionPassManagerForModule(module);

            // Set up the optimizer pipeline.  Start with registering info about how the
            // target lays out data structures.
            // LLVM.DisposeTargetData(LLVM.GetExecutionEngineTargetData(engine));

            // Provide basic AliasAnalysis support for GVN.
            LLVM.AddBasicAliasAnalysisPass(passManager);

            // Promote allocas to registers.
            LLVM.AddPromoteMemoryToRegisterPass(passManager);

            // Do simple "peephole" optimizations and bit-twiddling optzns.
            LLVM.AddInstructionCombiningPass(passManager);

            // Reassociate expressions.
            LLVM.AddReassociatePass(passManager);

            // Eliminate Common SubExpressions.
            LLVM.AddGVNPass(passManager);

            // Simplify the control flow graph (deleting unreachable blocks, etc).
            LLVM.AddCFGSimplificationPass(passManager);

            LLVM.InitializeFunctionPassManager(passManager);
            this.passManager = passManager;

            base.VisitChunk(context);



            LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string str);

            LLVM.DumpModule(module);

            LLVM.WriteBitcodeToFile(module, BytecodePath);

            LLVM.DisposePassManager(passManager);
            LLVM.DisposeModule(module);
            
            return NullCompilerResult.INSTANCE;
        }

        public override CompilerResult VisitBlock(LuaParser.BlockContext context)
        {
            return base.VisitBlock(context);
        }

        public override CompilerResult VisitStat(LuaParser.StatContext context)
        {
            return base.VisitStat(context);
        }

        public override CompilerResult VisitRetstat(LuaParser.RetstatContext context)
        {
            return base.VisitRetstat(context);
        }

        public override CompilerResult VisitLabel(LuaParser.LabelContext context)
        {
            return base.VisitLabel(context);
        }

        public override CompilerResult VisitFuncname(LuaParser.FuncnameContext context)
        {
            return base.VisitFuncname(context);
        }

        public override CompilerResult VisitVarlist(LuaParser.VarlistContext context)
        {
            return base.VisitVarlist(context);
        }

        public override CompilerResult VisitNamelist(LuaParser.NamelistContext context)
        {
            return base.VisitNamelist(context);
        }

        public override CompilerResult VisitExplist(LuaParser.ExplistContext context)
        {
            return base.VisitExplist(context);
        }

        public override CompilerResult VisitExp(LuaParser.ExpContext context)
        {
            return base.VisitExp(context);
        }

        public override CompilerResult VisitPrefixexp(LuaParser.PrefixexpContext context)
        {
            return base.VisitPrefixexp(context);
        }

        public override CompilerResult VisitFunctioncall(LuaParser.FunctioncallContext context)
        {
            return base.VisitFunctioncall(context);
        }

        public override CompilerResult VisitVarOrExp(LuaParser.VarOrExpContext context)
        {
            return base.VisitVarOrExp(context);
        }

        public override CompilerResult VisitVar(LuaParser.VarContext context)
        {
            return base.VisitVar(context);
        }

        public override CompilerResult VisitVarSuffix(LuaParser.VarSuffixContext context)
        {
            return base.VisitVarSuffix(context);
        }

        public override CompilerResult VisitNameAndArgs(LuaParser.NameAndArgsContext context)
        {
            return base.VisitNameAndArgs(context);
        }

        public override CompilerResult VisitArgs(LuaParser.ArgsContext context)
        {
            return base.VisitArgs(context);
        }

        public override CompilerResult VisitFunctiondef(LuaParser.FunctiondefContext context)
        {
            return base.VisitFunctiondef(context);
        }

        public override CompilerResult VisitFuncbody(LuaParser.FuncbodyContext context)
        {
            return base.VisitFuncbody(context);
        }

        public override CompilerResult VisitParlist(LuaParser.ParlistContext context)
        {
            return base.VisitParlist(context);
        }

        public override CompilerResult VisitTableconstructor(LuaParser.TableconstructorContext context)
        {
            return base.VisitTableconstructor(context);
        }

        public override CompilerResult VisitFieldlist(LuaParser.FieldlistContext context)
        {
            return base.VisitFieldlist(context);
        }

        public override CompilerResult VisitField(LuaParser.FieldContext context)
        {
            return base.VisitField(context);
        }

        public override CompilerResult VisitFieldsep(LuaParser.FieldsepContext context)
        {
            return base.VisitFieldsep(context);
        }

        public override CompilerResult VisitOperatorOr(LuaParser.OperatorOrContext context)
        {
            return base.VisitOperatorOr(context);
        }

        public override CompilerResult VisitOperatorAnd(LuaParser.OperatorAndContext context)
        {
            return base.VisitOperatorAnd(context);
        }

        public override CompilerResult VisitOperatorComparison(LuaParser.OperatorComparisonContext context)
        {
            return base.VisitOperatorComparison(context);
        }

        public override CompilerResult VisitOperatorStrcat(LuaParser.OperatorStrcatContext context)
        {
            return base.VisitOperatorStrcat(context);
        }

        public override CompilerResult VisitOperatorAddSub(LuaParser.OperatorAddSubContext context)
        {
            return base.VisitOperatorAddSub(context);
        }

        public override CompilerResult VisitOperatorMulDivMod(LuaParser.OperatorMulDivModContext context)
        {
            return base.VisitOperatorMulDivMod(context);
        }

        public override CompilerResult VisitOperatorBitwise(LuaParser.OperatorBitwiseContext context)
        {
            return base.VisitOperatorBitwise(context);
        }

        public override CompilerResult VisitOperatorUnary(LuaParser.OperatorUnaryContext context)
        {
            return base.VisitOperatorUnary(context);
        }

        public override CompilerResult VisitOperatorPower(LuaParser.OperatorPowerContext context)
        {
            return base.VisitOperatorPower(context);
        }

        public override CompilerResult VisitNumber(LuaParser.NumberContext context)
        {
            return base.VisitNumber(context);
        }

        public override CompilerResult VisitString(LuaParser.StringContext context)
        {
            return base.VisitString(context);
        }

        public override CompilerResult Visit(IParseTree tree)
        {
            return base.Visit(tree);
        }

        public override CompilerResult VisitChildren(IRuleNode node)
        {
            return base.VisitChildren(node);
        }

        public override CompilerResult VisitTerminal(ITerminalNode node)
        {
            
            return base.VisitTerminal(node);
        }

        public override CompilerResult VisitErrorNode(IErrorNode node)
        {
            return base.VisitErrorNode(node);
        }
    }
}