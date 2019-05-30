using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using rubyAntlrCompiler;

namespace RubyCompiler.rubyAntlrCompiler
{
    public class RubyCompilerListener : RubyBaseListener
    {

        ParseTreeProperty<int> IntValues = new ParseTreeProperty<int>();
        ParseTreeProperty<float> FloatValues = new ParseTreeProperty<float>();
        ParseTreeProperty<string> StringValues = new ParseTreeProperty<string>();
        ParseTreeProperty<string> WhichValues = new ParseTreeProperty<string>();
        
        Stack<MemoryStream> StackOutputStreams = new Stack<MemoryStream>();
        Hashtable FunctionDefinitionStreams = new Hashtable();
        MemoryStream MainStream = new MemoryStream();
        MemoryStream FunctionStream = new MemoryStream();
        MemoryStream ErrorStream = new MemoryStream();
        StreamWriter PrintStreamError = null;

        private int SemanticErrorsNum = 0;
        private int NumStr = 1;
        private int NumReg = 0;
        private int NumLabel = 0;
        Stack<int> StackLoopLabels = new Stack<int>();
        LinkedList<string> MainDefenitions = new LinkedList<string>();
        ArrayList<string> FunctionCalls = new ArrayList<string>();
        Stack<LinkedList<string>> StackDefinitions = new Stack<LinkedList<string>>();

        public bool IsDefined(LinkedList<string> definitions, string variable)
        {
            return definitions.Any(def => def.Equals(variable));
        }

        public string repeat(string s, int times)
        {
            if (times <= 0)
                return "";
            else
                return s + repeat(s, times - 1);
        }
        
        
        public RubyCompilerListener(string bytecodePath)
        {
            this.BytecodePath = bytecodePath;
            this.PrintStreamError = new StreamWriter(ErrorStream);
        }

        public string BytecodePath { get; set; }

        public override void EnterProg(RubyParser.ProgContext context)
        {
            MemoryStream outStream = MainStream; 
            var ps = new StreamWriter(outStream);
            ps.WriteLine(".sub main");
            StackDefinitions.Push(MainDefenitions);
            StackOutputStreams.Push(outStream);
        }

        public override void ExitProg(RubyParser.ProgContext context)
        {
            var outStream = MainStream;
            var ps = new StreamWriter(outStream);
            
            ps.WriteLine("\n.end");
            ps.WriteLine("\n.include stdlib.pir");

            foreach (var functionCall in FunctionCalls)
            {
                MemoryStream funcStream = FunctionDefinitionStreams[functionCall] as MemoryStream;
                funcStream?.CopyTo(outStream);
            }

            StackDefinitions.Pop();
            StackOutputStreams.Push(outStream);
        }

        public override void EnterExpression_list(RubyParser.Expression_listContext context)
        {
            base.EnterExpression_list(context);
        }

        public override void ExitExpression_list(RubyParser.Expression_listContext context)
        {
            base.ExitExpression_list(context);
        }

        public override void EnterExpression(RubyParser.ExpressionContext context)
        {
            base.EnterExpression(context);
        }

        public override void ExitExpression(RubyParser.ExpressionContext context)
        {
            base.ExitExpression(context);
        }

        public override void EnterGlobal_get(RubyParser.Global_getContext context)
        {
            base.EnterGlobal_get(context);
        }

        public override void ExitGlobal_get(RubyParser.Global_getContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var defenitions = StackDefinitions.Pop();
            var printStream = new StreamWriter(outStream);

            var variable = context.var_name.GetText();
            var global = context.global_name.GetText();

            if (!IsDefined(defenitions, variable))
            {
                printStream.WriteLine("");
                printStream.WriteLine(".local pmc " + variable);
                printStream.WriteLine(variable + " = new \"Integer\"");
                defenitions.AddLast(variable);
            }

            printStream.WriteLine("get_global " + variable + ", \"" + global + "\"");
            
            StackOutputStreams.Push(outStream);
            StackDefinitions.Push(defenitions);
        }

        public override void EnterGlobal_set(RubyParser.Global_setContext context)
        {
            base.EnterGlobal_set(context);
        }

