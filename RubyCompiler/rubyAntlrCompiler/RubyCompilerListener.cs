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
        private readonly ParseTreeProperty<int> _intValues = new ParseTreeProperty<int>();
        private readonly ParseTreeProperty<float> _floatValues = new ParseTreeProperty<float>();
        private readonly ParseTreeProperty<string> _stringValues = new ParseTreeProperty<string>();
        private readonly ParseTreeProperty<string> _whichValues = new ParseTreeProperty<string>();
        
        private readonly Stack<MemoryStream> _stackOutputStreams = new Stack<MemoryStream>();
        private readonly Hashtable _functionDefinitionStreams = new Hashtable();
        private readonly MemoryStream _mainStream = new MemoryStream();
        private readonly MemoryStream _functionStream = new MemoryStream();
        private readonly MemoryStream _errorStream = new MemoryStream();
        private readonly StreamWriter _printStreamError = null;

        private int _semanticErrorsNum = 0;
        private int _numStr = 1;
        private int _numRegInt = 0;
        private int _numReg = 0;
        private int _numLabel = 0;
        private readonly Stack<int> _stackLoopLabels = new Stack<int>();
        private readonly LinkedList<string> _mainDefenitions = new LinkedList<string>();
        private readonly ArrayList _functionCalls = new ArrayList();
        private readonly Stack<LinkedList<string>> _stackDefinitions = new Stack<LinkedList<string>>();

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
            _printStreamError = new StreamWriter(_errorStream) {AutoFlush = true};
            CultureInfo = (CultureInfo) CultureInfo.CurrentCulture.Clone();
            CultureInfo.NumberFormat.CurrencyDecimalSeparator = ".";
            CultureInfo.NumberFormat.NumberDecimalSeparator = ".";
        }

        public CultureInfo CultureInfo { get; set; }

        public void CreateIRFile(string path)
        {
            var outStream = new StreamWriter(path) {AutoFlush = true};
            var codeStream = _stackOutputStreams.Pop();
            codeStream.Seek(0, SeekOrigin.Begin);
            var irCode = new StreamReader(codeStream).ReadToEnd();
            Console.WriteLine(irCode);
            outStream.Write(irCode);
        }

        public bool HasSemanticError()
        {
            return _semanticErrorsNum > 0;
        }

        public string GetErrors()
        {
            if (!HasSemanticError())
                return "";

            _errorStream.Seek(0, SeekOrigin.Begin);
            return new StreamReader(_errorStream).ReadToEnd();
        }

        public override void EnterProg(RubyParser.ProgContext context)
        {
            MemoryStream outStream = _mainStream;
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            ps.WriteLine(".sub main");
            _stackDefinitions.Push(_mainDefenitions);
            _stackOutputStreams.Push(outStream);
        }

        public override void ExitProg(RubyParser.ProgContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            
            ps.WriteLine("\n.end");
            ps.WriteLine("\n.include \"stdlib/stdlib.pir\"");

            foreach (var functionCall in _functionCalls)
            {
                MemoryStream funcStream = _functionDefinitionStreams[functionCall] as MemoryStream;
                if (funcStream != null)
                {
                    funcStream.Seek(0, SeekOrigin.Begin);
                    ps.WriteLine(new StreamReader(funcStream).ReadToEnd());
                }
                    
            }

            _stackDefinitions.Pop();
            _stackOutputStreams.Push(outStream);
        }

        public override void ExitGlobal_get(RubyParser.Global_getContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var defenitions = _stackDefinitions.Pop();
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
            
            _stackOutputStreams.Push(outStream);
            _stackDefinitions.Push(defenitions);
        }

        public override void ExitGlobal_set(RubyParser.Global_setContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var definitions = _stackDefinitions.Pop();
            var printStream = new StreamWriter(outStream) {AutoFlush = true};

            var global = context.global_name.GetText();

            var typeArg = _whichValues.Get(context.GetChild(2));

            switch(typeArg) 
            {
                case "Integer":
                    var resultInt = _intValues.Get(context.GetChild(2));
                    printStream.WriteLine("set_global \"" + global + "\", " + resultInt);
                    break;
                case "Float":
                    var resultFloat = _floatValues.Get(context.GetChild(2));
                    printStream.WriteLine("set_global \"" + global + "\", " + resultFloat.ToString(CultureInfo));
                    break;
                case "String":
                    var resultString = _stringValues.Get(context.GetChild(2));
                    printStream.WriteLine("set_global \"" + global + "\", " + resultString);
                    break;
                case "Dynamic":
                    var resultDynamic = _stringValues.Get(context.GetChild(2));
                    printStream.WriteLine("set_global \"" + global + "\", " + resultDynamic);
                    break;
            }

            _stackOutputStreams.Push(outStream);
            _stackDefinitions.Push(definitions);
        }

        public override void ExitFunction_inline_call(RubyParser.Function_inline_callContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            ps.WriteLine(_stringValues.Get(context.GetChild(0)));
            _stackOutputStreams.Push(outStream);
        }

        public override void EnterPir_expression_list(RubyParser.Pir_expression_listContext context)
        {
            var emptyStream = new MemoryStream();
            _stackOutputStreams.Push(emptyStream);
        }

        public override void ExitPir_expression_list(RubyParser.Pir_expression_listContext context)
        {
            var emptyStream = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            ps.WriteLine(context.GetText());

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterFunction_definition(RubyParser.Function_definitionContext context)
        {
            var funcDefinitions = new LinkedList<string>();
            _stackDefinitions.Push(funcDefinitions);
            var funcParams = new MemoryStream();
            _stackOutputStreams.Push(funcParams);
            var outStream = new MemoryStream();
            _stackOutputStreams.Push(outStream);
        }

        public override void ExitFunction_definition(RubyParser.Function_definitionContext context)
        {
            var funcBody = _stackOutputStreams.Pop();
            var funcParams = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var funcName = _stringValues.Get(context.GetChild(0));
            ps.WriteLine("\n.sub " + funcName);
            ps.WriteLine("");
            funcParams.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(funcParams).ReadToEnd());
            funcBody.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(funcBody).ReadToEnd());
            ps.Write(".end");

            _functionDefinitionStreams[funcName] = outStream;

            _stackDefinitions.Pop();
        }

        public override void EnterFunction_definition_body(RubyParser.Function_definition_bodyContext context)
        {
            var funcBody = new MemoryStream();
            _stackOutputStreams.Push(funcBody);
        }

        public override void ExitFunction_definition_header(RubyParser.Function_definition_headerContext context)
        {
            _stringValues.Put(context, context.GetChild(1).GetText());
        }

        public override void ExitFunction_definition_param_id(RubyParser.Function_definition_param_idContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var paramId = context.GetChild(0).GetText();
            ps.WriteLine(".param pmc " + paramId);
            ps.Flush();
            
            _stackOutputStreams.Push(outStream);
        }

        public override void ExitReturn_statement(RubyParser.Return_statementContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var typeArg = _whichValues.Get(context.GetChild(1));

            switch(typeArg) 
            {
                case "Integer":
                    var resultInt = _intValues.Get(context.GetChild(1));
                    ps.WriteLine(".return(" + resultInt + ")");
                    break;
                case "Float":
                    var resultFloat = _floatValues.Get(context.GetChild(1));
                    ps.WriteLine(".return(" + resultFloat.ToString(CultureInfo) + ")");
                    break;
                case "String":
                    var resultString = _stringValues.Get(context.GetChild(1));
                    ps.WriteLine(".return(" + resultString + ")");
                    break;
                case "Dynamic":
                    var resultDynamic = _stringValues.Get(context.GetChild(1));
                    ps.WriteLine(".return(" + resultDynamic + ")");
                    break;
            }

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterFunction_call(RubyParser.Function_callContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var assignmentStream = new MemoryStream();
            var argsStream = new MemoryStream();
            _stackOutputStreams.Push(outStream);
            _stackOutputStreams.Push(argsStream);
            _stackOutputStreams.Push(assignmentStream);
        }

        public override void ExitFunction_call(RubyParser.Function_callContext context)
        {
            var assignmentStream = _stackOutputStreams.Pop();
            var argsStream = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            argsStream.Seek(0, SeekOrigin.Begin);
            var args = new StreamReader(argsStream).ReadToEnd();
            var funcName = context.name.GetText();
            args = Regex.Replace(args, ",$", ""); 
            // ASSIGNMENT of dynamic function params
            assignmentStream.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(assignmentStream).ReadToEnd());
            // call of function
            _stringValues.Put(context, funcName + "(" + args + ")");

            _functionCalls.Add(funcName);

            _stackOutputStreams.Push(outStream);
        }

        public override void ExitFunction_unnamed_param(RubyParser.Function_unnamed_paramContext context)
        {
            var assignmentStream = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            switch(_whichValues.Get(context.GetChild(0))) 
            {
                case "Integer":
                    var resultInt = _intValues.Get(context.GetChild(0));
                    ps.Write(resultInt + ",");
                    break;
                case "Float":
                    var resultFloat = _floatValues.Get(context.GetChild(0));
                    ps.Write(resultFloat.ToString(CultureInfo) + ",");
                    break;
                case "String":
                    var resultString = _stringValues.Get(context.GetChild(0));
                    ps.Write("\"" + resultString + "\",");
                    break;
                case "Dynamic":
                    var resultDynamic = _stringValues.Get(context.GetChild(0));
                    ps.Write(resultDynamic + ",");
                    break;
            }

            _stackOutputStreams.Push(outStream);
            _stackOutputStreams.Push(assignmentStream);
        }

        public override void ExitFunction_named_param(RubyParser.Function_named_paramContext context)
        {
            var assignmentStream = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var idParam = context.GetChild(0).GetText();

            switch(_whichValues.Get(context.GetChild(2)))
            {
                case "Integer":
                    var resultInt = _intValues.Get(context.GetChild(2));
                    ps.Write(resultInt + " :named(\"" + idParam + "\"),");
                    break;
                case "Float":
                    var resultFloat = _floatValues.Get(context.GetChild(2));
                    ps.Write(resultFloat.ToString(CultureInfo) + " :named(\"" + idParam + "\"),");
                    break;
                case "String":
                    var resultString = _stringValues.Get(context.GetChild(2));
                    ps.Write("\"" + resultString + "\" :named(\"" + idParam + "\"),");
                    break;
                case "Dynamic":
                    var resultDynamic = _stringValues.Get(context.GetChild(2));
                    ps.Write(resultDynamic + " :named(\"" + idParam + "\"),");
                    break;
            }

            _stackOutputStreams.Push(outStream);
            _stackOutputStreams.Push(assignmentStream);
        }

        public override void EnterFunction_call_assignment(RubyParser.Function_call_assignmentContext context)
        {
            var outStream = new MemoryStream();
            _stackOutputStreams.Push(outStream);
        }

        public override void ExitFunction_call_assignment(RubyParser.Function_call_assignmentContext context)
        {
            var func = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            func.Seek(0, SeekOrigin.Begin);
            var funcCall = new StreamReader(func).ReadToEnd();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            ps.Write(funcCall);

            _stringValues.Put(context, _stringValues.Get(context.GetChild(0)));
            _whichValues.Put(context, "Dynamic");
            _stackOutputStreams.Push(outStream);
        }

        public override void ExitAll_result(RubyParser.All_resultContext context)
        {
            var typeArg = _whichValues.Get(context.GetChild(0));
            
            switch(typeArg)
            {
                case "Integer":
                    var resultInt = _intValues.Get(context.GetChild(0));
                    _intValues.Put(context, resultInt);
                    _whichValues.Put(context, typeArg);
                    break;
                case "Float":
                    var resultFloat = _floatValues.Get(context.GetChild(0));
                    _floatValues.Put(context, resultFloat);
                    _whichValues.Put(context, typeArg);
                    break;
                case "String":
                    var resultString = _stringValues.Get(context.GetChild(0));
                    _stringValues.Put(context, "\"" + resultString + "\"");
                    _whichValues.Put(context, typeArg);
                    break;
                case "Dynamic":
                    var resultDynamic = _stringValues.Get(context.GetChild(0));
                    _stringValues.Put(context, resultDynamic);
                    _whichValues.Put(context, typeArg);
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
                labelEnd = _stackLoopLabels.Pop();
                _stackLoopLabels.Push(labelEnd);     
            }

            _stackLoopLabels.Push(++_numLabel);
            _stackLoopLabels.Push(++_numLabel);

            if (childCount > 4)
                _stackLoopLabels.Push(labelEnd);

            _stackOutputStreams.Push(elsifStream);
        }

        public override void ExitIf_elsif_statement(RubyParser.If_elsif_statementContext context)
        {
            var elseBodyStream = new MemoryStream();
            var labelEnd = 0;
            var childCount = context.ChildCount;

            if (childCount > 4) {
                elseBodyStream = _stackOutputStreams.Pop();   
                labelEnd = _stackLoopLabels.Pop();  
            }

            var bodyStream = _stackOutputStreams.Pop();
            var condStream = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var conditionVar = _stringValues.Get(context.GetChild(1));

            var labelFalse = _stackLoopLabels.Pop();
            var labelTrue = _stackLoopLabels.Pop();

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

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterIf_statement(RubyParser.If_statementContext context)
        {
            _stackLoopLabels.Push(++_numLabel);
            _stackLoopLabels.Push(++_numLabel);

            var child = context.GetChild(4).GetText();

            if (child.Equals("else") || child.Equals("elsif"))
                _stackLoopLabels.Push(++_numLabel);
        }

        public override void ExitIf_statement(RubyParser.If_statementContext context)
        {
            var elseBodyStream = new MemoryStream();
            var labelEnd = 0;
            var child = context.GetChild(4).GetText();

            if (child.Equals("else") || child.Equals("elsif"))
            {
                elseBodyStream = _stackOutputStreams.Pop();     
                labelEnd = _stackLoopLabels.Pop();
            }

            var labelFalse = _stackLoopLabels.Pop();
            var labelTrue = _stackLoopLabels.Pop();

            var bodyStream = _stackOutputStreams.Pop();
            var condStream = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var conditionVar = _stringValues.Get(context.GetChild(1));

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

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterUnless_statement(RubyParser.Unless_statementContext context)
        {
            _stackLoopLabels.Push(++_numLabel);
            _stackLoopLabels.Push(++_numLabel);

            var child = context.GetChild(4).GetText();

            if (child.Equals("else") || child.Equals("elsif"))
                _stackLoopLabels.Push(++_numLabel);
        }

        public override void ExitUnless_statement(RubyParser.Unless_statementContext context)
        {
            var elseBodyStream = new MemoryStream();
            var labelEnd = 0;
            var child = context.GetChild(4).GetText();

            if (child.Equals("else") || child.Equals("elsif"))
            {
                elseBodyStream = _stackOutputStreams.Pop();     
                labelEnd = _stackLoopLabels.Pop();
            }

            var labelFalse = _stackLoopLabels.Pop();
            var labelTrue = _stackLoopLabels.Pop();

            var bodyStream = _stackOutputStreams.Pop();
            var condStream = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var conditionVar = _stringValues.Get(context.GetChild(1));

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

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterWhile_statement(RubyParser.While_statementContext context)
        {
            _stackLoopLabels.Push(++_numLabel);
            _stackLoopLabels.Push(++_numLabel);
        }

        public override void ExitWhile_statement(RubyParser.While_statementContext context)
        {
            var body = _stackOutputStreams.Pop();
            var cond = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var labelEnd = _stackLoopLabels.Pop();
            var labelBegin = _stackLoopLabels.Pop();

            var conditionVar = _stringValues.Get(context.GetChild(1));
            ps.WriteLine("label_" + labelBegin + ":");
            cond.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(cond).ReadToEnd());
            ps.WriteLine("unless " + conditionVar + " goto label_" + labelEnd);
            body.Seek(0, SeekOrigin.Begin);
            ps.WriteLine(new StreamReader(body).ReadToEnd());
            ps.WriteLine("goto label_" + labelBegin);
            ps.WriteLine("label_" + labelEnd + ":");

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterFor_statement(RubyParser.For_statementContext context)
        {
            _stackLoopLabels.Push(++_numLabel);
            _stackLoopLabels.Push(++_numLabel);
        }

        public override void ExitFor_statement(RubyParser.For_statementContext context)
        {
            var temp4 = _stackOutputStreams.Pop();
            var temp3 = _stackOutputStreams.Pop();
            var temp2 = _stackOutputStreams.Pop();
            var temp1 = _stackOutputStreams.Pop();
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var labelEnd = _stackLoopLabels.Pop();
            var labelBegin = _stackLoopLabels.Pop();

            if (context.ChildCount == 11) 
            {
                temp1.Seek(0, SeekOrigin.Begin);
                ps.WriteLine(new StreamReader(temp1).ReadToEnd());
                var cond = _stringValues.Get(context.GetChild(4));
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
                var cond = _stringValues.Get(context.GetChild(3));
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

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterInit_expression(RubyParser.Init_expressionContext context)
        {
            var temp1 = new MemoryStream();
            _stackOutputStreams.Push(temp1);
        }

        public override void EnterCond_expression(RubyParser.Cond_expressionContext context)
        {
            var temp2 = new MemoryStream();
            _stackOutputStreams.Push(temp2);
        }

        public override void ExitCond_expression(RubyParser.Cond_expressionContext context)
        {
            _stringValues.Put(context, _stringValues.Get(context.GetChild(0)));
        }

        public override void EnterLoop_expression(RubyParser.Loop_expressionContext context)
        {
            var temp3 = new MemoryStream();
            _stackOutputStreams.Push(temp3);
        }

        public override void EnterStatement_body(RubyParser.Statement_bodyContext context)
        {
            var temp4 = new MemoryStream();
            _stackOutputStreams.Push(temp4);
        }

        public override void EnterDynamic_assignment(RubyParser.Dynamic_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var definitions = _stackDefinitions.Pop();

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
                        _numReg = 0;
                    }
                    break;
                default:
                    variable = context.var_id.GetText();
                    if (!IsDefined(definitions, variable)) 
                    {
                        _printStreamError.WriteLine("line " + _numStr + " Error! Undefined variable " + variable + "!");
                        _semanticErrorsNum++;
                    }
                    break;
            }

            _stackOutputStreams.Push(outStream); 
            _stackDefinitions.Push(definitions);
        }

        public override void ExitDynamic_assignment(RubyParser.Dynamic_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var str = context.var_id.GetText() + " " + context.op.Text + " " + _stringValues.Get(context.GetChild(2));
            _numReg = 0;
            ps.WriteLine(str);

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterInt_assignment(RubyParser.Int_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var definitions = _stackDefinitions.Pop();

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
                        _printStreamError.WriteLine("line " + _numStr + " Error! Undefined variable " + variable + "!");
                        _semanticErrorsNum++;
                    }
                    break;
            }

            _stackOutputStreams.Push(outStream); 
            _stackDefinitions.Push(definitions);
        }

        public override void ExitInt_assignment(RubyParser.Int_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var str = context.var_id.GetText() + " " + context.op.Text + " " + _intValues.Get(context.GetChild(2));
            ps.WriteLine(str);

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterFloat_assignment(RubyParser.Float_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var definitions = _stackDefinitions.Pop();

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
                        _printStreamError.WriteLine("line " + _numStr + " Error! Undefined variable " + variable + "!");
                        _semanticErrorsNum++;
                    }
                    break;
            }

            _stackOutputStreams.Push(outStream);
            _stackDefinitions.Push(definitions);
        }

        public override void ExitFloat_assignment(RubyParser.Float_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var str = context.var_id.GetText() + " " + context.op.Text + " " + _floatValues.Get(context.GetChild(2));
            ps.WriteLine(str);

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterString_assignment(RubyParser.String_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var definitions = _stackDefinitions.Pop();

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
                        _printStreamError.WriteLine("line " + _numStr + " Error! Undefined variable " + variable + "!");
                        _semanticErrorsNum++;
                    }
                    break;
            }

            _stackOutputStreams.Push(outStream);
            _stackDefinitions.Push(definitions);
        }

        public override void ExitString_assignment(RubyParser.String_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var str = context.var_id.GetText() + " " + context.op.Text + " \"" + _stringValues.Get(context.GetChild(2)) + "\"";
            ps.WriteLine(str);

            _stackOutputStreams.Push(outStream);
        }

        public override void EnterInitial_array_assignment(RubyParser.Initial_array_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var definitions = _stackDefinitions.Pop();

            var variable = context.var_id.GetText();

            if (!IsDefined(definitions, variable)) {
                ps.WriteLine("");
                ps.WriteLine(".local pmc " + variable);
                definitions.AddLast(variable);
            }
            ps.WriteLine(variable + " = new \"ResizablePMCArray\"");

            _stackOutputStreams.Push(outStream);
            _stackDefinitions.Push(definitions);
        }
        
        public override void ExitArray_assignment(RubyParser.Array_assignmentContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var definitions = _stackDefinitions.Pop();
            var arrDef = _stringValues.Get(context.GetChild(0));

            var typeArg = _whichValues.Get(context.GetChild(2));

            switch(typeArg)
            {
                case "Integer":
                    var resultInt = _intValues.Get(context.GetChild(2));
                    ps.WriteLine(arrDef + " = " + resultInt);
                    break;
                case "Float":
                    var resultFloat = _floatValues.Get(context.GetChild(2));
                    ps.WriteLine(arrDef + " = " + resultFloat.ToString(CultureInfo));
                    break;
                case "String":
                    var resultString = _stringValues.Get(context.GetChild(2));
                    ps.WriteLine(arrDef + " = " + resultString);
                    break;
                case "Dynamic":
                    var resultDynamic = _stringValues.Get(context.GetChild(2));
                    ps.WriteLine(arrDef + " = " + resultDynamic);
                    break;
            }

            _stackOutputStreams.Push(outStream);
            _stackDefinitions.Push(definitions);
        }

        public override void ExitArray_selector(RubyParser.Array_selectorContext context)
        {
            var name = _stringValues.Get(context.GetChild(0));
            var typeArg = _whichValues.Get(context.GetChild(2));

            switch(typeArg)
            {
                case "Integer":
                    var selectorInt = _intValues.Get(context.GetChild(2));
                    _stringValues.Put(context, name + "[" + selectorInt + "]");
                    break;
                case "Dynamic":
                    var selectorStr = _stringValues.Get(context.GetChild(2));
                    _stringValues.Put(context, name + "[" + selectorStr + "]");
                    break;
            }

            _whichValues.Put(context, "Dynamic");
        }

        public override void ExitDynamic_result(RubyParser.Dynamic_resultContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            if ( context.ChildCount == 3 && context.op != null ) 
            { 

                int intDyn = 0;
                float floatDyn = 0;
                var strDyn = "";
                var strDyn1 = "";
                var anotherNode = "";
                var strOutput = "";

                switch(_whichValues.Get(context.GetChild(0)))
                {
                    case "Integer":
                        intDyn = _intValues.Get(context.GetChild(0));
                        anotherNode = _stringValues.Get(context.GetChild(2));
                        strOutput = "$P" + _numReg + " = " + anotherNode + " " + context.op.Text + " " + intDyn;                  
                        break;
                    case "Float":
                        floatDyn = _floatValues.Get(context.GetChild(0));
                        anotherNode = _stringValues.Get(context.GetChild(2));
                        strOutput = "$P" + _numReg + " = " + anotherNode + " " + context.op.Text + " " + floatDyn;
                        break;
                    case "String":
                        strDyn = _stringValues.Get(context.GetChild(0));
                        anotherNode = _stringValues.Get(context.GetChild(2));
                        strOutput = "$P" + _numReg + " = " + anotherNode + " " + context.op.Text + " " + strDyn;
                        break;
                    case "Dynamic":
                        strDyn1 = _stringValues.Get(context.GetChild(0));
                        switch(_whichValues.Get(context.GetChild(2)))
                        {
                            case "Integer":
                                intDyn = _intValues.Get(context.GetChild(2));
                                strOutput = "$P" + _numReg + " = " + strDyn1 + " " + context.op.Text + " " + intDyn;                  
                                break;
                            case "Float":
                                floatDyn = _floatValues.Get(context.GetChild(2));
                                strOutput = "$P" + _numReg + " = " + strDyn1 + " " + context.op.Text + " " + floatDyn;
                                break;
                            case "String":
                                strDyn = _stringValues.Get(context.GetChild(2));
                                strOutput = "$P" + _numReg + " = " + strDyn1 + " " + context.op.Text + " " + strDyn;
                                break;
                            case "Dynamic":
                                strDyn = _stringValues.Get(context.GetChild(2));
                                strOutput = "$P" + _numReg + " = " + strDyn1 + " " + context.op.Text + " " + strDyn;
                                break;
                        }
                        break;
                }

                ps.WriteLine("$P" + _numReg + " = new \"Integer\"");
                ps.WriteLine(strOutput);
                _stringValues.Put(context, "$P" + _numReg);
                _whichValues.Put(context, "Dynamic");
                _numReg++;
            }
            else if ( context.ChildCount == 1 )
            { 
                _stringValues.Put(context, _stringValues.Get(context.GetChild(0)));
                _whichValues.Put(context, "Dynamic");
            }
            else if ( context.ChildCount == 3 && context.op == null )
            { 
                _stringValues.Put(context, _stringValues.Get(context.GetChild(1)));
                _whichValues.Put(context, "Dynamic");
            }

            _stackOutputStreams.Push(outStream);
        }

        public override void ExitDynamic(RubyParser.DynamicContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var strDynTerm = _stringValues.Get(context.GetChild(0));
            ps.WriteLine("$P" + _numReg + " = new \"Integer\"");
            ps.WriteLine("$P" + _numReg + " = " + strDynTerm);
            _stringValues.Put(context, "$P" + _numReg);
            _numReg++;
            _whichValues.Put(context, _whichValues.Get(context.GetChild(0)));

            _stackOutputStreams.Push(outStream);
        }

        public override void ExitInt_result(RubyParser.Int_resultContext context)
        {
            if ( context.ChildCount == 3 && context.op != null )
            { 
                var left = _intValues.Get(context.GetChild(0));
                var right = _intValues.Get(context.GetChild(2));

                switch(context.op.Type) 
                {
                    case RubyParser.MUL:
                        _intValues.Put(context, left * right);
                        _whichValues.Put(context, "Integer");
                        break;
                    case RubyParser.DIV:
                        _intValues.Put(context, left / right);
                        _whichValues.Put(context, "Integer");
                        break;
                    case RubyParser.MOD:
                        _intValues.Put(context, left % right);
                        _whichValues.Put(context, "Integer");
                        break;
                    case RubyParser.PLUS:
                        _intValues.Put(context, left + right);
                        _whichValues.Put(context, "Integer");
                        break;
                    case RubyParser.MINUS:
                        _intValues.Put(context, left - right);
                        _whichValues.Put(context, "Integer");
                        break;
                }
            }
            else if ( context.ChildCount == 1 )
            { 
                _intValues.Put(context, _intValues.Get(context.GetChild(0)));
                _whichValues.Put(context,  "Integer");
            }
            else if ( context.ChildCount == 3 && context.op == null ) 
            { 
                _intValues.Put(context, _intValues.Get(context.GetChild(1)));
                _whichValues.Put(context, "Integer");
            }
        }

        public override void ExitFloat_result(RubyParser.Float_resultContext context)
        {
            if ( context.ChildCount == 3 && context.op != null )
            {
                float left = 0;
                float right = 0;

                switch(_whichValues.Get(context.GetChild(0))) {
                    case "Integer":
                        left = (float) _intValues.Get(context.GetChild(0));
                        break;
                    case "Float":
                        left = _floatValues.Get(context.GetChild(0));
                        break;
                }

                switch(_whichValues.Get(context.GetChild(2))) {
                    case "Integer":
                        right = (float) _intValues.Get(context.GetChild(2));
                        break;
                    case "Float":
                        right = _floatValues.Get(context.GetChild(2));
                        break;
                }

                switch(context.op.Type) 
                {
                    case RubyParser.MUL:
                        _floatValues.Put(context, left * right);
                        _whichValues.Put(context, "Float");
                        break;
                    case RubyParser.DIV:
                        _floatValues.Put(context, left / right);
                        _whichValues.Put(context, "Float");
                        break;
                    case RubyParser.MOD:
                        _floatValues.Put(context, left % right);
                        _whichValues.Put(context, "Float");
                        break;
                    case RubyParser.PLUS:
                        _floatValues.Put(context, left + right);
                        _whichValues.Put(context, "Float");
                        break;
                    case RubyParser.MINUS:
                        _floatValues.Put(context, left - right);
                        _whichValues.Put(context, "Float");
                        break;
                }
            }
            else if ( context.ChildCount == 1 ) 
            { 
                _floatValues.Put(context, _floatValues.Get(context.GetChild(0)));
                _whichValues.Put(context, "Float");
            }
            else if ( context.ChildCount == 3 && context.op == null )
            { 
                _floatValues.Put(context, _floatValues.Get(context.GetChild(1)));
                _whichValues.Put(context, "Float");
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

                switch(_whichValues.Get(context.GetChild(0))) 
                {
                    case "Integer":
                        times = _intValues.Get(context.GetChild(0));
                        break;
                    case "String":
                        leftS = _stringValues.Get(context.GetChild(0));
                        str = leftS;
                        break;
                }

                switch(_whichValues.Get(context.GetChild(2))) 
                {
                    case "Integer":
                        times = _intValues.Get(context.GetChild(2));
                        break;
                    case "String":
                        rightS = _stringValues.Get(context.GetChild(2));
                        str = rightS;
                        break;
                }

                switch(context.op.Type)
                {
                    case RubyParser.MUL:
                        _stringValues.Put(context,Repeat(str, times));
                        _whichValues.Put(context, "String");
                        break;
                    case RubyParser.PLUS:
                        _stringValues.Put(context,  leftS + rightS);
                        _whichValues.Put(context, "String");
                        break;
                }
            }
            else if ( context.ChildCount == 1 )
            { 
                _stringValues.Put(context, _stringValues.Get(context.GetChild(0)));
                _whichValues.Put(context, "String");
            }
            else if ( context.ChildCount == 3 && context.op == null )
            { 
                _stringValues.Put(context, _stringValues.Get(context.GetChild(1)));
                _whichValues.Put(context, "String");
            }
        }

        public override void ExitComparison_list(RubyParser.Comparison_listContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            if ( context.ChildCount == 3 && context.op != null ) 
            {
                var left = _stringValues.Get(context.GetChild(0));
                var right = _stringValues.Get(context.GetChild(2));     

                switch(context.op.Type) {
                    case RubyParser.BIT_AND:
                        ps.WriteLine("$I" + _numRegInt + " = " + left + " && " + right);
                        break;
                    case RubyParser.AND:
                        ps.WriteLine("$I" + _numRegInt + " = " + left + " && " + right);
                        break;
                    case RubyParser.BIT_OR:
                        ps.WriteLine("$I" + _numRegInt + " = " + left + " || " + right);
                        break;
                    case RubyParser.OR:
                        ps.WriteLine("$I" + _numRegInt + " = " + left + " || " + right);
                        break;
                }

                _stringValues.Put(context, "$I" + _numRegInt);
                _numRegInt++;
            }
            else if ( context.ChildCount == 3 && context.op == null )
            {
                _stringValues.Put(context, _stringValues.Get(context.GetChild(1)));
            }
            else if ( context.ChildCount == 1 ) 
            {
                _stringValues.Put(context, _stringValues.Get(context.GetChild(0)));
            }

            _stackOutputStreams.Push(outStream);
        }

        public override void ExitComparison(RubyParser.ComparisonContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var left = _stringValues.Get(context.GetChild(0));
            var right = _stringValues.Get(context.GetChild(2));

            switch(context.op.Type) 
            {
                case RubyParser.LESS:
                    ps.WriteLine("$I" + _numRegInt + " = islt " + left + ", " + right);
                    break;
                case RubyParser.GREATER:
                    ps.WriteLine("$I" + _numRegInt + " = isgt " + left + ", " + right);
                    break;
                case RubyParser.LESS_EQUAL:
                    ps.WriteLine("$I" + _numRegInt + " = isle " + left + ", " + right);
                    break;
                case RubyParser.GREATER_EQUAL:
                    ps.WriteLine("$I" + _numRegInt + " = isge " + left + ", " + right);
                    break;
                case RubyParser.EQUAL:
                    ps.WriteLine("$I" + _numRegInt + " = iseq " + left + ", " + right);
                    break;
                case RubyParser.NOT_EQUAL:
                    var temp = "\n$I" + _numRegInt + " = not " + "$I" + _numRegInt;
                    ps.WriteLine("$I" + _numRegInt + " = iseq " + left + ", " + right + temp);
                    
                    break;
            }
            _stringValues.Put(context, "$I" + _numRegInt);
            _numRegInt++;

            _stackOutputStreams.Push(outStream);
        }

        public override void ExitComp_var(RubyParser.Comp_varContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};

            var typeArg = _whichValues.Get(context.GetChild(0));
            var strOutput = "";
            
            switch(typeArg) 
            {
                case "Integer":
                    var resultInt = _intValues.Get(context.GetChild(0));
                    strOutput = "$P" + _numReg + " = " + resultInt;
                    break;
                case "Float":
                    var resultFloat = _floatValues.Get(context.GetChild(0));
                    strOutput = "$P" + _numReg + " = " + resultFloat.ToString(CultureInfo);
                    break;
                case "String":
                    var resultString = _stringValues.Get(context.GetChild(0));
                    strOutput = "$P" + _numReg + " = " + resultString;
                    break;
                case "Dynamic":
                    var resultDynamic = _stringValues.Get(context.GetChild(0));
                    _stringValues.Put(context, resultDynamic);
                    _whichValues.Put(context, typeArg);
                    _stackOutputStreams.Push(outStream);
                    return;
            }

            ps.WriteLine("$P" + _numReg + " = new \"Integer\"");
            ps.WriteLine(strOutput);
            _stringValues.Put(context, "$P" + _numReg);
            _whichValues.Put(context, "Dynamic");
            _numReg++;

            _stackOutputStreams.Push(outStream);
        }

        public override void ExitBreak_expression(RubyParser.Break_expressionContext context)
        {
            var outStream = _stackOutputStreams.Pop();
            var ps = new StreamWriter(outStream) {AutoFlush = true};
            var labelEndCurrentLoop = _stackLoopLabels.Pop();

            ps.WriteLine("goto label_" + labelEndCurrentLoop);

            _stackLoopLabels.Push(labelEndCurrentLoop);
            _stackOutputStreams.Push(outStream);
        }

        public override void ExitLiteral_t(RubyParser.Literal_tContext context)
        {
            _stringValues.Put(context, _stringValues.Get(context.GetChild(0)));
            _whichValues.Put(context, _whichValues.Get(context.GetChild(0)));
        }

        public override void ExitFloat_t(RubyParser.Float_tContext context)
        {
            _floatValues.Put(context, _floatValues.Get(context.GetChild(0)));
            _whichValues.Put(context, _whichValues.Get(context.GetChild(0)));
        }

        public override void ExitInt_t(RubyParser.Int_tContext context)
        {
            _intValues.Put(context, _intValues.Get(context.GetChild(0)));
            _whichValues.Put(context, _whichValues.Get(context.GetChild(0)));
        }

        public override void ExitId(RubyParser.IdContext context)
        {
            _stringValues.Put(context, _stringValues.Get(context.GetChild(0)));
            _whichValues.Put(context, _whichValues.Get(context.GetChild(0)));
        }

        public override void ExitCrlf(RubyParser.CrlfContext context)
        {
            _numStr++;
        }

        public override void VisitTerminal(ITerminalNode node)
        {
            var symbol = node.Symbol;
            switch(symbol.Type) 
            {
                case RubyParser.INT:
                    _intValues.Put(node, int.Parse(symbol.Text));
                    _whichValues.Put(node, "Integer");
                    break;
                case RubyParser.FLOAT:
                    _floatValues.Put(node, float.Parse(symbol.Text, NumberStyles.Any, CultureInfo));
                    _whichValues.Put(node, "Float");
                    break;
                case RubyParser.LITERAL:
                    var strTerminal = symbol.Text;
                    strTerminal = Regex.Replace(strTerminal, "\"$", "");
                    strTerminal = Regex.Replace(strTerminal, "^\"", "");
                    strTerminal = Regex.Replace(strTerminal, "\'$", "");
                    strTerminal = Regex.Replace(strTerminal, "^\'", "");
                    _stringValues.Put(node, strTerminal);
                    _whichValues.Put(node, "String");
                    break;
                case RubyParser.ID:
                    var dynamicTerminal = symbol.Text;
                    _stringValues.Put(node, dynamicTerminal);
                    _whichValues.Put(node, "Dynamic");
                    break;
            }
        }

    }
}