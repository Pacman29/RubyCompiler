using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime.Tree;
using LLVMSharp;
using rubyAntlrCompiler;

namespace RubyCompiler.rubyAntlrCompiler
{
    public class RubyCompilerVisitor : RubyBaseVisitor<CompilerResult>
    {
        private LLVMModuleRef module;
        private LLVMValueRef method;
        private LLVMBuilderRef builder;
        private Stack<Context> contexts;
        private LLVMPassManagerRef passManager;

        public RubyCompilerVisitor(string bytecodePath)
        {
            this.BytecodePath = bytecodePath;
        }

        public string BytecodePath { get; set; }

        public override CompilerResult VisitProg(RubyParser.ProgContext context)
        {
            module = LLVM.ModuleCreateWithName("Ruby");
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

            base.VisitProg(context);



            LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string str);

            LLVM.DumpModule(module);

            LLVM.WriteBitcodeToFile(module, BytecodePath);

            LLVM.DisposePassManager(passManager);
            LLVM.DisposeModule(module);
            
            return NullCompilerResult.INSTANCE;
        }

        public override CompilerResult VisitExpression_list(RubyParser.Expression_listContext context)
        {
            return base.VisitExpression_list(context);
        }

        public override CompilerResult VisitExpression(RubyParser.ExpressionContext context)
        {
            return base.VisitExpression(context);
        }

        public override CompilerResult VisitGlobal_get(RubyParser.Global_getContext context)
        {
            return base.VisitGlobal_get(context);
        }

        public override CompilerResult VisitGlobal_set(RubyParser.Global_setContext context)
        {
            return base.VisitGlobal_set(context);
        }

        public override CompilerResult VisitGlobal_result(RubyParser.Global_resultContext context)
        {
            return base.VisitGlobal_result(context);
        }

        public override CompilerResult VisitFunction_inline_call(RubyParser.Function_inline_callContext context)
        {
            return base.VisitFunction_inline_call(context);
        }

        public override CompilerResult VisitRequire_block(RubyParser.Require_blockContext context)
        {
            return base.VisitRequire_block(context);
        }

        public override CompilerResult VisitPir_inline(RubyParser.Pir_inlineContext context)
        {
            return base.VisitPir_inline(context);
        }

        public override CompilerResult VisitPir_expression_list(RubyParser.Pir_expression_listContext context)
        {
            return base.VisitPir_expression_list(context);
        }

        public override CompilerResult VisitFunction_definition(RubyParser.Function_definitionContext context)
        {
            return base.VisitFunction_definition(context);
        }

        public override CompilerResult VisitFunction_definition_body(RubyParser.Function_definition_bodyContext context)
        {
            return base.VisitFunction_definition_body(context);
        }

        public override CompilerResult VisitFunction_definition_header(RubyParser.Function_definition_headerContext context)
        {
            return base.VisitFunction_definition_header(context);
        }

        public override CompilerResult VisitFunction_name(RubyParser.Function_nameContext context)
        {
            return base.VisitFunction_name(context);
        }

        public override CompilerResult VisitFunction_definition_params(RubyParser.Function_definition_paramsContext context)
        {
            return base.VisitFunction_definition_params(context);
        }

        public override CompilerResult VisitFunction_definition_params_list(RubyParser.Function_definition_params_listContext context)
        {
            return base.VisitFunction_definition_params_list(context);
        }

        public override CompilerResult VisitFunction_definition_param_id(RubyParser.Function_definition_param_idContext context)
        {
            return base.VisitFunction_definition_param_id(context);
        }

        public override CompilerResult VisitReturn_statement(RubyParser.Return_statementContext context)
        {
            return base.VisitReturn_statement(context);
        }

        public override CompilerResult VisitFunction_call(RubyParser.Function_callContext context)
        {
            return base.VisitFunction_call(context);
        }

        public override CompilerResult VisitFunction_call_param_list(RubyParser.Function_call_param_listContext context)
        {
            return base.VisitFunction_call_param_list(context);
        }

        public override CompilerResult VisitFunction_call_params(RubyParser.Function_call_paramsContext context)
        {
            return base.VisitFunction_call_params(context);
        }

        public override CompilerResult VisitFunction_param(RubyParser.Function_paramContext context)
        {
            return base.VisitFunction_param(context);
        }

        public override CompilerResult VisitFunction_unnamed_param(RubyParser.Function_unnamed_paramContext context)
        {
            return base.VisitFunction_unnamed_param(context);
        }

        public override CompilerResult VisitFunction_named_param(RubyParser.Function_named_paramContext context)
        {
            return base.VisitFunction_named_param(context);
        }

        public override CompilerResult VisitFunction_call_assignment(RubyParser.Function_call_assignmentContext context)
        {
            return base.VisitFunction_call_assignment(context);
        }

        public override CompilerResult VisitAll_result(RubyParser.All_resultContext context)
        {
            return base.VisitAll_result(context);
        }

        public override CompilerResult VisitElsif_statement(RubyParser.Elsif_statementContext context)
        {
            return base.VisitElsif_statement(context);
        }

        public override CompilerResult VisitIf_elsif_statement(RubyParser.If_elsif_statementContext context)
        {
            return base.VisitIf_elsif_statement(context);
        }

        public override CompilerResult VisitIf_statement(RubyParser.If_statementContext context)
        {
            return base.VisitIf_statement(context);
        }

        public override CompilerResult VisitUnless_statement(RubyParser.Unless_statementContext context)
        {
            return base.VisitUnless_statement(context);
        }

        public override CompilerResult VisitWhile_statement(RubyParser.While_statementContext context)
        {
            return base.VisitWhile_statement(context);
        }

        public override CompilerResult VisitFor_statement(RubyParser.For_statementContext context)
        {
            return base.VisitFor_statement(context);
        }