        public override void ExitGlobal_set(RubyParser.Global_setContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var definitions = StackDefinitions.Pop();
            var printStream = new StreamWriter(outStream);

            var global = context.global_name.GetText();

            var typeArg = WhichValues.Get(context.GetChild(2));

            switch(typeArg) 
            {
                case "Integer":
                    var resultInt = IntValues.Get(context.GetChild(2));
                    printStream.WriteLine("set_global \"" + global + "\", " + resultInt);
                    break;
                case "Float":
                    var resultFloat = FloatValues.Get(context.GetChild(2));
                    printStream.WriteLine("set_global \"" + global + "\", " + resultFloat);
                    break;
                case "String":
                    var resultString = StringValues.Get(context.GetChild(2));
                    printStream.WriteLine("set_global \"" + global + "\", " + resultString);
                    break;
                case "Dynamic":
                    var resultDynamic = StringValues.Get(context.GetChild(2));
                    printStream.WriteLine("set_global \"" + global + "\", " + resultDynamic);
                    break;
            }

            StackOutputStreams.Push(outStream);
            StackDefinitions.Push(definitions);
        }

        public override void EnterGlobal_result(RubyParser.Global_resultContext context)
        {
            base.EnterGlobal_result(context);
        }

        public override void ExitGlobal_result(RubyParser.Global_resultContext context)
        {
            base.ExitGlobal_result(context);
        }

        public override void EnterFunction_inline_call(RubyParser.Function_inline_callContext context)
        {
            base.EnterFunction_inline_call(context);
        }

        public override void ExitFunction_inline_call(RubyParser.Function_inline_callContext context)
        {
            base.ExitFunction_inline_call(context);
        }

        public override void EnterRequire_block(RubyParser.Require_blockContext context)
        {
            base.EnterRequire_block(context);
        }

        public override void ExitRequire_block(RubyParser.Require_blockContext context)
        {
            base.ExitRequire_block(context);
        }

        public override void EnterPir_inline(RubyParser.Pir_inlineContext context)
        {
            base.EnterPir_inline(context);
        }

        public override void ExitPir_inline(RubyParser.Pir_inlineContext context)
        {
            base.ExitPir_inline(context);
        }

        public override void EnterPir_expression_list(RubyParser.Pir_expression_listContext context)
        {
            base.EnterPir_expression_list(context);
        }

        public override void ExitPir_expression_list(RubyParser.Pir_expression_listContext context)
        {
            base.ExitPir_expression_list(context);
        }

        public override void EnterFunction_definition(RubyParser.Function_definitionContext context)
        {
            base.EnterFunction_definition(context);
        }

        public override void ExitFunction_definition(RubyParser.Function_definitionContext context)
        {
            base.ExitFunction_definition(context);
        }

        public override void EnterFunction_definition_body(RubyParser.Function_definition_bodyContext context)
        {
            base.EnterFunction_definition_body(context);
        }

        public override void ExitFunction_definition_body(RubyParser.Function_definition_bodyContext context)
        {
            base.ExitFunction_definition_body(context);
        }

        public override void EnterFunction_definition_header(RubyParser.Function_definition_headerContext context)
        {
            base.EnterFunction_definition_header(context);
        }

        public override void ExitFunction_definition_header(RubyParser.Function_definition_headerContext context)
        {
            base.ExitFunction_definition_header(context);
        }

        public override void EnterFunction_name(RubyParser.Function_nameContext context)
        {
            base.EnterFunction_name(context);
        }

        public override void ExitFunction_name(RubyParser.Function_nameContext context)
        {
            base.ExitFunction_name(context);
        }

        public override void EnterFunction_definition_params(RubyParser.Function_definition_paramsContext context)
        {
            base.EnterFunction_definition_params(context);
        }

        public override void ExitFunction_definition_params(RubyParser.Function_definition_paramsContext context)
        {
            base.ExitFunction_definition_params(context);
        }

        public override void EnterFunction_definition_params_list(RubyParser.Function_definition_params_listContext context)
        {
            base.EnterFunction_definition_params_list(context);
        }

        public override void ExitFunction_definition_params_list(RubyParser.Function_definition_params_listContext context)
        {
            base.ExitFunction_definition_params_list(context);
        }

        public override void EnterFunction_definition_param_id(RubyParser.Function_definition_param_idContext context)
        {
            base.EnterFunction_definition_param_id(context);
        }

        public override void ExitFunction_definition_param_id(RubyParser.Function_definition_param_idContext context)
        {
            base.ExitFunction_definition_param_id(context);
        }

        public override void EnterReturn_statement(RubyParser.Return_statementContext context)
        {
            base.EnterReturn_statement(context);
        }

        public override void ExitReturn_statement(RubyParser.Return_statementContext context)
        {
            base.ExitReturn_statement(context);
        }

        public override void EnterFunction_call(RubyParser.Function_callContext context)
        {
            base.EnterFunction_call(context);
        }

        public override void ExitFunction_call(RubyParser.Function_callContext context)
        {
            base.ExitFunction_call(context);
        }

