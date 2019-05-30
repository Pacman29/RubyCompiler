using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

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
        private int NumRegInt = 0;
        private int NumReg = 0;
        private int NumLabel = 0;
        Stack<int> StackLoopLabels = new Stack<int>();
        LinkedList<string> MainDefenitions = new LinkedList<string>();
        ArrayList FunctionCalls = new ArrayList();
        Stack<LinkedList<string>> StackDefinitions = new Stack<LinkedList<string>>();

        private bool IsDefined(LinkedList<string> definitions, string variable)
        {
            return definitions.Any(def => def.Equals(variable));
        }

        private string Repeat(string s, int times)
        {
            if (times <= 0)
                return "";
            else
                return s + Repeat(s, times - 1);
        }
        
        
        public RubyCompilerListener()
        {
            PrintStreamError = new StreamWriter(ErrorStream) {AutoFlush = true};
            CultureInfo = (CultureInfo) CultureInfo.CurrentCulture.Clone();
            CultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
            CultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        }

        public CultureInfo CultureInfo { get; set; }

        public void CreateIRFile(string path)
        {
            ErrorStream.Seek(0, SeekOrigin.Begin);
            var errorReader = new StreamReader(ErrorStream);
            Console.WriteLine(errorReader.ReadToEnd());
            
            var outStream = new StreamWriter(path) {AutoFlush = true};
            var codeStream = StackOutputStreams.Pop();
            codeStream.Seek(0, SeekOrigin.Begin);
            var irCode = new StreamReader(codeStream).ReadToEnd();
            Console.WriteLine(irCode);
            outStream.Write(irCode);
        }

        public override void EnterProg(RubyParser.ProgContext context)
        {
            MemoryStream outStream = MainStream;
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            ps.WriteLine(".sub main");
            StackDefinitions.Push(MainDefenitions);
            StackOutputStreams.Push(outStream);
        }

        public override void ExitProg(RubyParser.ProgContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            
            ps.WriteLine("\n.end");
            ps.WriteLine("\n.include \"stdlib/stdlib.pir\"");

            foreach (var functionCall in FunctionCalls)
            {
                MemoryStream funcStream = FunctionDefinitionStreams[functionCall] as MemoryStream;
                if (funcStream != null)
                {
                    funcStream.Seek(0, SeekOrigin.Begin);
                    ps.WriteLine(new StreamReader(funcStream).ReadToEnd());
                }
                    
            }

            StackDefinitions.Pop();
            StackOutputStreams.Push(outStream);
        }

        public override void ExitGlobal_get(RubyParser.Global_getContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var defenitions = StackDefinitions.Pop();
            var printStream = new StreamWriter(outStream) {AutoFlush = true};

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

        public override void ExitGlobal_set(RubyParser.Global_setContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var definitions = StackDefinitions.Pop();
            var printStream = new StreamWriter(outStream) {AutoFlush = true};

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
                    printStream.WriteLine("set_global \"" + global + "\", " + resultFloat.ToString(CultureInfo));
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

        public override void ExitFunction_inline_call(RubyParser.Function_inline_callContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            ps.WriteLine(StringValues.Get(context.GetChild(0)));
            StackOutputStreams.Push(outStream);
        }

        public override void EnterPir_expression_list(RubyParser.Pir_expression_listContext context)
        {
            var emptyStream = new MemoryStream();
            StackOutputStreams.Push(emptyStream);
        }

        public override void ExitPir_expression_list(RubyParser.Pir_expression_listContext context)
        {
            var emptyStream = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            ps.WriteLine(context.GetText());

            StackOutputStreams.Push(outStream);
        }

        public override void EnterFunction_definition(RubyParser.Function_definitionContext context)
        {
            var funcDefinitions = new LinkedList<string>();
            StackDefinitions.Push(funcDefinitions);
            var funcParams = new MemoryStream();
            StackOutputStreams.Push(funcParams);
            var outStream = new MemoryStream();
            StackOutputStreams.Push(outStream);
        }

        public override void ExitFunction_definition(RubyParser.Function_definitionContext context)
        {
            var funcBody = StackOutputStreams.Pop();
            var funcParams = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var funcName = StringValues.Get(context.GetChild(0));
            ps.WriteLine("\n.sub " + funcName);
            ps.WriteLine("");
            funcParams.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(funcParams).ReadToEnd());
            funcBody.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(funcBody).ReadToEnd());
            ps.Write(".end");

            FunctionDefinitionStreams[funcName] = outStream;

            StackDefinitions.Pop();
        }

        public override void EnterFunction_definition_body(RubyParser.Function_definition_bodyContext context)
        {
            var funcBody = new MemoryStream();
            StackOutputStreams.Push(funcBody);
        }

        public override void ExitFunction_definition_header(RubyParser.Function_definition_headerContext context)
        {
            StringValues.Put(context, context.GetChild(1).GetText());
        }

        public override void ExitFunction_definition_param_id(RubyParser.Function_definition_param_idContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var paramId = context.GetChild(0).GetText();
            ps.WriteLine(".param pmc " + paramId);
            ps.Flush();
            
            StackOutputStreams.Push(outStream);
        }

        public override void ExitReturn_statement(RubyParser.Return_statementContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var typeArg = WhichValues.Get(context.GetChild(1));

            switch(typeArg) 
            {
                case "Integer":
                    var resultInt = IntValues.Get(context.GetChild(1));
                    ps.WriteLine(".return(" + resultInt + ")");
                    break;
                case "Float":
                    var resultFloat = FloatValues.Get(context.GetChild(1));
                    ps.WriteLine(".return(" + resultFloat.ToString(CultureInfo) + ")");
                    break;
                case "String":
                    var resultString = StringValues.Get(context.GetChild(1));
                    ps.WriteLine(".return(" + resultString + ")");
                    break;
                case "Dynamic":
                    var resultDynamic = StringValues.Get(context.GetChild(1));
                    ps.WriteLine(".return(" + resultDynamic + ")");
                    break;
            }

            StackOutputStreams.Push(outStream);
        }

        public override void EnterFunction_call(RubyParser.Function_callContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var assignmentStream = new MemoryStream();
            var argsStream = new MemoryStream();
            StackOutputStreams.Push(outStream);
            StackOutputStreams.Push(argsStream);
            StackOutputStreams.Push(assignmentStream);
        }

        public override void ExitFunction_call(RubyParser.Function_callContext context)
        {
            var assignmentStream = StackOutputStreams.Pop();
            var argsStream = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            argsStream.Seek(0, SeekOrigin.Begin);
            var args = new StreamReader(argsStream).ReadToEnd();
            var funcName = context.name.GetText();
            args = Regex.Replace(args, ",$", ""); 
            // ASSIGNMENT of dynamic function params
            assignmentStream.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(assignmentStream).ReadToEnd());
            // call of function
            StringValues.Put(context, funcName + "(" + args + ")");

            FunctionCalls.Add(funcName);

            StackOutputStreams.Push(outStream);
        }

        public override void ExitFunction_unnamed_param(RubyParser.Function_unnamed_paramContext context)
        {
            var assignmentStream = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            switch(WhichValues.Get(context.GetChild(0))) 
            {
                case "Integer":
                    var resultInt = IntValues.Get(context.GetChild(0));
                    ps.Write(resultInt + ",");
                    break;
                case "Float":
                    var resultFloat = FloatValues.Get(context.GetChild(0));
                    ps.Write(resultFloat.ToString(CultureInfo) + ",");
                    break;
                case "String":
                    var resultString = StringValues.Get(context.GetChild(0));
                    ps.Write("\"" + resultString + "\",");
                    break;
                case "Dynamic":
                    var resultDynamic = StringValues.Get(context.GetChild(0));
                    ps.Write(resultDynamic + ",");
                    break;
            }

            StackOutputStreams.Push(outStream);
            StackOutputStreams.Push(assignmentStream);
        }

        public override void ExitFunction_named_param(RubyParser.Function_named_paramContext context)
        {
            var assignmentStream = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var idParam = context.GetChild(0).GetText();

            switch(WhichValues.Get(context.GetChild(2)))
            {
                case "Integer":
                    var resultInt = IntValues.Get(context.GetChild(2));
                    ps.Write(resultInt + " :named(\"" + idParam + "\"),");
                    break;
                case "Float":
                    var resultFloat = FloatValues.Get(context.GetChild(2));
                    ps.Write(resultFloat.ToString(CultureInfo) + " :named(\"" + idParam + "\"),");
                    break;
                case "String":
                    var resultString = StringValues.Get(context.GetChild(2));
                    ps.Write("\"" + resultString + "\" :named(\"" + idParam + "\"),");
                    break;
                case "Dynamic":
                    var resultDynamic = StringValues.Get(context.GetChild(2));
                    ps.Write(resultDynamic + " :named(\"" + idParam + "\"),");
                    break;
            }

            StackOutputStreams.Push(outStream);
            StackOutputStreams.Push(assignmentStream);
        }

        public override void EnterFunction_call_assignment(RubyParser.Function_call_assignmentContext context)
        {
            var outStream = new MemoryStream();
            StackOutputStreams.Push(outStream);
        }

        public override void ExitFunction_call_assignment(RubyParser.Function_call_assignmentContext context)
        {
            var func = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            func.Seek(0, SeekOrigin.Begin);
            var funcCall = new StreamReader(func).ReadToEnd();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            ps.Write(funcCall);

            StringValues.Put(context, StringValues.Get(context.GetChild(0)));
            WhichValues.Put(context, "Dynamic");
            StackOutputStreams.Push(outStream);
        }

        public override void ExitAll_result(RubyParser.All_resultContext context)
        {
            var typeArg = WhichValues.Get(context.GetChild(0));
            
            switch(typeArg)
            {
                case "Integer":
                    var resultInt = IntValues.Get(context.GetChild(0));
                    IntValues.Put(context, resultInt);
                    WhichValues.Put(context, typeArg);
                    break;
                case "Float":
                    var resultFloat = FloatValues.Get(context.GetChild(0));
                    FloatValues.Put(context, resultFloat);
                    WhichValues.Put(context, typeArg);
                    break;
                case "String":
                    var resultString = StringValues.Get(context.GetChild(0));
                    StringValues.Put(context, "\"" + resultString + "\"");
                    WhichValues.Put(context, typeArg);
                    break;
                case "Dynamic":
                    var resultDynamic = StringValues.Get(context.GetChild(0));
                    StringValues.Put(context, resultDynamic);
                    WhichValues.Put(context, typeArg);
                    break;
            }
        }

        public override void EnterIf_elsif_statement(RubyParser.If_elsif_statementContext context)
        {
            var elsifStream = new MemoryStream();
            var labelEnd = 0;
            var childCount = context.ChildCount;

            if (childCount > 4)
            {
                labelEnd = StackLoopLabels.Pop();
                StackLoopLabels.Push(labelEnd);     
            }

            StackLoopLabels.Push(++NumLabel);
            StackLoopLabels.Push(++NumLabel);

            if (childCount > 4)
                StackLoopLabels.Push(labelEnd);

            StackOutputStreams.Push(elsifStream);
        }

        public override void ExitIf_elsif_statement(RubyParser.If_elsif_statementContext context)
        {
            var elseBodyStream = new MemoryStream();
            var labelEnd = 0;
            var childCount = context.ChildCount;

            if (childCount > 4) {
                elseBodyStream = StackOutputStreams.Pop();   
                labelEnd = StackLoopLabels.Pop();  
            }

            var bodyStream = StackOutputStreams.Pop();
            var condStream = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var conditionVar = StringValues.Get(context.GetChild(1));

            var labelFalse = StackLoopLabels.Pop();
            var labelTrue = StackLoopLabels.Pop();

            ps.WriteLine("");
            condStream.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(condStream).ReadToEnd());
            ps.WriteLine("if " + conditionVar + " goto label_" + labelTrue);
            ps.WriteLine("goto label_" + labelFalse);
            ps.WriteLine("label_" + labelTrue + ":");

            bodyStream.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(bodyStream).ReadToEnd());

            if (childCount > 4) 
            {
                ps.WriteLine("goto label_" + labelEnd);
                ps.WriteLine("label_" + labelFalse + ":");
                elseBodyStream.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(elseBodyStream).ReadToEnd());
            }
            else 
            {
                ps.WriteLine("label_" + labelFalse + ":");
            }

            StackOutputStreams.Push(outStream);
        }

        public override void EnterIf_statement(RubyParser.If_statementContext context)
        {
            StackLoopLabels.Push(++NumLabel);
            StackLoopLabels.Push(++NumLabel);

            var child = context.GetChild(4).GetText();

            if (child.Equals("else") || child.Equals("elsif"))
                StackLoopLabels.Push(++NumLabel);
        }

        public override void ExitIf_statement(RubyParser.If_statementContext context)
        {
            var elseBodyStream = new MemoryStream();
            var labelEnd = 0;
            var child = context.GetChild(4).GetText();

            if (child.Equals("else") || child.Equals("elsif"))
            {
                elseBodyStream = StackOutputStreams.Pop();     
                labelEnd = StackLoopLabels.Pop();
            }

            var labelFalse = StackLoopLabels.Pop();
            var labelTrue = StackLoopLabels.Pop();

            var bodyStream = StackOutputStreams.Pop();
            var condStream = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var conditionVar = StringValues.Get(context.GetChild(1));

            ps.WriteLine("");
            condStream.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(condStream).ReadToEnd());
            ps.WriteLine("if " + conditionVar + " goto label_" + labelTrue);
            ps.WriteLine("goto label_" + labelFalse);
            ps.WriteLine("label_" + labelTrue + ":");

            bodyStream.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(bodyStream).ReadToEnd());

            if (child.Equals("else") || child.Equals("elsif")) 
            {
                ps.WriteLine("goto label_" + labelEnd);
                ps.WriteLine("label_" + labelFalse + ":");
                elseBodyStream.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(elseBodyStream).ReadToEnd());
                ps.WriteLine("label_" + labelEnd + ":");     
            }
            else 
            {
                ps.WriteLine("label_" + labelFalse + ":");
            }

            StackOutputStreams.Push(outStream);
        }

        public override void EnterUnless_statement(RubyParser.Unless_statementContext context)
        {
            StackLoopLabels.Push(++NumLabel);
            StackLoopLabels.Push(++NumLabel);

            var child = context.GetChild(4).GetText();

            if (child.Equals("else") || child.Equals("elsif"))
                StackLoopLabels.Push(++NumLabel);
        }

        public override void ExitUnless_statement(RubyParser.Unless_statementContext context)
        {
            var elseBodyStream = new MemoryStream();
            var labelEnd = 0;
            var child = context.GetChild(4).GetText();

            if (child.Equals("else") || child.Equals("elsif"))
            {
                elseBodyStream = StackOutputStreams.Pop();     
                labelEnd = StackLoopLabels.Pop();
            }

            var labelFalse = StackLoopLabels.Pop();
            var labelTrue = StackLoopLabels.Pop();

            var bodyStream = StackOutputStreams.Pop();
            var condStream = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var conditionVar = StringValues.Get(context.GetChild(1));

            ps.WriteLine("");
            condStream.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(condStream).ReadToEnd());
            ps.WriteLine("unless " + conditionVar + " goto label_" + labelTrue);
            ps.WriteLine("goto label_" + labelFalse);
            ps.WriteLine("label_" + labelTrue + ":");

            bodyStream.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(bodyStream).ReadToEnd());

            if (child.Equals("else") || child.Equals("elsif")) 
            {
                ps.WriteLine("goto label_" + labelEnd);
                ps.WriteLine("label_" + labelFalse + ":");
                elseBodyStream.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(elseBodyStream).ReadToEnd());
                ps.WriteLineAsync("label_" + labelEnd + ":");     
            }
            else 
            {
                ps.WriteLine("label_" + labelFalse + ":");
            }

            StackOutputStreams.Push(outStream);
        }

        public override void EnterWhile_statement(RubyParser.While_statementContext context)
        {
            StackLoopLabels.Push(++NumLabel);
            StackLoopLabels.Push(++NumLabel);
        }

        public override void ExitWhile_statement(RubyParser.While_statementContext context)
        {
            var body = StackOutputStreams.Pop();
            var cond = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var labelEnd = StackLoopLabels.Pop();
            var labelBegin = StackLoopLabels.Pop();

            var conditionVar = StringValues.Get(context.GetChild(1));
            ps.WriteLine("label_" + labelBegin + ":");
            cond.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(cond).ReadToEnd());
            ps.WriteLine("unless " + conditionVar + " goto label_" + labelEnd);
            body.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(body).ReadToEnd());
            ps.WriteLine("goto label_" + labelBegin);
            ps.WriteLine("label_" + labelEnd + ":");

            StackOutputStreams.Push(outStream);
        }

        public override void EnterFor_statement(RubyParser.For_statementContext context)
        {
            StackLoopLabels.Push(++NumLabel);
            StackLoopLabels.Push(++NumLabel);
        }

        public override void ExitFor_statement(RubyParser.For_statementContext context)
        {
            var temp4 = StackOutputStreams.Pop();
            var temp3 = StackOutputStreams.Pop();
            var temp2 = StackOutputStreams.Pop();
            var temp1 = StackOutputStreams.Pop();
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var labelEnd = StackLoopLabels.Pop();
            var labelBegin = StackLoopLabels.Pop();

            if (context.ChildCount == 11) 
            {
                temp1.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(temp1).ReadToEnd());
                var cond = StringValues.Get(context.GetChild(4));
                ps.WriteLine("label_" + labelBegin + ":");
                temp2.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(temp2).ReadToEnd());
                ps.WriteLine("unless " + cond + " goto label_" + labelEnd);
                temp4.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(temp4).ReadToEnd());
                temp3.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(temp3).ReadToEnd());
                ps.WriteLine("goto label_" + labelBegin);
                ps.WriteLine("label_" + labelEnd + ":");
            }
            else if (context.ChildCount == 9) 
            {
                temp1.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(temp1).ReadToEnd());
                var cond = StringValues.Get(context.GetChild(3));
                ps.WriteLine("label_" + labelBegin + ":");
                temp2.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(temp2).ReadToEnd());
                ps.WriteLine("unless " + cond + " goto label_" + labelEnd);
                temp4.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(temp4).ReadToEnd());
                temp3.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(temp3).ReadToEnd());
                ps.WriteLine("goto label_" + labelBegin);
                ps.WriteLine("label_" + labelEnd + ":");
            }

            StackOutputStreams.Push(outStream);
        }

        public override void EnterInit_expression(RubyParser.Init_expressionContext context)
        {
            var temp1 = new MemoryStream();
            StackOutputStreams.Push(temp1);
        }

        public override void EnterCond_expression(RubyParser.Cond_expressionContext context)
        {
            var temp2 = new MemoryStream();
            StackOutputStreams.Push(temp2);
        }

        public override void ExitCond_expression(RubyParser.Cond_expressionContext context)
        {
            StringValues.Put(context, StringValues.Get(context.GetChild(0)));
        }

        public override void EnterLoop_expression(RubyParser.Loop_expressionContext context)
        {
            var temp3 = new MemoryStream();
            StackOutputStreams.Push(temp3);
        }

        public override void EnterStatement_body(RubyParser.Statement_bodyContext context)
        {
            var temp4 = new MemoryStream();
            StackOutputStreams.Push(temp4);
        }

        public override void EnterDynamic_assignment(RubyParser.Dynamic_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
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
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var str = context.var_id.GetText() + " " + context.op.Text + " " + StringValues.Get(context.GetChild(2));
            NumReg = 0;
            ps.WriteLine(str);

            StackOutputStreams.Push(outStream);
        }

        public override void EnterInt_assignment(RubyParser.Int_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
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
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var str = context.var_id.GetText() + " " + context.op.Text + " " + IntValues.Get(context.GetChild(2));
            ps.WriteLine(str);

            StackOutputStreams.Push(outStream);
        }

        public override void EnterFloat_assignment(RubyParser.Float_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
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
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var str = context.var_id.GetText() + " " + context.op.Text + " " + FloatValues.Get(context.GetChild(2));
            ps.WriteLine(str);

            StackOutputStreams.Push(outStream);
        }

        public override void EnterString_assignment(RubyParser.String_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
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
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var str = context.var_id.GetText() + " " + context.op.Text + " \"" + StringValues.Get(context.GetChild(2)) + "\"";
            ps.WriteLine(str);

            StackOutputStreams.Push(outStream);
        }

        public override void EnterInitial_array_assignment(RubyParser.Initial_array_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
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
        
        public override void ExitArray_assignment(RubyParser.Array_assignmentContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
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
                    ps.WriteLine(arrDef + " = " + resultFloat.ToString(CultureInfo));
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

        public override void ExitDynamic_result(RubyParser.Dynamic_resultContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

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

        public override void ExitDynamic(RubyParser.DynamicContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var strDynTerm = StringValues.Get(context.GetChild(0));
            ps.WriteLine("$P" + NumReg + " = new \"Integer\"");
            ps.WriteLine("$P" + NumReg + " = " + strDynTerm);
            StringValues.Put(context, "$P" + NumReg);
            NumReg++;
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));

            StackOutputStreams.Push(outStream);
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
                        StringValues.Put(context,Repeat(str, times));
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

        public override void ExitComparison_list(RubyParser.Comparison_listContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            if ( context.ChildCount == 3 && context.op != null ) 
            {
                var left = StringValues.Get(context.GetChild(0));
                var right = StringValues.Get(context.GetChild(2));     

                switch(context.op.Type) {
                    case RubyParser.BIT_AND:
                        ps.WriteLine("$I" + NumRegInt + " = " + left + " && " + right);
                        break;
                    case RubyParser.AND:
                        ps.WriteLine("$I" + NumRegInt + " = " + left + " && " + right);
                        break;
                    case RubyParser.BIT_OR:
                        ps.WriteLine("$I" + NumRegInt + " = " + left + " || " + right);
                        break;
                    case RubyParser.OR:
                        ps.WriteLine("$I" + NumRegInt + " = " + left + " || " + right);
                        break;
                }

                StringValues.Put(context, "$I" + NumRegInt);
                NumRegInt++;
            }
            else if ( context.ChildCount == 3 && context.op == null )
            {
                StringValues.Put(context, StringValues.Get(context.GetChild(1)));
            }
            else if ( context.ChildCount == 1 ) 
            {
                StringValues.Put(context, StringValues.Get(context.GetChild(0)));
            }

            StackOutputStreams.Push(outStream);
        }

        public override void ExitComparison(RubyParser.ComparisonContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var left = StringValues.Get(context.GetChild(0));
            var right = StringValues.Get(context.GetChild(2));

            switch(context.op.Type) 
            {
                case RubyParser.LESS:
                    ps.WriteLine("$I" + NumRegInt + " = islt " + left + ", " + right);
                    break;
                case RubyParser.GREATER:
                    ps.WriteLine("$I" + NumRegInt + " = isgt " + left + ", " + right);
                    break;
                case RubyParser.LESS_EQUAL:
                    ps.WriteLine("$I" + NumRegInt + " = isle " + left + ", " + right);
                    break;
                case RubyParser.GREATER_EQUAL:
                    ps.WriteLine("$I" + NumRegInt + " = isge " + left + ", " + right);
                    break;
                case RubyParser.EQUAL:
                    ps.WriteLine("$I" + NumRegInt + " = iseq " + left + ", " + right);
                    break;
                case RubyParser.NOT_EQUAL:
                    var temp = "\n$I" + NumRegInt + " = not " + "$I" + NumRegInt;
                    ps.WriteLine("$I" + NumRegInt + " = iseq " + left + ", " + right + temp);
                    
                    break;
            }
            StringValues.Put(context, "$I" + NumRegInt);
            NumRegInt++;

            StackOutputStreams.Push(outStream);
        }

        public override void ExitComp_var(RubyParser.Comp_varContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var typeArg = WhichValues.Get(context.GetChild(0));
            var strOutput = "";
            
            switch(typeArg) 
            {
                case "Integer":
                    var resultInt = IntValues.Get(context.GetChild(0));
                    strOutput = "$P" + NumReg + " = " + resultInt;
                    break;
                case "Float":
                    var resultFloat = FloatValues.Get(context.GetChild(0));
                    strOutput = "$P" + NumReg + " = " + resultFloat.ToString(CultureInfo);
                    break;
                case "String":
                    var resultString = StringValues.Get(context.GetChild(0));
                    strOutput = "$P" + NumReg + " = " + resultString;
                    break;
                case "Dynamic":
                    var resultDynamic = StringValues.Get(context.GetChild(0));
                    StringValues.Put(context, resultDynamic);
                    WhichValues.Put(context, typeArg);
                    StackOutputStreams.Push(outStream);
                    return;
            }

            ps.WriteLine("$P" + NumReg + " = new \"Integer\"");
            ps.WriteLine(strOutput);
            StringValues.Put(context, "$P" + NumReg);
            WhichValues.Put(context, "Dynamic");
            NumReg++;

            StackOutputStreams.Push(outStream);
        }

        public override void ExitBreak_expression(RubyParser.Break_expressionContext context)
        {
            var outStream = StackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var labelEndCurrentLoop = StackLoopLabels.Pop();

            ps.WriteLine("goto label_" + labelEndCurrentLoop);

            StackLoopLabels.Push(labelEndCurrentLoop);
            StackOutputStreams.Push(outStream);
        }

        public override void ExitLiteral_t(RubyParser.Literal_tContext context)
        {
            StringValues.Put(context, StringValues.Get(context.GetChild(0)));
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));
        }

        public override void ExitFloat_t(RubyParser.Float_tContext context)
        {
            FloatValues.Put(context, FloatValues.Get(context.GetChild(0)));
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));
        }

        public override void ExitInt_t(RubyParser.Int_tContext context)
        {
            IntValues.Put(context, IntValues.Get(context.GetChild(0)));
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));
        }

        public override void ExitId(RubyParser.IdContext context)
        {
            StringValues.Put(context, StringValues.Get(context.GetChild(0)));
            WhichValues.Put(context, WhichValues.Get(context.GetChild(0)));
        }

        public override void ExitCrlf(RubyParser.CrlfContext context)
        {
            NumStr++;
        }

        public override void VisitTerminal(ITerminalNode node)
        {
            var symbol = node.Symbol;
            switch(symbol.Type) 
            {
                case RubyParser.INT:
                    IntValues.Put(node, int.Parse(symbol.Text));
                    WhichValues.Put(node, "Integer");
                    break;
                case RubyParser.FLOAT:
                    FloatValues.Put(node, float.Parse(symbol.Text, NumberStyles.Any, CultureInfo));
                    WhichValues.Put(node, "Float");
                    break;
                case RubyParser.LITERAL:
                    var strTerminal = symbol.Text;
                    strTerminal = Regex.Replace(strTerminal, "\"$", "");
                    strTerminal = Regex.Replace(strTerminal, "^\"", "");
                    strTerminal = Regex.Replace(strTerminal, "\'$", "");
                    strTerminal = Regex.Replace(strTerminal, "^\'", "");
                    StringValues.Put(node, strTerminal);
                    WhichValues.Put(node, "String");
                    break;
                case RubyParser.ID:
                    var dynamicTerminal = symbol.Text;
                    StringValues.Put(node, dynamicTerminal);
                    WhichValues.Put(node, "Dynamic");
                    break;
            }
        }

    }
}