        public override CompilerResult VisitInit_expression(RubyParser.Init_expressionContext context)
        {
            return base.VisitInit_expression(context);
        }

        public override CompilerResult VisitAll_assignment(RubyParser.All_assignmentContext context)
        {
            return base.VisitAll_assignment(context);
        }

        public override CompilerResult VisitFor_init_list(RubyParser.For_init_listContext context)
        {
            return base.VisitFor_init_list(context);
        }

        public override CompilerResult VisitCond_expression(RubyParser.Cond_expressionContext context)
        {
            return base.VisitCond_expression(context);
        }

        public override CompilerResult VisitLoop_expression(RubyParser.Loop_expressionContext context)
        {
            return base.VisitLoop_expression(context);
        }

        public override CompilerResult VisitFor_loop_list(RubyParser.For_loop_listContext context)
        {
            return base.VisitFor_loop_list(context);
        }

        public override CompilerResult VisitStatement_body(RubyParser.Statement_bodyContext context)
        {
            return base.VisitStatement_body(context);
        }

        public override CompilerResult VisitStatement_expression_list(RubyParser.Statement_expression_listContext context)
        {
            return base.VisitStatement_expression_list(context);
        }

        public override CompilerResult VisitAssignment(RubyParser.AssignmentContext context)
        {
            return base.VisitAssignment(context);
        }

        public override CompilerResult VisitDynamic_assignment(RubyParser.Dynamic_assignmentContext context)
        {
            return base.VisitDynamic_assignment(context);
        }

        public override CompilerResult VisitInt_assignment(RubyParser.Int_assignmentContext context)
        {
            return base.VisitInt_assignment(context);
        }

        public override CompilerResult VisitFloat_assignment(RubyParser.Float_assignmentContext context)
        {
            return base.VisitFloat_assignment(context);
        }

        public override CompilerResult VisitString_assignment(RubyParser.String_assignmentContext context)
        {
            return base.VisitString_assignment(context);
        }

        public override CompilerResult VisitInitial_array_assignment(RubyParser.Initial_array_assignmentContext context)
        {
            return base.VisitInitial_array_assignment(context);
        }

        public override CompilerResult VisitArray_assignment(RubyParser.Array_assignmentContext context)
        {
            return base.VisitArray_assignment(context);
        }

        public override CompilerResult VisitArray_definition(RubyParser.Array_definitionContext context)
        {
            return base.VisitArray_definition(context);
        }

        public override CompilerResult VisitArray_definition_elements(RubyParser.Array_definition_elementsContext context)
        {
            return base.VisitArray_definition_elements(context);
        }

        public override CompilerResult VisitArray_selector(RubyParser.Array_selectorContext context)
        {
            return base.VisitArray_selector(context);
        }

        public override CompilerResult VisitDynamic_result(RubyParser.Dynamic_resultContext context)
        {
            return base.VisitDynamic_result(context);
        }

        public override CompilerResult VisitDynamic(RubyParser.DynamicContext context)
        {
            return base.VisitDynamic(context);
        }

        public override CompilerResult VisitInt_result(RubyParser.Int_resultContext context)
        {
            return base.VisitInt_result(context);
        }

        public override CompilerResult VisitFloat_result(RubyParser.Float_resultContext context)
        {
            return base.VisitFloat_result(context);
        }

        public override CompilerResult VisitString_result(RubyParser.String_resultContext context)
        {
            return base.VisitString_result(context);
        }

        public override CompilerResult VisitComparison_list(RubyParser.Comparison_listContext context)
        {
            return base.VisitComparison_list(context);
        }

        public override CompilerResult VisitComparison(RubyParser.ComparisonContext context)
        {
            return base.VisitComparison(context);
        }

        public override CompilerResult VisitComp_var(RubyParser.Comp_varContext context)
        {
            return base.VisitComp_var(context);
        }

        public override CompilerResult VisitLvalue(RubyParser.LvalueContext context)
        {
            return base.VisitLvalue(context);
        }

        public override CompilerResult VisitRvalue(RubyParser.RvalueContext context)
        {
            return base.VisitRvalue(context);
        }

        public override CompilerResult VisitBreak_expression(RubyParser.Break_expressionContext context)
        {
            return base.VisitBreak_expression(context);
        }

        public override CompilerResult VisitLiteral_t(RubyParser.Literal_tContext context)
        {
            return base.VisitLiteral_t(context);
        }

        public override CompilerResult VisitFloat_t(RubyParser.Float_tContext context)
        {
            return base.VisitFloat_t(context);
        }

        public override CompilerResult VisitInt_t(RubyParser.Int_tContext context)
        {
            return base.VisitInt_t(context);
        }

        public override CompilerResult VisitBool_t(RubyParser.Bool_tContext context)
        {
            return base.VisitBool_t(context);
        }

        public override CompilerResult VisitNil_t(RubyParser.Nil_tContext context)
        {
            return base.VisitNil_t(context);
        }

        public override CompilerResult VisitId(RubyParser.IdContext context)
        {
            return base.VisitId(context);
        }

        public override CompilerResult VisitId_global(RubyParser.Id_globalContext context)
        {
            return base.VisitId_global(context);
        }

        public override CompilerResult VisitId_function(RubyParser.Id_functionContext context)
        {
            return base.VisitId_function(context);
        }

        public override CompilerResult VisitTerminator(RubyParser.TerminatorContext context)
        {
            return base.VisitTerminator(context);
        }

        public override CompilerResult VisitElse_token(RubyParser.Else_tokenContext context)
        {
            return base.VisitElse_token(context);
        }

        public override CompilerResult VisitCrlf(RubyParser.CrlfContext context)
        {
            return base.VisitCrlf(context);
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