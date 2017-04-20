﻿using System;
using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace GraphView
{

    internal abstract class BooleanFunction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        //internal List<string> header { get; set; }      // To be removed.
        public abstract bool Evaluate(RawRecord r);
    }

    //internal abstract class ComparisonBooleanFunction : BooleanFunction
    //{
    //    // To be replaced by BooleanComparisonType
    //    internal enum ComparisonType
    //    {
    //        neq,
    //        eq,
    //        lt,
    //        gt,
    //        gte,
    //        lte
    //    }
    //}

    internal class ComparisonFunction : BooleanFunction
    {
        ScalarFunction firstScalarFunction;
        ScalarFunction secondScalarFunction;
        BooleanComparisonType comparisonType;
        
        public ComparisonFunction(ScalarFunction f1, ScalarFunction f2, BooleanComparisonType comparisonType)
        {
            firstScalarFunction = f1;
            secondScalarFunction = f2;
            this.comparisonType = comparisonType;
        }


        public override bool Evaluate(RawRecord record)
        {
            FieldObject lhs = firstScalarFunction.Evaluate(record);
            FieldObject rhs = secondScalarFunction.Evaluate(record);

            if (lhs == null || rhs == null) {
                return false;
            }

            if (lhs is VertexPropertyField)
            {
                VertexPropertyField vp = (VertexPropertyField)lhs;
                foreach (VertexSinglePropertyField vsp in vp.Multiples.Values)
                {
                    JsonDataType type1 = vsp.JsonDataType;
                    JsonDataType type2 = secondScalarFunction.DataType();

                    JsonDataType targetType = type1 > type2 ? type1 : type2;

                    if (Compare(vsp.ToValue, rhs.ToValue, targetType, this.comparisonType)) {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                JsonDataType type1 = firstScalarFunction.DataType();
                JsonDataType type2 = secondScalarFunction.DataType();

                JsonDataType targetType = type1 > type2 ? type1 : type2;

                string value1 = firstScalarFunction.Evaluate(record)?.ToValue;
                string value2 = secondScalarFunction.Evaluate(record)?.ToValue;

                return Compare(value1, value2, targetType, this.comparisonType);
            }
        }

        public static bool Compare(string value1, string value2, JsonDataType targetType, BooleanComparisonType comparisonType)
        {
            switch (targetType)
            {
                case JsonDataType.Boolean:
                    bool bool_value1, bool_value2;
                    if (bool.TryParse(value1, out bool_value1) && bool.TryParse(value2, out bool_value2))
                    {
                        switch (comparisonType)
                        {
                            case BooleanComparisonType.Equals:
                                return bool_value1 == bool_value2;
                            case BooleanComparisonType.NotEqualToBrackets:
                            case BooleanComparisonType.NotEqualToExclamation:
                                return bool_value1 != bool_value2;
                            default:
                                throw new QueryCompilationException("Operator " + comparisonType.ToString() + " cannot be applied to operands of type \"boolean\".");
                        }
                    }
                    else
                    {
                        return false;
                        //throw new QueryCompilationException(string.Format("Cannot cast \"{0}\" or \"{1}\" to values of type \"boolean\"",
                        //    value1, value2));
                    }
                case JsonDataType.Bytes:
                    switch (comparisonType)
                    {
                        case BooleanComparisonType.Equals:
                            return value1 == value2;
                        case BooleanComparisonType.NotEqualToBrackets:
                        case BooleanComparisonType.NotEqualToExclamation:
                            return value1 != value2;
                        default:
                            return false;
                            //throw new NotImplementedException();
                    }
                case JsonDataType.Int:
                    int int_value1, int_value2;
                    if (int.TryParse(value1, out int_value1) && int.TryParse(value2, out int_value2))
                    {
                        switch (comparisonType)
                        {
                            case BooleanComparisonType.Equals:
                                return int_value1 == int_value2;
                            case BooleanComparisonType.GreaterThan:
                                return int_value1 > int_value2;
                            case BooleanComparisonType.GreaterThanOrEqualTo:
                                return int_value1 >= int_value2;
                            case BooleanComparisonType.LessThan:
                                return int_value1 < int_value2;
                            case BooleanComparisonType.LessThanOrEqualTo:
                                return int_value1 <= int_value2;
                            case BooleanComparisonType.NotEqualToBrackets:
                            case BooleanComparisonType.NotEqualToExclamation:
                                return int_value1 != int_value2;
                            case BooleanComparisonType.NotGreaterThan:
                                return !(int_value1 > int_value2);
                            case BooleanComparisonType.NotLessThan:
                                return !(int_value1 < int_value2);
                            default:
                                throw new QueryCompilationException("Operator " + comparisonType.ToString() + " cannot be applied to operands of type \"int\".");
                        }
                    }
                    else
                    {
                        return false;
                        //throw new QueryCompilationException(string.Format("Cannot cast \"{0}\" or \"{1}\" to values of type \"int\"",
                        //    value1, value2));
                    }
                case JsonDataType.Long:
                    long long_value1, long_value2;
                    if (long.TryParse(value1, out long_value1) && long.TryParse(value2, out long_value2))
                    {
                        switch (comparisonType)
                        {
                            case BooleanComparisonType.Equals:
                                return long_value1 == long_value2;
                            case BooleanComparisonType.GreaterThan:
                                return long_value1 > long_value2;
                            case BooleanComparisonType.GreaterThanOrEqualTo:
                                return long_value1 >= long_value2;
                            case BooleanComparisonType.LessThan:
                                return long_value1 < long_value2;
                            case BooleanComparisonType.LessThanOrEqualTo:
                                return long_value1 <= long_value2;
                            case BooleanComparisonType.NotEqualToBrackets:
                            case BooleanComparisonType.NotEqualToExclamation:
                                return long_value1 != long_value2;
                            case BooleanComparisonType.NotGreaterThan:
                                return !(long_value1 > long_value2);
                            case BooleanComparisonType.NotLessThan:
                                return !(long_value1 < long_value2);
                            default:
                                throw new QueryCompilationException("Operator " + comparisonType.ToString() + " cannot be applied to operands of type \"long\".");
                        }
                    }
                    else
                    {
                        return false;
                        //throw new QueryCompilationException(string.Format("Cannot cast \"{0}\" or \"{1}\" to values of type \"long\"",
                        //    value1, value2));
                    }
                case JsonDataType.Double:
                    double double_value1, double_value2;
                    if (double.TryParse(value1, out double_value1) && double.TryParse(value2, out double_value2))
                    {
                        switch (comparisonType)
                        {
                            case BooleanComparisonType.Equals:
                                return double_value1 == double_value2;
                            case BooleanComparisonType.GreaterThan:
                                return double_value1 > double_value2;
                            case BooleanComparisonType.GreaterThanOrEqualTo:
                                return double_value1 >= double_value2;
                            case BooleanComparisonType.LessThan:
                                return double_value1 < double_value2;
                            case BooleanComparisonType.LessThanOrEqualTo:
                                return double_value1 <= double_value2;
                            case BooleanComparisonType.NotEqualToBrackets:
                            case BooleanComparisonType.NotEqualToExclamation:
                                return double_value1 != double_value2;
                            case BooleanComparisonType.NotGreaterThan:
                                return !(double_value1 > double_value2);
                            case BooleanComparisonType.NotLessThan:
                                return !(double_value1 < double_value2);
                            default:
                                throw new QueryCompilationException("Operator " + comparisonType.ToString() + " cannot be applied to operands of type \"double\".");
                        }
                    }
                    else
                    {
                        return false;
                        //throw new QueryCompilationException(string.Format("Cannot cast \"{0}\" or \"{1}\" to values of type \"double\"",
                        //    value1, value2));
                    }
                case JsonDataType.Float:
                    float float_value1, float_value2;
                    if (float.TryParse(value1, out float_value1) && float.TryParse(value2, out float_value2))
                    {
                        switch (comparisonType)
                        {
                            case BooleanComparisonType.Equals:
                                return float_value1 == float_value2;
                            case BooleanComparisonType.GreaterThan:
                                return float_value1 > float_value2;
                            case BooleanComparisonType.GreaterThanOrEqualTo:
                                return float_value1 >= float_value2;
                            case BooleanComparisonType.LessThan:
                                return float_value1 < float_value2;
                            case BooleanComparisonType.LessThanOrEqualTo:
                                return float_value1 <= float_value2;
                            case BooleanComparisonType.NotEqualToBrackets:
                            case BooleanComparisonType.NotEqualToExclamation:
                                return float_value1 != float_value2;
                            case BooleanComparisonType.NotGreaterThan:
                                return !(float_value1 > float_value2);
                            case BooleanComparisonType.NotLessThan:
                                return !(float_value1 < float_value2);
                            default:
                                throw new QueryCompilationException("Operator " + comparisonType.ToString() + " cannot be applied to operands of type \"float\".");
                        }
                    }
                    else
                    {
                        return false;
                        //throw new QueryCompilationException(string.Format("Cannot cast \"{0}\" or \"{1}\" to values of type \"float\"",
                        //    value1, value2));
                    }
                case JsonDataType.String:
                    switch (comparisonType)
                    {
                        case BooleanComparisonType.Equals:
                            return value1 == value2;
                        case BooleanComparisonType.GreaterThan:
                        case BooleanComparisonType.GreaterThanOrEqualTo:
                            return value1.CompareTo(value2) > 0;
                        case BooleanComparisonType.LessThan:
                        case BooleanComparisonType.LessThanOrEqualTo:
                            return value1.CompareTo(value2) > 0;
                        case BooleanComparisonType.NotEqualToBrackets:
                        case BooleanComparisonType.NotEqualToExclamation:
                            return value1 != value2;
                        case BooleanComparisonType.NotGreaterThan:
                            return value1.CompareTo(value2) <= 0;
                        case BooleanComparisonType.NotLessThan:
                            return value1.CompareTo(value2) >= 0;
                        default:
                            throw new QueryCompilationException("Operator " + comparisonType.ToString() + " cannot be applied to operands of type \"string\".");
                    }
                case JsonDataType.Date:
                    throw new NotImplementedException();
                case JsonDataType.Null:
                    return false;
                default:
                    throw new QueryCompilationException("Unsupported data type.");
            }
        }
    }

    //internal class FieldComparisonFunction : ComparisonBooleanFunction
    //{
    //    //internal int LhsFieldIndex;
    //    //internal int RhsFieldIndex;
    //    internal string LhsFieldName;
    //    internal string RhsFieldName;
    //    internal ComparisonType type;

    //    //internal FieldComparisonFunction(int lhs, int rhs, ComparisonType pType)
    //    //{
    //    //    LhsFieldIndex = lhs;
    //    //    RhsFieldIndex = rhs;
    //    //    type = pType;
    //    //}

    //    public FieldComparisonFunction(string lhs, string rhs, ComparisonType pType)
    //    {
    //        LhsFieldName = lhs;
    //        RhsFieldName = rhs;
    //        type = pType;
    //    }
    //    public override bool Evaluate(RawRecord r)
    //    {
    //        var lhsIndex = header.IndexOf(LhsFieldName);
    //        var rhsIndex = header.IndexOf(RhsFieldName);
    //        switch (type)
    //        {
    //            case ComparisonType.eq:
    //                return r.RetriveData(lhsIndex) == r.RetriveData(rhsIndex);
    //            case ComparisonType.neq:
    //                return r.RetriveData(lhsIndex) != r.RetriveData(rhsIndex);
    //            case ComparisonType.lt:
    //                return double.Parse(r.RetriveData(lhsIndex).ToString()) < double.Parse(r.RetriveData(rhsIndex).ToString());
    //            case ComparisonType.gt:
    //                return double.Parse(r.RetriveData(lhsIndex).ToString()) > double.Parse(r.RetriveData(rhsIndex).ToString());
    //            case ComparisonType.gte:
    //                return double.Parse(r.RetriveData(lhsIndex).ToString()) >= double.Parse(r.RetriveData(rhsIndex).ToString());
    //            case ComparisonType.lte:
    //                return double.Parse(r.RetriveData(lhsIndex).ToString()) <= double.Parse(r.RetriveData(rhsIndex).ToString());
    //            default:
    //                return false;
    //        }
            
    //    }
    //}

    internal enum BooleanBinaryFunctionType
    {
        And,
        Or,
    }

    internal class BooleanBinaryFunction : BooleanFunction
    {
        private BooleanBinaryFunctionType type;
        private BooleanFunction lhs;
        private BooleanFunction rhs;
        internal BooleanBinaryFunction(BooleanFunction plhs, BooleanFunction prhs, BooleanBinaryFunctionType ptype)
        {
            lhs = plhs;
            rhs = prhs;
            type = ptype;
        }
        public override bool Evaluate(RawRecord r)
        {
            if (type == BooleanBinaryFunctionType.And) return lhs.Evaluate(r) && rhs.Evaluate(r);
            if (type == BooleanBinaryFunctionType.Or) return lhs.Evaluate(r) || rhs.Evaluate(r);
            return false;
        }
    }

    internal class ExistsFunction : BooleanFunction
    {
        // When a subquery is compiled, the tuple from the outer context
        // is injected into the subquery through a constant-source scan, 
        // which is in a Cartesian product with the operators compiled from the query. 
        private GraphViewExecutionOperator subqueryOp;
        private ConstantSourceOperator constantSourceOp;

        public ExistsFunction(GraphViewExecutionOperator subqueryOp, ConstantSourceOperator constantSourceOp)
        {
            this.subqueryOp = subqueryOp;
            this.constantSourceOp = constantSourceOp;
        }

        public override bool Evaluate(RawRecord r)
        {
            constantSourceOp.ConstantSource = r;
            subqueryOp.ResetState();
            RawRecord firstResult = subqueryOp.Next();
            subqueryOp.Close();

            return firstResult != null;
        }
    }

    internal class BooleanNotFunction : BooleanFunction
    {
        private BooleanFunction _booleanFunction;

        public BooleanNotFunction(BooleanFunction booleanFunction)
        {
            _booleanFunction = booleanFunction;
        }

        public override bool Evaluate(RawRecord r)
        {
            return !_booleanFunction.Evaluate(r);
        }
    }
}