        public override void EnterFunction_call_param_list(RubyParser.Function_call_param_listContext context)
        {
            base.EnterFunction_call_param_list(context);
        }

        public override void ExitFunction_call_param_list(RubyParser.Function_call_param_listContext context)
        {
            base.ExitFunction_call_param_list(context);
        }

        public override void EnterFunction_call_params(RubyParser.Function_call_paramsContext context)
        {
            base.EnterFunction_call_params(context);
        }

        public override void ExitFunction_call_params(RubyParser.Function_call_paramsContext context)
        {
            base.ExitFunction_call_params(context);
        }

        public override void EnterFunction_param(RubyParser.Function_paramContext context)
        {
            base.EnterFunction_param(context);
        }

        public override void ExitFunction_param(RubyParser.Function_paramContext context)
        {
            base.ExitFunction_param(context);
        }

        public override void EnterFunction_unnamed_param(RubyParser.Function_unnamed_paramContext context)
        {
            base.EnterFunction_unnamed_param(context);
        }

        public override void ExitFunction_unnamed_param(RubyParser.Function_unnamed_paramContext context)
        {
            base.ExitFunction_unnamed_param(context);
        }

        public override void EnterFunction_named_param(RubyParser.Function_named_paramContext context)
        {
            base.EnterFunction_named_param(context);
        }

        public override void ExitFunction_named_param(RubyParser.Function_named_paramContext context)
        {
            base.ExitFunction_named_param(context);
        }

        public override void EnterFunction_call_assignment(RubyParser.Function_call_assignmentContext context)
        {
            base.EnterFunction_call_assignment(context);
        }

        public override void ExitFunction_call_assignment(RubyParser.Function_call_assignmentContext context)
        {
            base.ExitFunction_call_assignment(context);
        }

        public override void EnterAll_result(RubyParser.All_resultContext context)
        {
            base.EnterAll_result(context);
        }

        public override void ExitAll_result(RubyParser.All_resultContext context)
        {
            base.ExitAll_result(context);
        }

        public override void EnterElsif_statement(RubyParser.Elsif_statementContext context)
        {
            base.EnterElsif_statement(context);
        }

        public override void ExitElsif_statement(RubyParser.Elsif_statementContext context)
        {
            base.ExitElsif_statement(context);
        }

        public override void EnterIf_elsif_statement(RubyParser.If_elsif_statementContext context)
        {
            base.EnterIf_elsif_statement(context);
        }

        public override void ExitIf_elsif_statement(RubyParser.If_elsif_statementContext context)
        {
            base.ExitIf_elsif_statement(context);
        }

        public override void EnterIf_statement(RubyParser.If_statementContext context)
        {
            base.EnterIf_statement(context);
        }

        public override void ExitIf_statement(RubyParser.If_statementContext context)
        {
            base.ExitIf_statement(context);
        }

        public override void EnterUnless_statement(RubyParser.Unless_statementContext context)
        {
            base.EnterUnless_statement(context);
        }

        public override void ExitUnless_statement(RubyParser.Unless_statementContext context)
        {
            base.ExitUnless_statement(context);
        }

        public override void EnterWhile_statement(RubyParser.While_statementContext context)
        {
            base.EnterWhile_statement(context);
        }

        public override void ExitWhile_statement(RubyParser.While_statementContext context)
        {
            base.ExitWhile_statement(context);
        }

        public override void EnterFor_statement(RubyParser.For_statementContext context)
        {
            base.EnterFor_statement(context);
        }

        public override void ExitFor_statement(RubyParser.For_statementContext context)
        {
            base.ExitFor_statement(context);
        }

        public override void EnterInit_expression(RubyParser.Init_expressionContext context)
        {
            base.EnterInit_expression(context);
        }

        public override void ExitInit_expression(RubyParser.Init_expressionContext context)
        {
            base.ExitInit_expression(context);
        }

        public override void EnterAll_assignment(RubyParser.All_assignmentContext context)
        {
            base.EnterAll_assignment(context);
        }

        public override void ExitAll_assignment(RubyParser.All_assignmentContext context)
        {
            base.ExitAll_assignment(context);
        }

        public override void EnterFor_init_list(RubyParser.For_init_listContext context)
        {
            base.EnterFor_init_list(context);
        }

        public override void ExitFor_init_list(RubyParser.For_init_listContext context)
        {
            base.ExitFor_init_list(context);
        }

        public override void EnterCond_expression(RubyParser.Cond_expressionContext context)
        {
            base.EnterCond_expression(context);
        }

        public override void ExitCond_expression(RubyParser.Cond_expressionContext context)
        {
            base.ExitCond_expression(context);
        }

