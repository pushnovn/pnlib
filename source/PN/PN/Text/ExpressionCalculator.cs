using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PN.Text
{
    public class ExpressionCalculator
    {
        private class FunctionOrOperation
        {
            internal string Term { get; set; }

            internal int Precedence { get; set; }

            internal string StopCondition { get; set; }

            internal Func<List<double>, double> Calc { get; set; }

            internal int Parameters { get; set; } = 2;

            internal AssociationEnum Association { get; set; }

            internal ContextEnum Context { get; set; }

            internal enum AssociationEnum
            {
                LEFT_TO_RIGHT,
                RIGHT_TO_LEFT
            }

            [Flags]
            internal enum ContextEnum
            {
                NONE = 1,
                STACK = 2,
                POP = 4
            }

            public override string ToString()
            {
                return $"[FunctionOrOperation: Term={Term}, Context={Context}, Association={Association}]";
            }
        }
        
        public ExpressionCalculator(bool debug = false)
        {
            DEBUG = debug;

            #region Brackets
            // left parenthesis
            _functionsOrOperations.Add(new FunctionOrOperation()
            {
                Term = "(",
                Precedence = 1000,
                Association = FunctionOrOperation.AssociationEnum.LEFT_TO_RIGHT,
                Context = FunctionOrOperation.ContextEnum.STACK
                        | FunctionOrOperation.ContextEnum.NONE,
            });

            // right parenthesis
            _functionsOrOperations.Add(new FunctionOrOperation()
            {
                Term = ")",
                Precedence = 1000,
                Association = FunctionOrOperation.AssociationEnum.LEFT_TO_RIGHT,
                Context = FunctionOrOperation.ContextEnum.POP,
                StopCondition = "("
            });
            #endregion

            // division
            _functionsOrOperations.Add(new FunctionOrOperation()
            {
                Term = "/",
                Precedence = 100,
                Association = FunctionOrOperation.AssociationEnum.LEFT_TO_RIGHT,
                Context = FunctionOrOperation.ContextEnum.STACK,
                Calc = (List<double> arg) => arg[1] / arg[0]
            });

            // multiplication
            _functionsOrOperations.Add(new FunctionOrOperation()
            {
                Term = "*",
                Precedence = 100,
                Association = FunctionOrOperation.AssociationEnum.LEFT_TO_RIGHT,
                Context = FunctionOrOperation.ContextEnum.STACK,
                Calc = (List<double> arg) => arg[0] * arg[1]
            });

            /// addition
            _functionsOrOperations.Add(new FunctionOrOperation()
            {
                Term = "+",
                Precedence = 50,
                Association = FunctionOrOperation.AssociationEnum.LEFT_TO_RIGHT,
                Context = FunctionOrOperation.ContextEnum.STACK,
                Calc = (List<double> arg) => arg[0] + arg[1]
            });

            /// subtraction
            _functionsOrOperations.Add(new FunctionOrOperation()
            {
                Term = "-",
                Precedence = 50,
                Association = FunctionOrOperation.AssociationEnum.LEFT_TO_RIGHT,
                Context = FunctionOrOperation.ContextEnum.STACK,
                Calc = (List<double> arg) => arg[1] - arg[0]
            });

            /// power
            _functionsOrOperations.Add(new FunctionOrOperation()
            {
                Term = "^",
                Precedence = 100,
                Association = FunctionOrOperation.AssociationEnum.RIGHT_TO_LEFT,
                Context = FunctionOrOperation.ContextEnum.STACK,
                Calc = (List<double> arg) => Math.Pow(arg[1], arg[0])
            });

            // square root
            _functionsOrOperations.Add(new FunctionOrOperation()
            {
                Term = "SQRT",
                Precedence = 100,
                Association = FunctionOrOperation.AssociationEnum.RIGHT_TO_LEFT,
                Context = FunctionOrOperation.ContextEnum.STACK,
                Calc = (List<double> arg) => Math.Sqrt(arg[0]),
                Parameters = 1
            });

            // 
            _functionsOrOperations.Add(new FunctionOrOperation()
            {
                Term = "ABS",
                Precedence = 110,
                Association = FunctionOrOperation.AssociationEnum.LEFT_TO_RIGHT,
                Context = FunctionOrOperation.ContextEnum.STACK,
                Calc = (List<double> arg) => Math.Abs(arg[0]),
                Parameters = 1
            });
        }

        Dictionary<string, Func<double>> _constants = new Dictionary<string, Func<double>>()
        {
            { "E", () => Math.E },
            { "PI", () => Math.PI },
            { "RAD", () => 180.0 / Math.PI },
        };

        List<FunctionOrOperation> _functionsOrOperations = new List<FunctionOrOperation>();

        Dictionary<string, FunctionOrOperation> _indexedFunctionOrOperation;

        private Dictionary<string, FunctionOrOperation> Indexed
        {
            get
            {
                if (_indexedFunctionOrOperation == null ||
                    _indexedFunctionOrOperation.Count != _functionsOrOperations.Count)
                {
                    _indexedFunctionOrOperation = _functionsOrOperations
                        .ToDictionary(key => key.Term.ToLowerInvariant(), value => value);
                }

                return _indexedFunctionOrOperation;
            }
        }

        readonly bool DEBUG = false;

        public bool TrySolve(string expression, out double result)
        {
            var exprBefore = expression.ToString();

            var parsed = true;
            result = 0.0;
            try
            {
                // remove all spaces
                expression = Regex.Replace(expression, @"\s+", string.Empty);
                // add spaces before and after signal, operators and brackets
                expression = Regex.Replace(expression, @"[\-\+\*\/\(\)\^]", " $& ");
                // adjust unary signals, as before brackets
                expression = Regex.Replace(expression, @"[\-\+](?=\s*\()", @"+ ( $&1 ) * ");
                // adjust unary signals, as before functions
                expression = Regex.Replace(expression, @"[\-\+](?=\s*[a-z]+)", @"+ ( $&1 ) * ", RegexOptions.IgnoreCase);
                // adjust unary signals, as equation beginning
                expression = Regex.Replace(expression, @"^\s*([\-\+])\s*([0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?)", @" $1$2 ");
                // adjust unary signals, as operators and signas followed by number
                expression = Regex.Replace(expression, @"(?<=[\+\/\*\-\^]\s*)([-+])\s*([0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?)", @" $1$2 ");
                // finally, adjust expression to postfix
                var values = ExpressionToPostFix(expression);

                if (DEBUG)
                {
                    Console.WriteLine("prepared:");
                    Console.WriteLine(expression);

                    Console.WriteLine("postfixed:");
                    foreach (var item in values)
                        Console.Write("{0} ", item is FunctionOrOperation ? ((FunctionOrOperation)item).Term : item);
                    Console.WriteLine();
                    Console.WriteLine();
                }

                result = Calculate(values);

            }
            catch (Exception ex)
            {
                parsed = false;
            }

            return parsed;
        }

        List<object> ExpressionToPostFix(string expression)
        {
            var output = new List<object>();
            var stack = new Stack<FunctionOrOperation>();
            var token = new char[] { ' ' };
            var tokens = expression.Split(token, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in tokens)
            {
                if (IsVariableOrConstant(item, out Func<double> value))
                {
                    if (value == null)
                    {
                        output.Add(item);
                    }
                    else
                    {
                        output.Add(value());
                    }

                    continue;
                }

                if (IsFunctionOrOperation(item, out FunctionOrOperation op))
                {
                    if (op.Context.HasFlag(FunctionOrOperation.ContextEnum.STACK))
                    {
                        while (stack.Count > 0 &&
                               !stack.Peek().Context.HasFlag(FunctionOrOperation.ContextEnum.NONE) &&
                               ((op.Association == FunctionOrOperation.AssociationEnum.LEFT_TO_RIGHT
                                  && op.Precedence <= stack.Peek().Precedence)
                                  || op.Association == FunctionOrOperation.AssociationEnum.RIGHT_TO_LEFT
                                  && op.Precedence < stack.Peek().Precedence)
                              )
                        {
                            var poped = stack.Pop();

                            if (!poped.Context.HasFlag(FunctionOrOperation.ContextEnum.NONE))
                            {
                                output.Add(poped);
                            }
                        }

                        stack.Push(op);
                    }
                    else if (op.Context.HasFlag(FunctionOrOperation.ContextEnum.POP))
                    {
                        var found = false;
                        while (stack.Count > 0 && !found)
                        {
                            var poped = stack.Pop();
                            if (Equals(op.StopCondition, poped.Term))
                            {
                                found = true;
                            }
                            else
                            {
                                output.Add(poped);
                            }
                        }
                    }
                }
            }

            while (stack.Count > 0)
            {
                output.Add(stack.Pop());
            }

            return output;
        }

        double Calculate(List<object> output)
        {
            var stack = new Stack<double>();
            foreach (var item in output)
            {
                if (item is FunctionOrOperation)
                {
                    if (DEBUG)
                        Console.WriteLine("Found? FunctionOrOperation.");

                    var op = item as FunctionOrOperation;

                    var parameters = new List<double>();
                    for (int aux = 0; aux < op.Parameters; aux++)
                    {
                        var value = stack.Pop();
                        parameters.Add(value);
                        if (DEBUG) Console.WriteLine("Pop: {0}", value);
                    }

                    var result = op.Calc(parameters);

                    if (DEBUG)
                    {
                        foreach (var parameter in parameters)
                        {
                            Console.Write($"[{parameter}] ");
                        }

                        Console.WriteLine($"{op.Term} = {result}");

                        Console.Write($"Pushing result: {result} => [{result}]");
                        foreach (var stacked in stack)
                            Console.Write($"{stacked} ");
                        Console.WriteLine();
                    }

                    stack.Push(result);

                }
                else
                {
                    if (DEBUG)
                    {
                        Console.WriteLine("Found? VariableOrConstant.");
                        Console.Write("Pushing: {0} => [{0}]", item);
                        foreach (var stacked in stack)
                            Console.Write("{0} ", stacked);
                        Console.WriteLine();
                    }

                    var dbl = 0.0;

                    try
                    {
                        dbl = Convert.ToDouble(item.ToString().Replace(".", ","));
                    }
                    catch
                    {
                        dbl = Convert.ToDouble(item.ToString().Replace(",", "."));
                    }

                    stack.Push(dbl);
                }
            }
            return stack.Pop();
        }

        bool IsFunctionOrOperation(string item, out FunctionOrOperation op)
        {
            op = null;

            return Indexed.TryGetValue(item.ToLowerInvariant(), out op);
        }

        bool IsVariableOrConstant(string item, out Func<double> value)
        {
            value = null;

            if (_constants.TryGetValue(item.ToUpperInvariant(), out value))
            {
                return true;
            }

            if (Regex.IsMatch(item, @"^[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?$"))
            {
                return true;
            }

            return false;
        }
    }
}