        public override void EnterLoop_expression(RubyParser.Loop_expressionContext context)
        {
            base.EnterLoop_expression(context);
        }

        public override void ExitLoop_expression(RubyParser.Loop_expressionContext context)
        {
            base.ExitLoop_expression(context);
        }

        public override void EnterFor_loop_list(RubyParser.For_loop_listContext context)
        {
            base.EnterFor_loop_list(context);
        }

        public override void ExitFor_loop_list(RubyParser.For_loop_listContext context)
        {
            base.ExitFor_loop_list(context);
        }

        public override void EnterStatement_body(RubyParser.Statement_bodyContext context)
        {
            base.EnterStatement_body(context);
        }

        public override void ExitStatement_body(RubyParser.Statement_bodyContext context)
        {
            base.ExitStatement_body(context);
        }

        public override void EnterStatement_expression_list(RubyParser.Statement_expression_listContext context)
        {
            base.EnterStatement_expression_list(context);
        }

        public override void ExitStatement_expression_list(RubyParser.Statement_expression_listContext context)
        {
            base.ExitStatement_expression_list(context);
        }

        public override void EnterAssignment(RubyParser.AssignmentContext context)
        {
            base.EnterAssignment(context);
        }

        public override void ExitAssignment(RubyParser.AssignmentContext context)
        {
            base.ExitAssignment(context);
        }

        public override void EnterDynamic_assignment(RubyParser.Dynamic_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);
            var definitions = StackDefinitions.Pop();

            string variable;
            switch(context.op.Type)
            {
                case RubyParser.ASSIGN:
                    variable = context.var_id.GetText();
                    if (!IsDefined(definitions, variable)) 
                    {
                        ps.WriteLine("");
                        ps.WriteLine(".local pmc " + context.var_id.GetText());
                        ps.WriteLine(context.var_id.GetText() + "= new \"Integer\"");
                        definitions.AddLast(context.var_id.GetText());
                        NumReg = 0;
                    }
                    break;
                default:
                    variable = context.var_id.GetText();
                    if (!IsDefined(definitions, variable)) 
                    {
                        PrintStreamError.WriteLine("line " + NumStr + " Error! Undefined variable " + variable + "!");
                        SemanticErrorsNum++;
                    }
                    break;
            }

            StackOutputStreams.Push(outStream); 
            StackDefinitions.Push(definitions);
        }

        public override void ExitDynamic_assignment(RubyParser.Dynamic_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);

            var str = context.var_id.GetText() + " " + context.op.Text + " " + StringValues.Get(context.GetChild(2));
            NumReg = 0;
            ps.WriteLine(str);

            StackOutputStreams.Push(outStream);
        }

        public override void EnterInt_assignment(RubyParser.Int_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);
            var definitions = StackDefinitions.Pop();

            string variable;
            switch(context.op.Type)
            {
                case RubyParser.ASSIGN:
                    variable = context.var_id.GetText();
                    if (!IsDefined(definitions, variable)) 
                    {
                        ps.WriteLine("");
                        ps.WriteLine(".local pmc " + context.var_id.GetText());
                        ps.WriteLine(context.var_id.GetText() + "= new \"Integer\"");
                        definitions.AddLast(context.var_id.GetText());                        
                    }
                    break;
                default:
                    variable = context.var_id.GetText();
                    if (!IsDefined(definitions, variable)) 
                    {
                        PrintStreamError.WriteLine("line " + NumStr + " Error! Undefined variable " + variable + "!");
                        SemanticErrorsNum++;
                    }
                    break;
            }

            StackOutputStreams.Push(outStream); 
            StackDefinitions.Push(definitions);
        }

        public override void ExitInt_assignment(RubyParser.Int_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);

            var str = context.var_id.GetText() + " " + context.op.Text + " " + IntValues.Get(context.GetChild(2));
            ps.WriteLine(str);

            StackOutputStreams.Push(outStream);
        }

        public override void EnterFloat_assignment(RubyParser.Float_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);
            var definitions = StackDefinitions.Pop();

            string variable;
            switch(context.op.Type)
            {
                case RubyParser.ASSIGN:
                    variable = context.var_id.GetText();
                    if (!IsDefined(definitions, variable))
                    {
                        ps.WriteLine("");
                        ps.WriteLine(".local pmc " + context.var_id.GetText());
                        ps.WriteLine(context.var_id.GetText() + "= new \"Double\"");
                        definitions.AddLast(context.var_id.GetText());
                    }
                    break;
                default:
                    variable = context.var_id.GetText();
                    if (!IsDefined(definitions, variable))
                    {
                        PrintStreamError.WriteLine("line " + NumStr + " Error! Undefined variable " + variable + "!");
                        SemanticErrorsNum++;
                    }
                    break;
            }

            StackOutputStreams.Push(outStream);
            StackDefinitions.Push(definitions);
        }

        public override void ExitFloat_assignment(RubyParser.Float_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);

            var str = context.var_id.GetText() + " " + context.op.Text + " " + FloatValues.Get(context.GetChild(2));
            ps.WriteLine(str);

            StackOutputStreams.Push(outStream);
        }

        public override void EnterString_assignment(RubyParser.String_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);
            var definitions = StackDefinitions.Pop();

            string variable;
            switch(context.op.Type) 
            {
                case RubyParser.ASSIGN:
                    variable = context.var_id.GetText();
                    if (!IsDefined(definitions, variable))
                    {
                        ps.WriteLine("");
                        ps.WriteLine(".local pmc " + context.var_id.GetText());
                        ps.WriteLine(context.var_id.GetText() + "= new \"String\"");
                        definitions.AddLast(context.var_id.GetText());
                    }
                    break;
                default:
                    variable = context.var_id.GetText();
                    if (!IsDefined(definitions, variable))
                    {
                        PrintStreamError.WriteLine("line " + NumStr + " Error! Undefined variable " + variable + "!");
                        SemanticErrorsNum++;
                    }
                    break;
            }

            StackOutputStreams.Push(outStream);
            StackDefinitions.Push(definitions);
        }

        public override void ExitString_assignment(RubyParser.String_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);

            var str = context.var_id.GetText() + " " + context.op.Text + " \"" + StringValues.Get(context.GetChild(2)) + "\"";
            ps.WriteLine(str);

            StackOutputStreams.Push(outStream);
        }

        public override void EnterInitial_array_assignment(RubyParser.Initial_array_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);
            var definitions = StackDefinitions.Pop();

            var variable = context.var_id.GetText();

            if (!IsDefined(definitions, variable)) {
                ps.WriteLine("");
                ps.WriteLine(".local pmc " + variable);
                definitions.AddLast(variable);
            }
            ps.WriteLine(variable + " = new \"ResizablePMCArray\"");

            StackOutputStreams.Push(outStream);
            StackDefinitions.Push(definitions);
        }

        public override void ExitInitial_array_assignment(RubyParser.Initial_array_assignmentContext context)
        {
            base.ExitInitial_array_assignment(context);
        }

        public override void EnterArray_assignment(RubyParser.Array_assignmentContext context)
        {
            base.EnterArray_assignment(context);
        }

        public override void ExitArray_assignment(RubyParser.Array_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);
            var definitions = StackDefinitions.Pop();
            var arrDef = StringValues.Get(context.GetChild(0));

            var typeArg = WhichValues.Get(context.GetChild(2));

            switch(typeArg)
            {
                case "Integer":
                    var resultInt = IntValues.Get(context.GetChild(2));
                    ps.WriteLine(arrDef + " = " + resultInt);
                    break;
                case "Float":
                    var resultFloat = FloatValues.Get(context.GetChild(2));
                    ps.WriteLine(arrDef + " = " + resultFloat);
                    break;
                case "String":
                    var resultString = StringValues.Get(context.GetChild(2));
                    ps.WriteLine(arrDef + " = " + resultString);
                    break;
                case "Dynamic":
                    var resultDynamic = StringValues.Get(context.GetChild(2));
                    ps.WriteLine(arrDef + " = " + resultDynamic);
                    break;
            }

            StackOutputStreams.Push(outStream);
            StackDefinitions.Push(definitions);
        }

        public override void EnterArray_definition(RubyParser.Array_definitionContext context)
        {
            base.EnterArray_definition(context);
        }

        public override void ExitArray_definition(RubyParser.Array_definitionContext context)
        {
            base.ExitArray_definition(context);
        }

        public override void EnterArray_definition_elements(RubyParser.Array_definition_elementsContext context)
        {
            base.EnterArray_definition_elements(context);
        }

        public override void ExitArray_definition_elements(RubyParser.Array_definition_elementsContext context)
        {
            base.ExitArray_definition_elements(context);
        }

        public override void EnterArray_selector(RubyParser.Array_selectorContext context)
        {
            base.EnterArray_selector(context);
        }

        public override void ExitArray_selector(RubyParser.Array_selectorContext context)
        {
            var name = StringValues.Get(context.GetChild(0));
            var typeArg = WhichValues.Get(context.GetChild(2));

            switch(typeArg)
            {
                case "Integer":
                    var selectorInt = IntValues.Get(context.GetChild(2));
                    StringValues.Put(context, name + "[" + selectorInt + "]");
                    break;
                case "Dynamic":
                    var selectorStr = StringValues.Get(context.GetChild(2));
                    StringValues.Put(context, name + "[" + selectorStr + "]");
                    break;
            }

            WhichValues.Put(context, "Dynamic");
        }

        public override void EnterDynamic_result(RubyParser.Dynamic_resultContext context)
        {
            base.EnterDynamic_result(context);
        }

        public override void ExitDynamic_result(RubyParser.Dynamic_resultContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);

            if ( context.ChildCount == 3 && context.op != null ) 
            { 

                int intDyn = 0;
                float floatDyn = 0;
                var strDyn = "";
                var strDyn1 = "";
                var anotherNode = "";
                var strOutput = "";

                switch(WhichValues.Get(context.GetChild(0)))
                {
                    case "Integer":
                        intDyn = IntValues.Get(context.GetChild(0));
                        anotherNode = StringValues.Get(context.GetChild(2));
                        strOutput = "$P" + NumReg + " = " + anotherNode + " " + context.op.Text + " " + intDyn;                  
                        break;
                    case "Float":
                        floatDyn = FloatValues.Get(context.GetChild(0));
                        anotherNode = StringValues.Get(context.GetChild(2));
                        strOutput = "$P" + NumReg + " = " + anotherNode + " " + context.op.Text + " " + floatDyn;
                        break;
                    case "String":
                        strDyn = StringValues.Get(context.GetChild(0));
                        anotherNode = StringValues.Get(context.GetChild(2));
                        strOutput = "$P" + NumReg + " = " + anotherNode + " " + context.op.Text + " " + strDyn;
                        break;
                    case "Dynamic":
                        strDyn1 = StringValues.Get(context.GetChild(0));
                        switch(WhichValues.Get(context.GetChild(2)))
                        {
                            case "Integer":
                                intDyn = IntValues.Get(context.GetChild(2));
                                strOutput = "$P" + NumReg + " = " + strDyn1 + " " + context.op.Text + " " + intDyn;                  
                                break;
                            case "Float":
                                floatDyn = FloatValues.Get(context.GetChild(2));
                                strOutput = "$P" + NumReg + " = " + strDyn1 + " " + context.op.Text + " " + floatDyn;
                                break;
                            case "String":
                                strDyn = StringValues.Get(context.GetChild(2));
                                strOutput = "$P" + NumReg + " = " + strDyn1 + " " + context.op.Text + " " + strDyn;
                                break;
                            case "Dynamic":
                                strDyn = StringValues.Get(context.GetChild(2));
                                strOutput = "$P" + NumReg + " = " + strDyn1 + " " + context.op.Text + " " + strDyn;
                                break;
                        }
                        break;
                }

                ps.WriteLine("$P" + NumReg + " = new \"Integer\"");
                ps.WriteLine(strOutput);
                StringValues.Put(context, "$P" + NumReg);
                WhichValues.Put(context, "Dynamic");
                NumReg++;
            }
            else if ( context.ChildCount == 1 )
            { 
                StringValues.Put(context, StringValues.Get(context.GetChild(0)));
                WhichValues.Put(context, "Dynamic");
            }
            else if ( context.ChildCount == 3 && context.op == null )
            { 
                StringValues.Put(context, StringValues.Get(context.GetChild(1)));
                WhichValues.Put(context, "Dynamic");
            }

            StackOutputStreams.Push(outStream);
        }

        public override void EnterDynamic(RubyParser.DynamicContext context)
        {
            base.EnterDynamic(context);
        }

        public override void ExitDynamic(RubyParser.DynamicContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream);

            var strDynTerm = StringValues.Get(context.GetChild(0));
            ps.WriteLine("$P" + NumReg + " = new \"Integer\"");
            ps.WriteLine("$P" + NumReg + " = " + strDynTerm);
            StringValues.Put(context, "$P" + NumReg);
            NumReg++;
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));

            StackOutputStreams.Push(outStream);
        }

        public override void EnterInt_result(RubyParser.Int_resultContext context)
        {
            base.EnterInt_result(context);
        }

        public override void ExitInt_result(RubyParser.Int_resultContext context)
        {
            if ( context.ChildCount == 3 && context.op != null )
            { 
                var left = IntValues.Get(context.GetChild(0));
                var right = IntValues.Get(context.GetChild(2));

                switch(context.op.Type) 
                {
                    case RubyParser.MUL:
                        IntValues.Put(context, left * right);
                        WhichValues.Put(context, "Integer");
                        break;
                    case RubyParser.DIV:
                        IntValues.Put(context, left / right);
                        WhichValues.Put(context, "Integer");
                        break;
                    case RubyParser.MOD:
                        IntValues.Put(context, left % right);
                        WhichValues.Put(context, "Integer");
                        break;
                    case RubyParser.PLUS:
                        IntValues.Put(context, left + right);
                        WhichValues.Put(context, "Integer");
                        break;
                    case RubyParser.MINUS:
                        IntValues.Put(context, left - right);
                        WhichValues.Put(context, "Integer");
                        break;
                }
            }
            else if ( context.ChildCount == 1 )
            { 
                IntValues.Put(context, IntValues.Get(context.GetChild(0)));
                WhichValues.Put(context,  "Integer");
            }
            else if ( context.ChildCount == 3 && context.op == null ) 
            { 
                IntValues.Put(context, IntValues.Get(context.GetChild(1)));
                WhichValues.Put(context, "Integer");
            }
        }

        public override void EnterFloat_result(RubyParser.Float_resultContext context)
        {
            base.EnterFloat_result(context);
        }

        public override void ExitFloat_result(RubyParser.Float_resultContext context)
        {
            if ( context.ChildCount == 3 && context.op != null )
            {
                float left = 0;
                float right = 0;

                switch(WhichValues.Get(context.GetChild(0))) {
                    case "Integer":
                        left = (float) IntValues.Get(context.GetChild(0));
                        break;
                    case "Float":
                        left = FloatValues.Get(context.GetChild(0));
                        break;
                }

                switch(WhichValues.Get(context.GetChild(2))) {
                    case "Integer":
                        right = (float) IntValues.Get(context.GetChild(2));
                        break;
                    case "Float":
                        right = FloatValues.Get(context.GetChild(2));
                        break;
                }

                switch(context.op.Type) 
                {
                    case RubyParser.MUL:
                        FloatValues.Put(context, left * right);
                        WhichValues.Put(context, "Float");
                        break;
                    case RubyParser.DIV:
                        FloatValues.Put(context, left / right);
                        WhichValues.Put(context, "Float");
                        break;
                    case RubyParser.MOD:
                        FloatValues.Put(context, left % right);
                        WhichValues.Put(context, "Float");
                        break;
                    case RubyParser.PLUS:
                        FloatValues.Put(context, left + right);
                        WhichValues.Put(context, "Float");
                        break;
                    case RubyParser.MINUS:
                        FloatValues.Put(context, left - right);
                        WhichValues.Put(context, "Float");
                        break;
                }
            }
            else if ( context.ChildCount == 1 ) 
            { 
                FloatValues.Put(context, FloatValues.Get(context.GetChild(0)));
                WhichValues.Put(context, "Float");
            }
            else if ( context.ChildCount == 3 && context.op == null )
            { 
                FloatValues.Put(context, FloatValues.Get(context.GetChild(1)));
                WhichValues.Put(context, "Float");
            }
        }

        public override void EnterString_result(RubyParser.String_resultContext context)
        {
            base.EnterString_result(context);
        }

        public override void ExitString_result(RubyParser.String_resultContext context)
        {
            if ( context.ChildCount == 3 && context.op != null )
            { 

                int times = 0;
                var leftS = "";
                var rightS = "";
                var str = "";

                switch(WhichValues.Get(context.GetChild(0))) 
                {
                    case "Integer":
                        times = IntValues.Get(context.GetChild(0));
                        break;
                    case "String":
                        leftS = StringValues.Get(context.GetChild(0));
                        str = leftS;
                        break;
                }

                switch(WhichValues.Get(context.GetChild(2))) 
                {
                    case "Integer":
                        times = IntValues.Get(context.GetChild(2));
                        break;
                    case "String":
                        rightS = StringValues.Get(context.GetChild(2));
                        str = rightS;
                        break;
                }

                switch(context.op.Type)
                {
                    case RubyParser.MUL:
                        StringValues.Put(context,repeat(str, times));
                        WhichValues.Put(context, "String");
                        break;
                    case RubyParser.PLUS:
                        StringValues.Put(context,  leftS + rightS);
                        WhichValues.Put(context, "String");
                        break;
                }
            }
            else if ( context.ChildCount == 1 )
            { 
                StringValues.Put(context, StringValues.Get(context.GetChild(0)));
                WhichValues.Put(context, "String");
            }
            else if ( context.ChildCount == 3 && context.op == null )
            { 
                StringValues.Put(context, StringValues.Get(context.GetChild(1)));
                WhichValues.Put(context, "String");
            }
        }

        public override void EnterComparison_list(RubyParser.Comparison_listContext context)
        {
            base.EnterComparison_list(context);
        }

        public override void ExitComparison_list(RubyParser.Comparison_listContext context)
        {
            base.ExitComparison_list(context);
        }

        public override void EnterComparison(RubyParser.ComparisonContext context)
        {
            base.EnterComparison(context);
        }

        public override void ExitComparison(RubyParser.ComparisonContext context)
        {
            base.ExitComparison(context);
        }

        public override void EnterComp_var(RubyParser.Comp_varContext context)
        {
            base.EnterComp_var(context);
        }

        public override void ExitComp_var(RubyParser.Comp_varContext context)
        {
            base.ExitComp_var(context);
        }

        public override void EnterLvalue(RubyParser.LvalueContext context)
        {
            base.EnterLvalue(context);
        }

        public override void ExitLvalue(RubyParser.LvalueContext context)
        {
            base.ExitLvalue(context);
        }

        public override void EnterRvalue(RubyParser.RvalueContext context)
        {
            base.EnterRvalue(context);
        }

        public override void ExitRvalue(RubyParser.RvalueContext context)
        {
            base.ExitRvalue(context);
        }

        public override void EnterBreak_expression(RubyParser.Break_expressionContext context)
        {
            base.EnterBreak_expression(context);
        }

        public override void ExitBreak_expression(RubyParser.Break_expressionContext context)
        {
            base.ExitBreak_expression(context);
        }

        public override void EnterLiteral_t(RubyParser.Literal_tContext context)
        {
            base.EnterLiteral_t(context);
        }

        public override void ExitLiteral_t(RubyParser.Literal_tContext context)
        {
            StringValues.Put(context, StringValues.Get(context.GetChild(0)));
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));
        }

        public override void EnterFloat_t(RubyParser.Float_tContext context)
        {
            base.EnterFloat_t(context);
        }

        public override void ExitFloat_t(RubyParser.Float_tContext context)
        {
            FloatValues.Put(context, FloatValues.Get(context.GetChild(0)));
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));
        }

        public override void EnterInt_t(RubyParser.Int_tContext context)
        {
            base.EnterInt_t(context);
        }

        public override void ExitInt_t(RubyParser.Int_tContext context)
        {
            IntValues.Put(context, IntValues.Get(context.GetChild(0)));
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));
        }

        public override void EnterBool_t(RubyParser.Bool_tContext context)
        {
            base.EnterBool_t(context);
        }

        public override void ExitBool_t(RubyParser.Bool_tContext context)
        {
            base.ExitBool_t(context);
        }

        public override void EnterNil_t(RubyParser.Nil_tContext context)
        {
            base.EnterNil_t(context);
        }

        public override void ExitNil_t(RubyParser.Nil_tContext context)
        {
            base.ExitNil_t(context);
        }

        public override void EnterId(RubyParser.IdContext context)
        {
            base.EnterId(context);
        }

        public override void ExitId(RubyParser.IdContext context)
        {
            StringValues.Put(context, StringValues.Get(context.GetChild(0)));
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));
        }

        public override void EnterId_global(RubyParser.Id_globalContext context)
        {
            base.EnterId_global(context);
        }

        public override void ExitId_global(RubyParser.Id_globalContext context)
        {
            base.ExitId_global(context);
        }

        public override void EnterId_function(RubyParser.Id_functionContext context)
        {
            base.EnterId_function(context);
        }

        public override void ExitId_function(RubyParser.Id_functionContext context)
        {
            base.ExitId_function(context);
        }

        public override void EnterTerminator(RubyParser.TerminatorContext context)
        {
            base.EnterTerminator(context);
        }

        public override void ExitTerminator(RubyParser.TerminatorContext context)
        {
            base.ExitTerminator(context);
        }

        public override void EnterElse_token(RubyParser.Else_tokenContext context)
        {
            base.EnterElse_token(context);
        }

        public override void ExitElse_token(RubyParser.Else_tokenContext context)
        {
            base.ExitElse_token(context);
        }

        public override void EnterCrlf(RubyParser.CrlfContext context)
        {
            base.EnterCrlf(context);
        }

        public override void ExitCrlf(RubyParser.CrlfContext context)
        {
            base.ExitCrlf(context);
        }

        public override void EnterEveryRule(ParserRuleContext context)
        {
            base.EnterEveryRule(context);
        }

        public override void ExitEveryRule(ParserRuleContext context)
        {
            base.ExitEveryRule(context);
        }

        public override void VisitTerminal(ITerminalNode node)
        {
            base.VisitTerminal(node);
        }

        public override void VisitErrorNode(IErrorNode node)
        {
            base.VisitErrorNode(node);
        }
    }
}