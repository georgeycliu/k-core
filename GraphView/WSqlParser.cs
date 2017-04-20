﻿// GraphView
// 
// Copyright (c) 2015 Microsoft Corporation
// 
// All rights reserved. 
// 
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace GraphView
{
    public class WSqlParser
    {
        internal TSql110Parser tsqlParser;
        private IList<TSqlParserToken> _tokens;

        public WSqlParser()
        {
            tsqlParser = new TSql110Parser(true);
        }

        public WSqlFragment Parse(IList<TSqlParserToken> tokens, out IList<ParseError> errors)
        {
            var fragment = tsqlParser.Parse(tokens, out errors);
            if (errors.Count > 0)
            {
                return null;
            }
            _tokens = tokens;
            return ConvertFragment(fragment);

        }

        public WSqlFragment Parse(TextReader queryInput, out IList<ParseError> errors)
        {
            var fragment = tsqlParser.Parse(queryInput, out errors);
            if (errors.Count > 0)
            {
                return null;
            }

            return ConvertFragment(fragment);
        }        

        private WSqlFragment ConvertFragment(TSqlFragment fragment)
        {

            var tscript = fragment as TSqlScript;

            var wscript = new WSqlScript
            {
                FirstTokenIndex = tscript.FirstTokenIndex,
                LastTokenIndex = tscript.LastTokenIndex,
                Batches = tscript.Batches == null ? null : new List<WSqlBatch>(tscript.Batches.Count),
            };

            foreach (var tbatch in tscript.Batches)
            {
                var wbatch = new WSqlBatch
                {
                    FirstTokenIndex = tbatch.FirstTokenIndex,
                    LastTokenIndex = tbatch.LastTokenIndex,
                    Statements = new List<WSqlStatement>(tbatch.Statements.Count),
                };

                foreach (var wstat in tbatch.Statements.Select(ParseStatement))
                {
                    wbatch.Statements.Add(wstat);
                }

                wscript.Batches.Add(wbatch);
            }

            return wscript;
        }

        private QuoteType ParseQuoteType(Microsoft.SqlServer.TransactSql.ScriptDom.QuoteType qt)
        {
            switch (qt)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.QuoteType.DoubleQuote:
                    return QuoteType.DoubleQuote;
                case Microsoft.SqlServer.TransactSql.ScriptDom.QuoteType.SquareBracket:
                    return QuoteType.SquareBracket;
                default:
                    return QuoteType.NotQuoted;
            }
        }

        private Identifier ParseIdentifier(Microsoft.SqlServer.TransactSql.ScriptDom.Identifier id)
        {
            return new Identifier()
            {
                Value = id.Value,
                QuoteType = ParseQuoteType(id.QuoteType),
                FirstTokenIndex = id.FirstTokenIndex,
                LastTokenIndex = id.LastTokenIndex
            };
        }

        private BinaryExpressionType ParseBinaryExpressionType(
            Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpressionType type)
        {
            switch (type)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpressionType.Add:
                    return BinaryExpressionType.Add;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpressionType.BitwiseAnd:
                    return BinaryExpressionType.BitwiseAnd;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpressionType.BitwiseOr:
                    return BinaryExpressionType.BitwiseOr;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpressionType.BitwiseXor:
                    return BinaryExpressionType.BitwiseXor;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpressionType.Divide:
                    return BinaryExpressionType.Divide;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpressionType.Modulo:
                    return BinaryExpressionType.Modulo;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpressionType.Multiply:
                    return BinaryExpressionType.Multiply;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryExpressionType.Subtract:
                    return BinaryExpressionType.Subtract;
                default:
                    throw new NotImplementedException();
            }
        }

        private BinaryQueryExpressionType ParseBinaryQueryExpressionType (
            Microsoft.SqlServer.TransactSql.ScriptDom.BinaryQueryExpressionType type)
        {
            switch (type)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryQueryExpressionType.Except:
                    return BinaryQueryExpressionType.Except;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryQueryExpressionType.Intersect:
                    return BinaryQueryExpressionType.Intersect;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryQueryExpressionType.Union:
                    return BinaryQueryExpressionType.Union;
                default:
                    throw new NotImplementedException();
            }
        }

        private BooleanBinaryExpressionType ParseBooleanBinaryExpressionType (
            Microsoft.SqlServer.TransactSql.ScriptDom.BooleanBinaryExpressionType type)
        {
            switch (type)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanBinaryExpressionType.And:
                    return BooleanBinaryExpressionType.And;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanBinaryExpressionType.Or:
                    return BooleanBinaryExpressionType.Or;
                default:
                    throw new NotImplementedException();
            }
        }

        private ParameterModifier ParseParameterModifier(
            Microsoft.SqlServer.TransactSql.ScriptDom.ParameterModifier modifier)
        {
            switch (modifier)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.ParameterModifier.None:
                    return ParameterModifier.None;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ParameterModifier.Output:
                    return ParameterModifier.Output;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ParameterModifier.ReadOnly:
                    return ParameterModifier.ReadOnly;
                default:
                    throw new NotImplementedException();
            }
        }

        private SortOrder ParseSortOrder(Microsoft.SqlServer.TransactSql.ScriptDom.SortOrder so)
        {
            switch(so)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.SortOrder.Ascending:
                    return SortOrder.Ascending;
                case Microsoft.SqlServer.TransactSql.ScriptDom.SortOrder.Descending:
                    return SortOrder.Descending;
                default:
                    return SortOrder.NotSpecified;
            }
        }

        private AssignmentKind ParseAssignmentKind(Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind kind)
        {
            switch(kind)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind.AddEquals:
                    return AssignmentKind.AddEquals;
                case Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind.BitwiseAndEquals:
                    return AssignmentKind.BitwiseAndEquals;
                case Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind.BitwiseOrEquals:
                    return AssignmentKind.BitwiseOrEquals;
                case Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind.BitwiseXorEquals:
                    return AssignmentKind.BitwiseXorEquals;
                case Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind.DivideEquals:
                    return AssignmentKind.DivideEquals;
                case Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind.Equals:
                    return AssignmentKind.Equals;
                case Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind.ModEquals:
                    return AssignmentKind.ModEquals;
                case Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind.MultiplyEquals:
                    return AssignmentKind.MultiplyEquals;
                case Microsoft.SqlServer.TransactSql.ScriptDom.AssignmentKind.SubtractEquals:
                    return AssignmentKind.SubtractEquals;
                default:
                    throw new NotImplementedException();
            }
        }

        private UniqueRowFilter ParseUniqueRowFilter(Microsoft.SqlServer.TransactSql.ScriptDom.UniqueRowFilter filter)
        {
            switch(filter)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.UniqueRowFilter.All:
                    return UniqueRowFilter.All;
                case Microsoft.SqlServer.TransactSql.ScriptDom.UniqueRowFilter.Distinct:
                    return UniqueRowFilter.Distinct;
                default:
                    return UniqueRowFilter.NotSpecified;
            }
        }

        private UnaryExpressionType ParseUnaryExpressionType(
            Microsoft.SqlServer.TransactSql.ScriptDom.UnaryExpressionType type)
        {
            switch(type)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.UnaryExpressionType.BitwiseNot:
                    return UnaryExpressionType.BitwiseNot;
                case Microsoft.SqlServer.TransactSql.ScriptDom.UnaryExpressionType.Negative:
                    return UnaryExpressionType.Negative;
                case Microsoft.SqlServer.TransactSql.ScriptDom.UnaryExpressionType.Positive:
                    return UnaryExpressionType.Positive;
                default:
                    throw new NotImplementedException();
            }
        }

        private QualifiedJoinType ParseQualifiedJoinType (
            Microsoft.SqlServer.TransactSql.ScriptDom.QualifiedJoinType jt)
        {
            switch(jt)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.QualifiedJoinType.FullOuter:
                    return QualifiedJoinType.FullOuter;
                case Microsoft.SqlServer.TransactSql.ScriptDom.QualifiedJoinType.Inner:
                    return QualifiedJoinType.Inner;
                case Microsoft.SqlServer.TransactSql.ScriptDom.QualifiedJoinType.LeftOuter:
                    return QualifiedJoinType.LeftOuter;
                case Microsoft.SqlServer.TransactSql.ScriptDom.QualifiedJoinType.RightOuter:
                    return QualifiedJoinType.RightOuter;
                default:
                    throw new NotImplementedException();
            }
        }

        private UnqualifiedJoinType ParseUnqualifiedJoinType (
            Microsoft.SqlServer.TransactSql.ScriptDom.UnqualifiedJoinType ujt)
        {
            switch (ujt)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.UnqualifiedJoinType.CrossApply:
                    return UnqualifiedJoinType.CrossApply;
                case Microsoft.SqlServer.TransactSql.ScriptDom.UnqualifiedJoinType.CrossJoin:
                    return UnqualifiedJoinType.CrossJoin;
                case Microsoft.SqlServer.TransactSql.ScriptDom.UnqualifiedJoinType.OuterApply:
                    return UnqualifiedJoinType.OuterApply;
                default:
                    throw new NotImplementedException();
            }
        }

        private SubqueryComparisonPredicateType ParseSubqueryComparisonPredicateType(
            Microsoft.SqlServer.TransactSql.ScriptDom.SubqueryComparisonPredicateType type)
        {
            switch (type)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.SubqueryComparisonPredicateType.All:
                    return SubqueryComparisonPredicateType.All;
                case Microsoft.SqlServer.TransactSql.ScriptDom.SubqueryComparisonPredicateType.Any:
                    return SubqueryComparisonPredicateType.Any;
                case Microsoft.SqlServer.TransactSql.ScriptDom.SubqueryComparisonPredicateType.None:
                    return SubqueryComparisonPredicateType.None;
                default:
                    throw new NotImplementedException();
            }
        }

        private JoinHint ParseJoinHint(
            Microsoft.SqlServer.TransactSql.ScriptDom.JoinHint jh)
        {
            switch (jh)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.JoinHint.Hash:
                    return JoinHint.Hash;
                case Microsoft.SqlServer.TransactSql.ScriptDom.JoinHint.Loop:
                    return JoinHint.Loop;
                case Microsoft.SqlServer.TransactSql.ScriptDom.JoinHint.Merge:
                    return JoinHint.Merge;
                case Microsoft.SqlServer.TransactSql.ScriptDom.JoinHint.None:
                    return JoinHint.None;
                case Microsoft.SqlServer.TransactSql.ScriptDom.JoinHint.Remote:
                    return JoinHint.Remote;
                default:
                    throw new NotImplementedException();
            }
        }

        private BooleanComparisonType ParseBooleanComparisonType(
            Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType type)
        {
            switch (type)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.Equals:
                    return BooleanComparisonType.Equals;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.GreaterThan:
                    return BooleanComparisonType.GreaterThan;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.GreaterThanOrEqualTo:
                    return BooleanComparisonType.GreaterThanOrEqualTo;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.LeftOuterJoin:
                    return BooleanComparisonType.LeftOuterJoin;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.LessThan:
                    return BooleanComparisonType.LessThan;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.LessThanOrEqualTo:
                    return BooleanComparisonType.LessThanOrEqualTo;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.NotEqualToBrackets:
                    return BooleanComparisonType.NotEqualToBrackets;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.NotEqualToExclamation:
                    return BooleanComparisonType.NotEqualToExclamation;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.NotGreaterThan:
                    return BooleanComparisonType.NotGreaterThan;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.NotLessThan:
                    return BooleanComparisonType.NotLessThan;
                case Microsoft.SqlServer.TransactSql.ScriptDom.BooleanComparisonType.RightOuterJoin:
                    return BooleanComparisonType.RightOuterJoin;
                default:
                    throw new NotImplementedException();
            }
        }

        private ParameterModifier Parse(Microsoft.SqlServer.TransactSql.ScriptDom.ParameterModifier pm)
        {
            switch(pm)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.ParameterModifier.Output:
                    return ParameterModifier.Output;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ParameterModifier.ReadOnly:
                    return ParameterModifier.ReadOnly;
                default:
                    return ParameterModifier.None;
            }
        }

        private WFunctionReturnType ParseFunctionReturnType (FunctionReturnType returnType)
        {
            switch(returnType.GetType().Name)
            {
                case "WScalarFunctionReturnType":
                    return new WScalarFunctionReturnType();
                case "WTableValuedFunctionReturnType":
                    return new WTableValuedFunctionReturnType();
                case "WSelectFunctionReturnType":
                    return new WSelectFunctionReturnType();
                default:
                    throw new NotImplementedException();
            }
        }

        private WSqlStatement ParseStatement(TSqlStatement tsqlStat)
        {
            if (tsqlStat == null)
            {
                return null;
            }

            WSqlStatement wstat;

            switch (tsqlStat.GetType().Name)
            {
                case "SelectStatement":
                    {
                        var sel = tsqlStat as SelectStatement;
                        WSelectStatement wselstat = new WSelectStatement
                        {
                            FirstTokenIndex = sel.FirstTokenIndex,
                            LastTokenIndex = sel.LastTokenIndex,
                            Into = ParseSchemaObjectName(sel.Into),
                            OptimizerHints = sel.OptimizerHints,
                            QueryExpr = ParseSelectQueryStatement(sel.QueryExpression)

                        };
                        wstat = wselstat;

                        //wstat = ParseSelectQueryStatement(sel.QueryExpression);
                        break;
                    }
                case "CreateFunctionStatement":
                    {
                        var creat = tsqlStat as CreateFunctionStatement;
                        var wcreat = new WCreateFunctionStatement
                        {
                            Parameters = new List<WProcedureParameter>(creat.Parameters.Select(e => new WProcedureParameter()
                            {
                                DataType = ParseDataType(e.DataType),
                                IsVarying = e.IsVarying,
                                Modifier = ParseParameterModifier(e.Modifier),
                                Value = ParseScalarExpression(e.Value),
                                VariableName = ParseIdentifier(e.VariableName),
                                FirstTokenIndex = e.FirstTokenIndex,
                                LastTokenIndex = e.LastTokenIndex
                            })),
                            ReturnType = ParseFunctionReturnType(creat.ReturnType),
                            StatementList = new List<WSqlStatement>(creat.StatementList.Statements.Count),
                            FirstTokenIndex = creat.FirstTokenIndex,
                            LastTokenIndex = creat.LastTokenIndex,
                            Name = ParseSchemaObjectName(creat.Name)
                        };

                        foreach (var stat in creat.StatementList.Statements)
                        {
                            wcreat.StatementList.Add(ParseStatement(stat));
                        }

                        wstat = wcreat;
                        break;
                    }
                case "BeginEndBlockStatement":
                    {
                        var bestat = tsqlStat as BeginEndBlockStatement;
                        var wbestat = new WBeginEndBlockStatement
                        {
                            StatementList = new List<WSqlStatement>(bestat.StatementList.Statements.Count),
                            FirstTokenIndex = bestat.FirstTokenIndex,
                            LastTokenIndex = bestat.LastTokenIndex
                        };

                        foreach (var pstat in bestat.StatementList.Statements.Select(ParseStatement))
                        {
                            wbestat.StatementList.Add(pstat);
                        }

                        wstat = wbestat;
                        break;
                    }
                case "UpdateStatement":
                    {
                        var upd = tsqlStat as UpdateStatement;
                        wstat = ParseUpdateStatement(upd.UpdateSpecification);
                        break;
                    }
                case "DeleteStatement":
                    {
                        var del = tsqlStat as DeleteStatement;
                        wstat = ParseDeleteStatement(del.DeleteSpecification);
                        break;
                    }
                case "InsertStatement":
                    {
                        var ins = tsqlStat as InsertStatement;
                        wstat = ParseInsertStatement(ins.InsertSpecification);
                        break;
                    }
                case "CreateTableStatement":
                    {
                        var cts = tsqlStat as CreateTableStatement;
                        var wcstat = new WCreateTableStatement
                        {
                            FirstTokenIndex = cts.FirstTokenIndex,
                            LastTokenIndex = cts.LastTokenIndex,
                            Definition = ParseTableDefinition(cts.Definition),
                            SchemaObjectName = ParseSchemaObjectName(cts.SchemaObjectName),
                        };
                        wstat = wcstat;
                        break;
                    }
                case "DropTableStatement":
                    {
                        var dts = tsqlStat as DropTableStatement;
                        var wdstat = new WDropTableStatement
                        {
                            FirstTokenIndex = dts.FirstTokenIndex,
                            LastTokenIndex = dts.LastTokenIndex,
                        };
                        if (dts.Objects != null)
                        {
                            wdstat.Objects = new List<WSchemaObjectName>();
                            foreach (var obj in dts.Objects)
                            {
                                wdstat.Objects.Add(ParseSchemaObjectName(obj));
                            }
                        }
                        wstat = wdstat;
                        break;
                    }
                case "CreateViewStatement":
                {
                    var cvs = tsqlStat as CreateViewStatement;
                        
                    var wcvs = new WCreateViewStatement
                    {
                        Columns = new List<Identifier>(cvs.Columns.Select(e => 
                            new Identifier() { Value = e.Value, QuoteType = ParseQuoteType(e.QuoteType) })),
                        FirstTokenIndex = cvs.FirstTokenIndex,
                        LastTokenIndex = cvs.LastTokenIndex,
                        SchemaObjectName = ParseSchemaObjectName(cvs.SchemaObjectName),
                        SelectStatement = ParseSelectQueryStatement(cvs.SelectStatement.QueryExpression),
                        ViewOptions = cvs.ViewOptions,
                        WithCheckOption = cvs.WithCheckOption
                    };
                    wstat = wcvs;
                    break;
                }
                case "BeginTransactionStatement":
                    {
                        var beginTranStat = tsqlStat as BeginTransactionStatement;
                        wstat = new WBeginTransactionStatement
                        {
                            Name = ParseIdentifierOrValueExpression(beginTranStat.Name),
                            Distributed = beginTranStat.Distributed,
                            FirstTokenIndex = beginTranStat.FirstTokenIndex,
                            LastTokenIndex = beginTranStat.LastTokenIndex
                        };
                        break;
                    }
                case "CommitTransactionStatement":
                    {

                        var commitTranStat = tsqlStat as CommitTransactionStatement;
                        wstat = new WCommitTransactionStatement
                        {
                            Name = ParseIdentifierOrValueExpression(commitTranStat.Name),
                            FirstTokenIndex = commitTranStat.FirstTokenIndex,
                            LastTokenIndex = commitTranStat.LastTokenIndex
                        };
                        break;
                    }
                case "RollbackTransactionStatement":
                    {

                        var rollbackTranStat = tsqlStat as RollbackTransactionStatement;
                        wstat = new WRollbackTransactionStatement
                        {
                            Name = ParseIdentifierOrValueExpression(rollbackTranStat.Name),
                            FirstTokenIndex = rollbackTranStat.FirstTokenIndex,
                            LastTokenIndex = rollbackTranStat.LastTokenIndex
                        };
                        break;
                    }
                case "SaveTransactionStatement":
                    {

                        var saveTranStat = tsqlStat as SaveTransactionStatement;
                        wstat = new WSaveTransactionStatement
                        {
                            Name = ParseIdentifierOrValueExpression(saveTranStat.Name),
                            FirstTokenIndex = saveTranStat.FirstTokenIndex,
                            LastTokenIndex = saveTranStat.LastTokenIndex
                        };
                        break;
                    }
                case "CreateProcedureStatement":
                    {
                        var creat = tsqlStat as CreateProcedureStatement;
                        var wcreat = new WCreateProcedureStatement
                        {
                            IsForReplication = creat.IsForReplication,
                            Parameters = new List<WProcedureParameter>(creat.Parameters.Select(e => new WProcedureParameter()
                            {
                                DataType = ParseDataType(e.DataType),
                                IsVarying = e.IsVarying,
                                Modifier = ParseParameterModifier(e.Modifier),
                                Value = ParseScalarExpression(e.Value),
                                VariableName = ParseIdentifier(e.VariableName),
                                FirstTokenIndex = e.FirstTokenIndex,
                                LastTokenIndex = e.LastTokenIndex
                            })),
                            StatementList = new List<WSqlStatement>(creat.StatementList.Statements.Count),
                            FirstTokenIndex = creat.FirstTokenIndex,
                            LastTokenIndex = creat.LastTokenIndex,
                            ProcedureReference = new WProcedureReference
                            {
                                Name = ParseSchemaObjectName(creat.ProcedureReference.Name),
                                Number = new WValueExpression()
                                {
                                    Value = creat.ProcedureReference.Number.Value,
                                    FirstTokenIndex = creat.ProcedureReference.Number.FirstTokenIndex,
                                    LastTokenIndex = creat.ProcedureReference.Number.LastTokenIndex
                                }
                            }
                        };

                        foreach (var stat in creat.StatementList.Statements)
                        {
                            wcreat.StatementList.Add(ParseStatement(stat));
                        }

                        wstat = wcreat;
                        break;
                    }
                case "DropProcedureStatement":
                    {
                        var dts = tsqlStat as DropProcedureStatement;
                        var wdstat = new WDropProcedureStatement
                        {
                            FirstTokenIndex = dts.FirstTokenIndex,
                            LastTokenIndex = dts.LastTokenIndex,
                        };
                        if (dts.Objects != null)
                        {
                            wdstat.Objects = new List<WSchemaObjectName>();
                            foreach (var obj in dts.Objects)
                            {
                                wdstat.Objects.Add(ParseSchemaObjectName(obj));
                            }
                        }
                        wstat = wdstat;
                        break;
                    }
                case "WhileStatement":
                    {
                        var bestat = tsqlStat as WhileStatement;
                        var wbestat = new WWhileStatement()
                        {
                            Predicate = ParseBooleanExpression(bestat.Predicate),
                            Statement = ParseStatement(bestat.Statement),
                            FirstTokenIndex = bestat.FirstTokenIndex,
                            LastTokenIndex = bestat.LastTokenIndex
                        };


                        wstat = wbestat;
                        break;
                    }
                case "IfStatement":
                    {
                        var ifSt = tsqlStat as IfStatement;

                        wstat = new WIfStatement()
                        {
                            Predicate = ParseBooleanExpression(ifSt.Predicate),
                            ThenStatement = ParseStatement(ifSt.ThenStatement),
                            ElseStatement = ParseStatement(ifSt.ElseStatement),
                            FirstTokenIndex = ifSt.FirstTokenIndex,
                            LastTokenIndex = ifSt.LastTokenIndex
                        };

                        break;
                    }
                case "DeclareVariableStatement":
                {
                    var dvstat = tsqlStat as DeclareVariableStatement;
                    wstat = new WDeclareVariableStatement
                    {
                        Statement = dvstat
                    };
                    break;
                }
                case "SetVariableStatement":
                {
                    var svs = tsqlStat as SetVariableStatement;
                        wstat = new WSetVariableStatement()
                        {
                            Expression = ParseScalarExpression(svs.Expression),
                            Identifier = new Identifier()
                            {
                                Value = svs.Identifier.Value,
                                QuoteType = ParseQuoteType(svs.Identifier.QuoteType),
                                FirstTokenIndex = svs.Identifier.FirstTokenIndex,
                                LastTokenIndex = svs.Identifier.LastTokenIndex
                            },
                        FirstTokenIndex = svs.FirstTokenIndex,
                        LastTokenIndex =  svs.LastTokenIndex,
                        Variable = ParseVariableReference(svs.Variable)
                    };
                    
                    break;
                }
                default:
                    {
                        wstat = new WSqlUnknownStatement(tsqlStat)
                        {
                            FirstTokenIndex = tsqlStat.FirstTokenIndex,
                            LastTokenIndex = tsqlStat.LastTokenIndex
                        };

                        break;
                    }
            }

            return wstat;
        }

        private WTableDefinition ParseTableDefinition(TableDefinition tableDef)
        {
            if (tableDef == null)
                return null;
            var wTableDef = new WTableDefinition
            {
                FirstTokenIndex = tableDef.FirstTokenIndex,
                LastTokenIndex = tableDef.LastTokenIndex,
            };

            if (tableDef.ColumnDefinitions != null)
            {
                wTableDef.ColumnDefinitions = new List<WColumnDefinition>(tableDef.ColumnDefinitions.Count);
                foreach (var colDef in tableDef.ColumnDefinitions)
                    wTableDef.ColumnDefinitions.Add(ParseColumnDefinition(colDef));
            }

            if (tableDef.TableConstraints != null)
            {
                wTableDef.TableConstraints = new List<WConstraintDefinition>(tableDef.TableConstraints.Count);
                foreach (var tableCon in tableDef.TableConstraints)
                    wTableDef.TableConstraints.Add(ParseConstraintDefinition(tableCon));
            }

            if (tableDef.Indexes != null)
            {
                wTableDef.Indexes = new List<WIndexDefinition>(tableDef.Indexes.Count);
                foreach (var idx in tableDef.Indexes)
                    wTableDef.Indexes.Add(ParseIndexDefinition(idx));
            }
            return wTableDef;
        }

        private ColumnType ParseColumnType(Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType ct)
        {
            switch (ct)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType.IdentityCol:
                    return ColumnType.IdentityCol;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType.PseudoColumnAction:
                    return ColumnType.PseudoColumnAction;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType.PseudoColumnCuid:
                    return ColumnType.PseudoColumnCuid;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType.PseudoColumnIdentity:
                    return ColumnType.PseudoColumnIdentity;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType.PseudoColumnRowGuid:
                    return ColumnType.PseudoColumnRowGuid;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType.Regular:
                    return ColumnType.Regular;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType.RowGuidCol:
                    return ColumnType.RowGuidCol;
                case Microsoft.SqlServer.TransactSql.ScriptDom.ColumnType.Wildcard:
                    return ColumnType.Wildcard;
                default:
                    throw new NotImplementedException();
            }
        }

        private WColumnDefinition ParseColumnDefinition(ColumnDefinition columnDef)
        {
            if (columnDef == null)
                return null;
            var wColumnDef = new WColumnDefinition
            {
                FirstTokenIndex = columnDef.FirstTokenIndex,
                LastTokenIndex = columnDef.LastTokenIndex,
                ColumnIdentifier = new Identifier()
                {
                    Value = columnDef.ColumnIdentifier.Value,
                    QuoteType = ParseQuoteType(columnDef.ColumnIdentifier.QuoteType),
                    FirstTokenIndex = columnDef.ColumnIdentifier.FirstTokenIndex,
                    LastTokenIndex = columnDef.ColumnIdentifier.LastTokenIndex
                },
                DataType = ParseDataType(columnDef.DataType),
                // Collation = columnDef.Collation,
                ComputedColumnExpression = ParseScalarExpression(columnDef.ComputedColumnExpression),
                StorageOptions = columnDef.StorageOptions,
                Index = ParseIndexDefinition(columnDef.Index),
            };
            if (columnDef.Constraints != null)
            {
                wColumnDef.Constraints = new List<WConstraintDefinition>(columnDef.Constraints.Count);
                foreach (var con in columnDef.Constraints)
                    wColumnDef.Constraints.Add(ParseConstraintDefinition(con));
            }
            if (columnDef.IdentityOptions != null)
                wColumnDef.IdentityOptions = new WIdentityOptions
                {
                    FirstTokenIndex = columnDef.IdentityOptions.FirstTokenIndex,
                    LastTokenIndex = columnDef.IdentityOptions.LastTokenIndex,
                    IdentitySeed = ParseScalarExpression(columnDef.IdentityOptions.IdentitySeed),
                    IdentityIncrement = ParseScalarExpression(columnDef.IdentityOptions.IdentityIncrement),
                    IsIdentityNotForReplication = columnDef.IdentityOptions.IsIdentityNotForReplication,
                };
            return wColumnDef;
        }

        private WConstraintDefinition ParseConstraintDefinition(ConstraintDefinition consDef)
        {
            if (consDef == null)
                return null;
            WConstraintDefinition wConsDef = null;
            switch (consDef.GetType().Name)
            {
                case "CheckConstraintDefinition":
                    {
                        var checkConsDef = consDef as CheckConstraintDefinition;
                        wConsDef = new WCheckConstraintDefinition
                        {
                            ConstraintIdentifier = new Identifier()
                            {
                                Value = checkConsDef.ConstraintIdentifier.Value,
                                QuoteType = ParseQuoteType(checkConsDef.ConstraintIdentifier.QuoteType),
                                FirstTokenIndex = checkConsDef.ConstraintIdentifier.FirstTokenIndex,
                                LastTokenIndex = checkConsDef.ConstraintIdentifier.LastTokenIndex
                            },
                            CheckCondition = ParseBooleanExpression(checkConsDef.CheckCondition),
                            NotForReplication = checkConsDef.NotForReplication
                        };
                        break;
                    }
                case "DefaultConstraintDefinition":
                    {
                        var defaultConsDef = consDef as DefaultConstraintDefinition;
                        wConsDef = new WDefaultConstraintDefinition
                        {
                            ConstraintIdentifier = new Identifier()
                            {
                                Value = defaultConsDef.ConstraintIdentifier.Value,
                                QuoteType = ParseQuoteType(defaultConsDef.ConstraintIdentifier.QuoteType),
                                FirstTokenIndex = defaultConsDef.ConstraintIdentifier.FirstTokenIndex,
                                LastTokenIndex = defaultConsDef.ConstraintIdentifier.LastTokenIndex
                            },
                            Expression = ParseScalarExpression(defaultConsDef.Expression),
                            Column = new Identifier()
                            {
                                Value = defaultConsDef.Column.Value,
                                QuoteType = ParseQuoteType(defaultConsDef.Column.QuoteType),
                                FirstTokenIndex = defaultConsDef.Column.FirstTokenIndex,
                                LastTokenIndex = defaultConsDef.Column.LastTokenIndex
                            },
                            WithValues = defaultConsDef.WithValues,
                        };
                        break;
                    }
                case "ForeignKeyConstraintDefinition":
                    {
                        var foreignConsDef = consDef as ForeignKeyConstraintDefinition;
                        var wForeignConsDef = new WForeignKeyConstraintDefinition
                        {
                            ConstraintIdentifier = new Identifier()
                            {
                                Value = foreignConsDef.ConstraintIdentifier.Value,
                                QuoteType = ParseQuoteType(foreignConsDef.ConstraintIdentifier.QuoteType),
                                FirstTokenIndex = foreignConsDef.ConstraintIdentifier.FirstTokenIndex,
                                LastTokenIndex = foreignConsDef.ConstraintIdentifier.LastTokenIndex
                            }, 
                            ReferenceTableName = ParseSchemaObjectName(foreignConsDef.ReferenceTableName),
                            DeleteAction = foreignConsDef.DeleteAction,
                            UpdateAction = foreignConsDef.UpdateAction,
                            NotForReplication = foreignConsDef.NotForReplication,
                        };
                        if (foreignConsDef.Columns != null)
                        {
                            wForeignConsDef.Columns = new List<Identifier>(foreignConsDef.Columns.Select(e => new Identifier()
                            {
                                Value = e.Value,
                                QuoteType = ParseQuoteType(e.QuoteType),
                                FirstTokenIndex = e.FirstTokenIndex,
                                LastTokenIndex = e.LastTokenIndex
                            }));
                        }

                        if (foreignConsDef.ReferencedTableColumns != null)
                        {
                            wForeignConsDef.ReferencedTableColumns = new List<Identifier>(foreignConsDef.ReferencedTableColumns.Select(e => new Identifier()
                            {
                                Value = e.Value,
                                QuoteType = ParseQuoteType(e.QuoteType),
                                FirstTokenIndex = e.FirstTokenIndex,
                                LastTokenIndex = e.LastTokenIndex
                            }));
                        }
                        wConsDef = wForeignConsDef;

                        break;
                    }
                case "NullableConstraintDefinition":
                    {
                        var nullConsDef = consDef as NullableConstraintDefinition;
                        wConsDef = new WNullableConstraintDefinition
                        {
                            ConstraintIdentifier = new Identifier()
                            {
                                Value = nullConsDef.ConstraintIdentifier.Value,
                                QuoteType = ParseQuoteType(nullConsDef.ConstraintIdentifier.QuoteType),
                                FirstTokenIndex = nullConsDef.ConstraintIdentifier.FirstTokenIndex,
                                LastTokenIndex = nullConsDef.ConstraintIdentifier.LastTokenIndex
                            },
                            Nullable = nullConsDef.Nullable,
                        };
                        break;
                    }
                case "UniqueConstraintDefinition":
                    {
                        var uniqConsDef = consDef as UniqueConstraintDefinition;
                        var wUniqConsDef = new WUniqueConstraintDefinition
                        {
                            ConstraintIdentifier = new Identifier()
                            {
                                Value = uniqConsDef.ConstraintIdentifier.Value,
                                QuoteType = ParseQuoteType(uniqConsDef.ConstraintIdentifier.QuoteType),
                                FirstTokenIndex = uniqConsDef.ConstraintIdentifier.FirstTokenIndex,
                                LastTokenIndex = uniqConsDef.ConstraintIdentifier.LastTokenIndex
                            },
                            Clustered = uniqConsDef.Clustered,
                            IsPrimaryKey = uniqConsDef.IsPrimaryKey,
                        };
                        if (uniqConsDef.Columns != null)
                        {
                            wUniqConsDef.Columns = new List<Tuple<WColumnReferenceExpression, SortOrder>>();
                            foreach (var col in uniqConsDef.Columns)
                            {
                                wUniqConsDef.Columns.Add(new Tuple<WColumnReferenceExpression, SortOrder>(
                                    new WColumnReferenceExpression
                                    {
                                        ColumnType = ParseColumnType(col.Column.ColumnType),
                                        MultiPartIdentifier = ParseMultiPartIdentifier(col.Column.MultiPartIdentifier),
                                        FirstTokenIndex = col.Column.FirstTokenIndex,
                                        LastTokenIndex = col.Column.LastTokenIndex,
                                    },
                                    ParseSortOrder(col.SortOrder)));
                            }
                        }
                        wConsDef = wUniqConsDef;
                        break;
                    }
            }
            return wConsDef;
        }

        private WIndexDefinition ParseIndexDefinition(IndexDefinition idxDef)
        {
            if (idxDef == null)
                return null;
            var wIdxDef = new WIndexDefinition
            {
                FirstTokenIndex = idxDef.FirstTokenIndex,
                LastTokenIndex = idxDef.LastTokenIndex,
                IndexType = idxDef.IndexType,
                Name = ParseIdentifier(idxDef.Name),
            };
            if (idxDef.Columns == null)
                return wIdxDef;

            wIdxDef.Columns = new List<Tuple<WColumnReferenceExpression, SortOrder>>();
            foreach (var col in idxDef.Columns)
            {
                wIdxDef.Columns.Add(new Tuple<WColumnReferenceExpression, SortOrder>(
                    new WColumnReferenceExpression
                    {
                        ColumnType = ParseColumnType(col.Column.ColumnType),
                        MultiPartIdentifier = ParseMultiPartIdentifier(col.Column.MultiPartIdentifier),
                        FirstTokenIndex = col.Column.FirstTokenIndex,
                        LastTokenIndex = col.Column.LastTokenIndex,
                    },
                    ParseSortOrder(col.SortOrder)));
            }
            return wIdxDef;
        }

        private WDataTypeReference ParseDataType(DataTypeReference dataType)
        {
            if (dataType == null)
                return null;
            var pDataType = dataType as ParameterizedDataTypeReference;
            if (pDataType == null)
                throw new NotImplementedException();
            var wDataType = new WParameterizedDataTypeReference
            {
                Name = ParseSchemaObjectName(pDataType.Name),
                FirstTokenIndex = pDataType.FirstTokenIndex,
                LastTokenIndex = pDataType.LastTokenIndex
            };
            if (pDataType.Parameters == null)
                return wDataType;
            wDataType.Parameters = new List<Literal>(pDataType.Parameters.Count);
            foreach (var param in pDataType.Parameters)
                wDataType.Parameters.Add(param);
            return wDataType;
        }

        private WSqlStatement ParseInsertStatement(InsertSpecification insSpec)
        {
            var winsSpec = new WInsertSpecification
            {
                Target = ParseTableReference(insSpec.Target),
                InsertOption = insSpec.InsertOption,
                InsertSource = ParseInsertSource(insSpec.InsertSource),
                FirstTokenIndex = insSpec.FirstTokenIndex,
                LastTokenIndex = insSpec.LastTokenIndex
            };

            if (insSpec.TopRowFilter != null)
            {
                winsSpec.TopRowFilter = new WTopRowFilter
                {
                    Expression = ParseScalarExpression(insSpec.TopRowFilter.Expression),
                    WithTies = insSpec.TopRowFilter.WithTies,
                    Percent = insSpec.TopRowFilter.Percent,
                    FirstTokenIndex = insSpec.TopRowFilter.FirstTokenIndex,
                    LastTokenIndex = insSpec.TopRowFilter.LastTokenIndex
                };
            }

            //Columns
            winsSpec.Columns = new List<WColumnReferenceExpression>(insSpec.Columns.Count);
            foreach (var wexpr in insSpec.Columns.Select(column => new WColumnReferenceExpression
            {
                MultiPartIdentifier = ParseMultiPartIdentifier(column.MultiPartIdentifier),
                ColumnType = ParseColumnType(column.ColumnType),
                FirstTokenIndex = column.FirstTokenIndex,
                LastTokenIndex = column.LastTokenIndex
            }))
            {
                winsSpec.Columns.Add(wexpr);
            }

            return winsSpec;
        }

        private WInsertSource ParseInsertSource(InsertSource insSource)
        {
            if (insSource == null)
                return null;

            WInsertSource winsSouce = null;
            switch (insSource.GetType().Name)
            {
                case "SelectInsertSource":
                    {
                        var selInsSource = insSource as SelectInsertSource;
                        var wselInsSource = new WSelectInsertSource
                        {
                            Select = ParseSelectQueryStatement(selInsSource.Select),
                            FirstTokenIndex = selInsSource.FirstTokenIndex,
                            LastTokenIndex = selInsSource.LastTokenIndex

                        };
                        winsSouce = wselInsSource;
                        break;
                    }
                case "ValuesInsertSource":
                    {
                        var valInsSource = insSource as ValuesInsertSource;
                        var wvalInsSource = new WValuesInsertSource
                        {
                            IsDefaultValues = valInsSource.IsDefaultValues,
                            RowValues = new List<WRowValue>(valInsSource.RowValues.Count),
                            FirstTokenIndex = valInsSource.FirstTokenIndex,
                            LastTokenIndex = valInsSource.LastTokenIndex
                        };

                        foreach (var rowValue in valInsSource.RowValues)
                        {
                            wvalInsSource.RowValues.Add(ParseRowValue(rowValue));
                        }

                        winsSouce = wvalInsSource;
                        break;
                    }
            }

            return winsSouce;
        }

        private WRowValue ParseRowValue(RowValue rowValue)
        {
            var wrowValue = new WRowValue
            {
                ColumnValues = new List<WScalarExpression>(rowValue.ColumnValues.Count),
                FirstTokenIndex = rowValue.FirstTokenIndex,
                LastTokenIndex = rowValue.LastTokenIndex
            };

            foreach (var expr in rowValue.ColumnValues)
            {
                wrowValue.ColumnValues.Add(ParseScalarExpression(expr));
            }

            return wrowValue;
        }

        private WSqlStatement ParseDeleteStatement(DeleteSpecification delSpec)
        {
            if (delSpec == null)
                return null;

            var wdelSpec = new WDeleteSpecification
            {
                Target = ParseTableReference(delSpec.Target),
                FirstTokenIndex = delSpec.FirstTokenIndex,
                LastTokenIndex = delSpec.LastTokenIndex
            };

            //From Clause
            if (delSpec.FromClause != null && delSpec.FromClause.TableReferences != null)
            {
                wdelSpec.FromClause = new WFromClause
                {
                    FirstTokenIndex = delSpec.FromClause.FirstTokenIndex,
                    LastTokenIndex = delSpec.FromClause.LastTokenIndex,
                    TableReferences = new List<WTableReference>(delSpec.FromClause.TableReferences.Count)
                };
                foreach (var pref in delSpec.FromClause.TableReferences.Select(ParseTableReference).Where(pref => pref != null))
                {
                    wdelSpec.FromClause.TableReferences.Add(pref);
                }
            }

            //where clause
            if (delSpec.WhereClause != null && delSpec.WhereClause.SearchCondition != null)
            {
                wdelSpec.WhereClause = new WWhereClause
                {
                    FirstTokenIndex = delSpec.WhereClause.FirstTokenIndex,
                    LastTokenIndex = delSpec.WhereClause.LastTokenIndex,
                    SearchCondition = ParseBooleanExpression(delSpec.WhereClause.SearchCondition)
                };
            }

            //top row filter
            if (delSpec.TopRowFilter != null)
            {
                wdelSpec.TopRowFilter = new WTopRowFilter
                {
                    Expression = ParseScalarExpression(delSpec.TopRowFilter.Expression),
                    Percent = delSpec.TopRowFilter.Percent,
                    WithTies = delSpec.TopRowFilter.WithTies,
                    FirstTokenIndex = delSpec.TopRowFilter.FirstTokenIndex,
                    LastTokenIndex = delSpec.TopRowFilter.LastTokenIndex
                };
            }

            return wdelSpec;
        }

        private WSqlStatement ParseUpdateStatement(UpdateSpecification upSpec)
        {
            if (upSpec == null)
                return null;
            var wupSpec = new WUpdateSpecification
            {
                Target = ParseTableReference(upSpec.Target),
                FirstTokenIndex = upSpec.FirstTokenIndex,
                LastTokenIndex = upSpec.LastTokenIndex
            };

            //TopRowFilter
            if (upSpec.TopRowFilter != null)
            {
                wupSpec.TopRowFilter = new WTopRowFilter
                {
                    Percent = upSpec.TopRowFilter.Percent,
                    WithTies = upSpec.TopRowFilter.WithTies,
                    Expression = ParseScalarExpression(upSpec.TopRowFilter.Expression),
                    FirstTokenIndex = upSpec.TopRowFilter.FirstTokenIndex,
                    LastTokenIndex = upSpec.TopRowFilter.LastTokenIndex
                };
            }

            //From Clause
            if (upSpec.FromClause != null && upSpec.FromClause.TableReferences != null)
            {
                wupSpec.FromClause = new WFromClause
                {
                    FirstTokenIndex = upSpec.FromClause.FirstTokenIndex,
                    LastTokenIndex = upSpec.FromClause.LastTokenIndex,
                    TableReferences = new List<WTableReference>(upSpec.FromClause.TableReferences.Count)
                };
                foreach (var pref in upSpec.FromClause.TableReferences.Select(ParseTableReference).Where(pref => pref != null))
                {
                    wupSpec.FromClause.TableReferences.Add(pref);
                }
            }

            //Where Clause
            if (upSpec.WhereClause != null && upSpec.WhereClause.SearchCondition != null)
            {
                wupSpec.WhereClause = new WWhereClause
                {
                    FirstTokenIndex = upSpec.WhereClause.FirstTokenIndex,
                    LastTokenIndex = upSpec.WhereClause.LastTokenIndex,
                    SearchCondition = ParseBooleanExpression(upSpec.WhereClause.SearchCondition)
                };
            }

            //Set Clauses
            IList<WSetClause> wsetClauses = new List<WSetClause>(upSpec.SetClauses.Count);
            foreach (var setClause in upSpec.SetClauses)
            {
                WSetClause wsetClause;
                switch (setClause.GetType().Name)
                {
                    case "AssignmentSetClause":
                        {
                            var asSetClause = setClause as AssignmentSetClause;
                            wsetClause = ParseAssignmentSetClause(asSetClause);
                            break;
                        }
                    case "FunctionCallSetClause":
                    {
                        var fcSetClause = setClause as FunctionCallSetClause;
                        var mtFunction = fcSetClause.MutatorFunction;
                        wsetClause = new WFunctionCallSetClause
                        {
                            MutatorFuction = ParseScalarExpression(mtFunction) as WFunctionCall
                        };
                        break;
                    }
                    default:
                        continue;
                }
                wsetClauses.Add(wsetClause);
            }
            wupSpec.SetClauses = wsetClauses;

            return wupSpec;
        }

        private WSetClause ParseAssignmentSetClause(AssignmentSetClause asSetClause)
        {
            var wasSetClause = new WAssignmentSetClause
            {
                AssignmentKind = ParseAssignmentKind(asSetClause.AssignmentKind),
                FirstTokenIndex = asSetClause.FirstTokenIndex,
                LastTokenIndex = asSetClause.LastTokenIndex
            };

            if (asSetClause.Column != null)
            {
                var wexpr = new WColumnReferenceExpression
                {
                    MultiPartIdentifier = ParseMultiPartIdentifier(asSetClause.Column.MultiPartIdentifier),
                    ColumnType = ParseColumnType(asSetClause.Column.ColumnType),
                    FirstTokenIndex = asSetClause.Column.FirstTokenIndex,
                    LastTokenIndex = asSetClause.Column.LastTokenIndex
                };
                wasSetClause.Column = wexpr;
            }

            if (asSetClause.NewValue != null)
                wasSetClause.NewValue = ParseScalarExpression(asSetClause.NewValue);
            if (asSetClause.Variable != null)
                wasSetClause.Variable = asSetClause.Variable.Name;

            return wasSetClause;
        }

        private WSelectQueryExpression ParseSelectQueryStatement(QueryExpression queryExpr)
        {

            if (queryExpr == null)
            {
                return null;
            }

            switch (queryExpr.GetType().Name)
            {
                case "BinaryQueryExpression":
                    {
                        var bqe = queryExpr as BinaryQueryExpression;
                        var pQueryExpr = new WBinaryQueryExpression
                        {
                            All = bqe.All,
                            FirstQueryExpr = ParseSelectQueryStatement(bqe.FirstQueryExpression),
                            SecondQueryExpr = ParseSelectQueryStatement(bqe.SecondQueryExpression),
                            FirstTokenIndex = bqe.FirstTokenIndex,
                            LastTokenIndex = bqe.LastTokenIndex
                        };

                        //pQueryExpr.OrderByExpr = parseOrderbyExpr(bqe.OrderByClause);

                        return pQueryExpr;
                    }
                case "QueryParenthesisExpression":
                    {
                        var qpe = queryExpr as QueryParenthesisExpression;
                        var pQueryExpr = new WQueryParenthesisExpression
                        {
                            QueryExpr = ParseSelectQueryStatement(qpe.QueryExpression),
                            FirstTokenIndex = qpe.FirstTokenIndex,
                            LastTokenIndex = qpe.LastTokenIndex
                        };

                        //pQueryExpr.OrderByExpr = parseOrderbyExpr(qpe.OrderByClause);

                        return pQueryExpr;
                    }
                case "QuerySpecification":
                    {
                        var qs = queryExpr as QuerySpecification;
                        var pQueryExpr = new WSelectQueryBlock
                        {
                            FirstTokenIndex = qs.FirstTokenIndex,
                            LastTokenIndex = qs.LastTokenIndex,
                            SelectElements = new List<WSelectElement>(qs.SelectElements.Count),
                            FromClause = new WFromClause(),
                            WhereClause = new WWhereClause()
                        };

                        //
                        // SELECT clause
                        // 
                        foreach (var wsel in qs.SelectElements.Select(ParseSelectElement).Where(wsel => wsel != null))
                        {
                            pQueryExpr.SelectElements.Add(wsel);
                        }

                        //
                        // Top row filter
                        // 
                        if (qs.TopRowFilter != null)
                        {
                            pQueryExpr.TopRowFilter = new WTopRowFilter
                            {
                                Percent = qs.TopRowFilter.Percent,
                                WithTies = qs.TopRowFilter.WithTies,
                                Expression = ParseScalarExpression(qs.TopRowFilter.Expression),
                                FirstTokenIndex = qs.TopRowFilter.FirstTokenIndex,
                                LastTokenIndex = qs.TopRowFilter.LastTokenIndex
                            };
                        }

                        pQueryExpr.UniqueRowFilter = ParseUniqueRowFilter(qs.UniqueRowFilter);

                        //
                        // FROM clause
                        //
                        if (qs.FromClause != null && qs.FromClause.TableReferences != null)
                        {
                            pQueryExpr.FromClause.FirstTokenIndex = qs.FromClause.FirstTokenIndex;
                            pQueryExpr.FromClause.LastTokenIndex = qs.FromClause.LastTokenIndex;
                            pQueryExpr.FromClause.TableReferences = new List<WTableReference>(qs.FromClause.TableReferences.Count);
                            foreach (var pref in qs.FromClause.TableReferences.Select(ParseTableReference).Where(pref => pref != null))
                            {
                                pQueryExpr.FromClause.TableReferences.Add(pref);
                            }
                        }

                        //
                        // WHERE clause
                        //

                        if (qs.WhereClause != null && qs.WhereClause.SearchCondition != null)
                        {
                            pQueryExpr.WhereClause.FirstTokenIndex = qs.WhereClause.FirstTokenIndex;
                            pQueryExpr.WhereClause.LastTokenIndex = qs.WhereClause.LastTokenIndex;
                            pQueryExpr.WhereClause.SearchCondition = ParseBooleanExpression(qs.WhereClause.SearchCondition);
                        }

                        // GROUP-BY clause
                        if (qs.GroupByClause != null)
                        {
                            pQueryExpr.GroupByClause = ParseGroupbyClause(qs.GroupByClause);
                        }

                        // Having clause
                        if (qs.HavingClause != null)
                        {
                            pQueryExpr.HavingClause = new WHavingClause
                            {
                                SearchCondition = ParseBooleanExpression(qs.HavingClause.SearchCondition),
                                FirstTokenIndex = qs.HavingClause.FirstTokenIndex,
                                LastTokenIndex = qs.HavingClause.LastTokenIndex
                            };
                        }

                        //
                        // ORDER-BY clause
                        // 
                        if (qs.OrderByClause != null)
                        {
                            pQueryExpr.OrderByClause = ParseOrderbyClause(qs.OrderByClause);
                        }

                        return pQueryExpr;
                    }
                default:
                    return null;
            }
        }

        private WSelectElement ParseSelectElement(SelectElement sel)
        {
            if (sel == null)
            {
                return null;
            }

            switch (sel.GetType().Name)
            {
                case "SelectScalarExpression":
                    {
                        var sse = sel as SelectScalarExpression;
                        var pScalarExpr = new WSelectScalarExpression
                        {
                            SelectExpr = ParseScalarExpression(sse.Expression),
                            FirstTokenIndex = sse.FirstTokenIndex,
                            LastTokenIndex = sse.LastTokenIndex
                        };
                        if (sse.ColumnName != null)
                        {
                            pScalarExpr.ColumnName = sse.ColumnName.Value;
                        }

                        return pScalarExpr;
                    }
                case "SelectStarExpression":
                    {
                        var sse = sel as SelectStarExpression;
                        return new WSelectStarExpression()
                        {
                            FirstTokenIndex = sse.FirstTokenIndex,
                            LastTokenIndex = sse.LastTokenIndex,
                            Qulifier = ParseMultiPartIdentifier(sse.Qualifier)
                        };
                    }
                case "SelectSetVariable":
                    {
                        var ssv = sel as SelectSetVariable;
                        return new WSelectSetVariable
                        {
                            VariableName = ssv.Variable.Name,
                            Expression = ParseScalarExpression(ssv.Expression),
                            AssignmentType = ParseAssignmentKind(ssv.AssignmentKind),
                            FirstTokenIndex = ssv.FirstTokenIndex,
                            LastTokenIndex = ssv.LastTokenIndex
                        };
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        private WScalarExpression ParseScalarExpression(ScalarExpression scalarExpr)
        {
            if (scalarExpr == null)
            {
                return null;
            }

            switch (scalarExpr.GetType().Name)
            {
                case "BinaryExpression":
                    {
                        var bexpr = scalarExpr as BinaryExpression;
                        var wexpr = new WBinaryExpression
                        {
                            ExpressionType = ParseBinaryExpressionType(bexpr.BinaryExpressionType),
                            FirstExpr = ParseScalarExpression(bexpr.FirstExpression),
                            SecondExpr = ParseScalarExpression(bexpr.SecondExpression),
                            FirstTokenIndex = bexpr.FirstTokenIndex,
                            LastTokenIndex = bexpr.LastTokenIndex,
                        };

                        return wexpr;
                    }
                case "UnaryExpression":
                    {
                        var uexpr = scalarExpr as UnaryExpression;
                        var wuexpr = new WUnaryExpression
                        {
                            Expression = ParseScalarExpression(uexpr.Expression),
                            ExpressionType = ParseUnaryExpressionType(uexpr.UnaryExpressionType),
                            FirstTokenIndex = uexpr.FirstTokenIndex,
                            LastTokenIndex = uexpr.LastTokenIndex
                        };

                        return wuexpr;
                    }
                case "ColumnReferenceExpression":
                    {
                        var cre = scalarExpr as ColumnReferenceExpression;
                        var wexpr = new WColumnReferenceExpression
                        {
                            MultiPartIdentifier = ParseMultiPartIdentifier(cre.MultiPartIdentifier),
                            ColumnType = ParseColumnType(cre.ColumnType),
                            FirstTokenIndex = cre.FirstTokenIndex,
                            LastTokenIndex = cre.LastTokenIndex
                        };

                        return wexpr;
                    }
                case "ScalarSubquery":
                    {
                        var oquery = scalarExpr as ScalarSubquery;
                        var wexpr = new WScalarSubquery
                        {
                            SubQueryExpr = ParseSelectQueryStatement(oquery.QueryExpression),
                            FirstTokenIndex = oquery.FirstTokenIndex,
                            LastTokenIndex = oquery.LastTokenIndex
                        };

                        return wexpr;
                    }
                case "ParenthesisExpression":
                    {
                        var parenExpr = scalarExpr as ParenthesisExpression;
                        var wexpr = new WParenthesisExpression
                        {
                            Expression = ParseScalarExpression(parenExpr.Expression),
                            FirstTokenIndex = parenExpr.FirstTokenIndex,
                            LastTokenIndex = parenExpr.LastTokenIndex,
                        };

                        return wexpr;
                    }
                case "FunctionCall":
                    {
                        var fc = scalarExpr as FunctionCall;
                        var wexpr = new WFunctionCall
                        {
                            CallTarget = ParseCallTarget(fc.CallTarget),
                            FunctionName = ParseIdentifier(fc.FunctionName),
                            UniqueRowFilter = ParseUniqueRowFilter(fc.UniqueRowFilter),
                            FirstTokenIndex = fc.FirstTokenIndex,
                            LastTokenIndex = fc.LastTokenIndex,
                        };

                        if (fc.Parameters == null) return wexpr;
                        wexpr.Parameters = new List<WScalarExpression>(fc.Parameters.Count);
                        foreach (var pe in fc.Parameters.Select(ParseScalarExpression).Where(pe => pe != null))
                        {
                            wexpr.Parameters.Add(pe);
                        }

                        return wexpr;
                    }
                case "SearchedCaseExpression":
                    {
                        var caseExpr = scalarExpr as SearchedCaseExpression;
                        var wexpr = new WSearchedCaseExpression
                        {
                            FirstTokenIndex = caseExpr.FirstTokenIndex,
                            LastTokenIndex = caseExpr.LastTokenIndex,
                            WhenClauses = new List<WSearchedWhenClause>(caseExpr.WhenClauses.Count)
                        };

                        foreach (var pwhen in caseExpr.WhenClauses.Select(swhen => new WSearchedWhenClause
                        {
                            WhenExpression = ParseBooleanExpression(swhen.WhenExpression),
                            ThenExpression = ParseScalarExpression(swhen.ThenExpression),
                            FirstTokenIndex = swhen.FirstTokenIndex,
                            LastTokenIndex = swhen.LastTokenIndex,
                        }))
                        {
                            wexpr.WhenClauses.Add(pwhen);
                        }

                        wexpr.ElseExpr = ParseScalarExpression(caseExpr.ElseExpression);

                        return wexpr;
                    }
                case "CastCall":
                    {
                        var castExpr = scalarExpr as CastCall;
                        var wexpr = new WCastCall
                        {
                            DataType = ParseDataType(castExpr.DataType),
                            Parameter = ParseScalarExpression(castExpr.Parameter),
                            FirstTokenIndex = castExpr.FirstTokenIndex,
                            LastTokenIndex = castExpr.LastTokenIndex,
                        };

                        return wexpr;
                    }
                default:
                    {
                        if (!(scalarExpr is ValueExpression)) return null;
                        var wexpr = new WValueExpression
                        {
                            FirstTokenIndex = scalarExpr.FirstTokenIndex,
                            LastTokenIndex = scalarExpr.LastTokenIndex,
                        };

                        var expr = scalarExpr as Literal;
                        if (expr != null)
                        {
                            wexpr.Value = expr.Value;

                            if (expr.LiteralType == LiteralType.String)
                            {
                                wexpr.SingleQuoted = true;
                            }
                        }
                        else
                        {
                            var reference = scalarExpr as VariableReference;
                            wexpr.Value = reference != null ? reference.Name : ((GlobalVariableExpression)scalarExpr).Name;
                        }

                        return wexpr;
                    }
            }
        }

        private WTableReference ParseTableReference(TableReference tabRef)
        {
            if (tabRef == null)
            {
                return null;
            }
            var tabRefWithAlias = tabRef as TableReferenceWithAlias;
            if (tabRefWithAlias!=null && tabRefWithAlias.Alias!=null &&
                 GraphViewKeywords._keywords.Contains(tabRefWithAlias.Alias.Value))
            {
                var token = _tokens[tabRefWithAlias.Alias.FirstTokenIndex];
                throw new SyntaxErrorException(token.Line, tabRefWithAlias.Alias.Value,
                    "System restricted Name cannot be used");
            }
            switch (tabRef.GetType().Name)
            {
                case "NamedTableReference":
                    {
                        var oref = tabRef as NamedTableReference;
                        if (oref.SchemaObject.BaseIdentifier.QuoteType == Microsoft.SqlServer.TransactSql.ScriptDom.QuoteType.NotQuoted &&
                            (oref.SchemaObject.BaseIdentifier.Value[0] == '@' ||
                             oref.SchemaObject.BaseIdentifier.Value[0] == '#'))
                        {
                            var pref = new WSpecialNamedTableReference
                            {
                                Alias = ParseIdentifier(oref.Alias),
                                TableHints = new List<WTableHint>(),
                                FirstTokenIndex = oref.FirstTokenIndex,
                                LastTokenIndex = oref.LastTokenIndex,
                                TableObjectName = ParseSchemaObjectName(oref.SchemaObject),
                            };

                            if (oref.TableHints != null)
                            {
                                foreach (var hint in oref.TableHints)
                                    pref.TableHints.Add(ParseTableHint(hint));
                            }

                            return pref;
                        }
                        else
                        {
                            var pref = new WNamedTableReference
                            {
                                Alias = ParseIdentifier(oref.Alias),
                                TableHints = new List<WTableHint>(),
                                FirstTokenIndex = oref.FirstTokenIndex,
                                LastTokenIndex = oref.LastTokenIndex,
                                TableObjectName = ParseSchemaObjectName(oref.SchemaObject),
                            };

                            if (oref.TableHints != null)
                            {
                                foreach (var hint in oref.TableHints)
                                    pref.TableHints.Add(ParseTableHint(hint));
                            }

                            return pref;
                        }
                    }
                case "QueryDerivedTable":
                    {
                        var oref = tabRef as QueryDerivedTable;
                        var pref = new WQueryDerivedTable
                        {
                            QueryExpr = ParseSelectQueryStatement(oref.QueryExpression),
                            Alias = ParseIdentifier(oref.Alias),
                            Columns = new List<Identifier>(oref.Columns.Select(e => ParseIdentifier(e))),
                            FirstTokenIndex = oref.FirstTokenIndex,
                            LastTokenIndex = oref.LastTokenIndex,
                        };

                        return pref;
                    }
                case "SchemaObjectFunctionTableReference":
                    {
                        var oref = tabRef as SchemaObjectFunctionTableReference;
                        var pref = new WSchemaObjectFunctionTableReference
                        {
                            Alias = ParseIdentifier(oref.Alias),
                            Columns = new List<Identifier>(oref.Columns.Select(e => ParseIdentifier(e))),
                            SchemaObject = ParseSchemaObjectName(oref.SchemaObject),
                            FirstTokenIndex = oref.FirstTokenIndex,
                            LastTokenIndex = oref.LastTokenIndex
                        };
                        if (oref.Parameters == null)
                            return pref;
                        pref.Parameters = new List<WScalarExpression>();
                        foreach (var param in oref.Parameters)
                            pref.Parameters.Add(ParseScalarExpression(param));
                        return pref;
                    }
                case "QualifiedJoin":
                    {
                        var oref = tabRef as QualifiedJoin;
                        var pref = new WQualifiedJoin
                        {
                            FirstTableRef = ParseTableReference(oref.FirstTableReference),
                            SecondTableRef = ParseTableReference(oref.SecondTableReference),
                            QualifiedJoinType = ParseQualifiedJoinType(oref.QualifiedJoinType),
                            JoinHint = ParseJoinHint(oref.JoinHint),
                            JoinCondition = ParseBooleanExpression(oref.SearchCondition),
                            FirstTokenIndex = oref.FirstTokenIndex,
                            LastTokenIndex = oref.LastTokenIndex,
                        };

                        return pref;
                    }
                case "UnqualifiedJoin":
                    {
                        var oref = tabRef as UnqualifiedJoin;
                        var pref = new WUnqualifiedJoin
                        {
                            FirstTableRef = ParseTableReference(oref.FirstTableReference),
                            SecondTableRef = ParseTableReference(oref.SecondTableReference),
                            UnqualifiedJoinType = ParseUnqualifiedJoinType(oref.UnqualifiedJoinType),
                            FirstTokenIndex = oref.FirstTokenIndex,
                            LastTokenIndex = oref.LastTokenIndex,
                        };
                        return pref;
                    }
                case "JoinParenthesisTableReference":
                    {
                        var ptab = tabRef as JoinParenthesisTableReference;

                        var wptab = new WParenthesisTableReference
                        {
                            Table = ParseTableReference(ptab.Join),
                            FirstTokenIndex = ptab.FirstTokenIndex,
                            LastTokenIndex = ptab.LastTokenIndex,
                        };

                        return wptab;
                    }
                case "VariableTableReference":
                    {
                        var ptab = tabRef as VariableTableReference;

                        var wptab = new WVariableTableReference
                        {
                            FirstTokenIndex = ptab.FirstTokenIndex,
                            LastTokenIndex = ptab.LastTokenIndex,
                            Alias = ParseIdentifier(ptab.Alias),
                            Variable = ParseVariableReference(ptab.Variable)
                        };

                        return wptab;
                    }
                default:
                    return null;
            }
        }

        private WOrderByClause ParseOrderbyClause(OrderByClause orderbyExpr)
        {
            var wobc = new WOrderByClause
            {
                FirstTokenIndex = orderbyExpr.FirstTokenIndex,
                LastTokenIndex = orderbyExpr.LastTokenIndex,
                OrderByElements = new List<WExpressionWithSortOrder>(orderbyExpr.OrderByElements.Count)
            };

            foreach (var pexp in from e in orderbyExpr.OrderByElements
                                 let pscalar = ParseScalarExpression(e.Expression)
                                 where pscalar != null
                                 select new WExpressionWithSortOrder
                                 {
                                     ScalarExpr = pscalar,
                                     SortOrder = ParseSortOrder(e.SortOrder),
                                     FirstTokenIndex = e.FirstTokenIndex,
                                     LastTokenIndex = e.LastTokenIndex
                                 })
            {
                wobc.OrderByElements.Add(pexp);
            }

            return wobc;
        }

        private WGroupByClause ParseGroupbyClause(GroupByClause groupbyExpr)
        {
            if (groupbyExpr == null)
            {
                return null;
            }

            var wgc = new WGroupByClause
            {
                FirstTokenIndex = groupbyExpr.FirstTokenIndex,
                LastTokenIndex = groupbyExpr.LastTokenIndex,
                GroupingSpecifications = new List<WGroupingSpecification>(groupbyExpr.GroupingSpecifications.Count)
            };

            foreach (var gs in groupbyExpr.GroupingSpecifications)
            {
                //if (!(gs is ExpressionGroupingSpecification))
                //    continue;
                var egs = gs as ExpressionGroupingSpecification;
                if (egs == null) continue;
                var pspec = new WExpressionGroupingSpec
                {
                    Expression = ParseScalarExpression(egs.Expression),
                    FirstTokenIndex = egs.FirstTokenIndex,
                    LastTokenIndex = egs.LastTokenIndex,
                };

                wgc.GroupingSpecifications.Add(pspec);
            }

            return wgc;
        }

        private WBooleanExpression ParseBooleanExpression(BooleanExpression bexpr)
        {
            if (bexpr == null)
            {
                return null;
            }

            switch (bexpr.GetType().Name)
            {
                case "BooleanBinaryExpression":
                    {
                        var oexpr = bexpr as BooleanBinaryExpression;
                        var pexpr = new WBooleanBinaryExpression
                        {
                            FirstExpr = ParseBooleanExpression(oexpr.FirstExpression),
                            SecondExpr = ParseBooleanExpression(oexpr.SecondExpression),
                            BooleanExpressionType = ParseBooleanBinaryExpressionType(oexpr.BinaryExpressionType),
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                        };

                        return pexpr;
                    }
                case "BooleanComparisonExpression":
                    {
                        var oexpr = bexpr as BooleanComparisonExpression;
                        var pexpr = new WBooleanComparisonExpression
                        {
                            ComparisonType = ParseBooleanComparisonType(oexpr.ComparisonType),
                            FirstExpr = ParseScalarExpression(oexpr.FirstExpression),
                            SecondExpr = ParseScalarExpression(oexpr.SecondExpression),
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                        };

                        return pexpr;
                    }
                case "BooleanIsNullExpression":
                    {
                        var oexpr = bexpr as BooleanIsNullExpression;
                        var pexpr = new WBooleanIsNullExpression
                        {
                            IsNot = oexpr.IsNot,
                            Expression = ParseScalarExpression(oexpr.Expression),
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                        };

                        return pexpr;
                    }
                case "BooleanNotExpression":
                    {
                        var oexpr = bexpr as BooleanNotExpression;
                        var pexpr = new WBooleanNotExpression
                        {
                            Expression = ParseBooleanExpression(oexpr.Expression),
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                        };

                        return pexpr;
                    }
                case "BooleanParenthesisExpression":
                    {
                        var oexpr = bexpr as BooleanParenthesisExpression;
                        var pexpr = new WBooleanParenthesisExpression
                        {
                            Expression = ParseBooleanExpression(oexpr.Expression),
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                        };

                        return pexpr;
                    }
                case "BooleanTernaryExpression":
                    {
                        var oexpr = bexpr as BooleanTernaryExpression;
                        var pexpr = new WBetweenExpression
                        {
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                        };

                        switch (oexpr.TernaryExpressionType)
                        {
                            case BooleanTernaryExpressionType.Between:
                                pexpr.NotDefined = false;
                                break;
                            case BooleanTernaryExpressionType.NotBetween:
                                pexpr.NotDefined = true;
                                break;
                            default:
                                throw new GraphViewException("Undefined tenary expression type");
                        }

                        pexpr.FirstExpr = ParseScalarExpression(oexpr.FirstExpression);
                        pexpr.SecondExpr = ParseScalarExpression(oexpr.SecondExpression);
                        pexpr.ThirdExpr = ParseScalarExpression(oexpr.ThirdExpression);

                        return pexpr;
                    }
                case "ExistsPredicate":
                    {
                        var oexpr = bexpr as ExistsPredicate;
                        var pexpr = new WExistsPredicate
                        {
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                            Subquery =
                                new WScalarSubquery
                                {
                                    SubQueryExpr = ParseSelectQueryStatement(oexpr.Subquery.QueryExpression),
                                    FirstTokenIndex = oexpr.Subquery.FirstTokenIndex,
                                    LastTokenIndex = oexpr.Subquery.LastTokenIndex,
                                }
                        };

                        return pexpr;
                    }
                case "InPredicate":
                    {
                        var oexpr = bexpr as InPredicate;
                        var pexpr = new WInPredicate
                        {
                            Expression = ParseScalarExpression(oexpr.Expression),
                            NotDefined = oexpr.NotDefined,
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                        };

                        if (oexpr.Subquery != null)
                        {
                            pexpr.Subquery = new WScalarSubquery
                            {
                                SubQueryExpr = ParseSelectQueryStatement(oexpr.Subquery.QueryExpression),
                                FirstTokenIndex = oexpr.Subquery.FirstTokenIndex,
                                LastTokenIndex = oexpr.Subquery.LastTokenIndex,

                            };
                        }
                        else
                        {
                            pexpr.Values = new List<WScalarExpression>(oexpr.Values.Count);
                            foreach (var wexp in oexpr.Values.Select(ParseScalarExpression).Where(wexp => wexp != null))
                            {
                                pexpr.Values.Add(wexp);
                            }
                        }

                        return pexpr;
                    }
                case "LikePredicate":
                    {
                        var oexpr = bexpr as LikePredicate;
                        var pexpr = new WLikePredicate
                        {
                            EscapeExpr = ParseScalarExpression(oexpr.EscapeExpression),
                            FirstExpr = ParseScalarExpression(oexpr.FirstExpression),
                            SecondExpr = ParseScalarExpression(oexpr.SecondExpression),
                            NotDefined = oexpr.NotDefined,
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                        };

                        return pexpr;
                    }
                case "SubqueryComparisonPredicate":
                    {
                        var oexpr = bexpr as SubqueryComparisonPredicate;
                        var pexpr = new WSubqueryComparisonPredicate
                        {
                            FirstTokenIndex = oexpr.FirstTokenIndex,
                            LastTokenIndex = oexpr.LastTokenIndex,
                            Subquery = new WScalarSubquery()
                            {
                                SubQueryExpr = ParseSelectQueryStatement(oexpr.Subquery.QueryExpression),
                                FirstTokenIndex = oexpr.Subquery.FirstTokenIndex,
                                LastTokenIndex = oexpr.Subquery.LastTokenIndex

                            },
                            ComparisonType = ParseBooleanComparisonType(oexpr.ComparisonType),
                            Expression = ParseScalarExpression(oexpr.Expression),
                            SubqueryComparisonType = ParseSubqueryComparisonPredicateType(oexpr.SubqueryComparisonPredicateType),
                        };

                        return pexpr;
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        private WTableHint ParseTableHint(TableHint hint)
        {
            return new WTableHint
            {
                FirstTokenIndex = hint.FirstTokenIndex,
                LastTokenIndex = hint.LastTokenIndex,
                HintKind = hint.HintKind
            };
        }

        private WMultiPartIdentifier ParseMultiPartIdentifier(MultiPartIdentifier name)
        {
            if (name == null)
                return null;
            var wMultiPartIdentifier = new WMultiPartIdentifier
            {
                FirstTokenIndex = name.FirstTokenIndex,
                LastTokenIndex = name.LastTokenIndex,
                Identifiers = new List<Identifier>(name.Identifiers.Select(e => ParseIdentifier(e)))
            };
            if (name.Identifiers != null)
            {
                wMultiPartIdentifier.Identifiers = new List<Identifier>();
                foreach (var identifier in name.Identifiers)
                {
                    if (GraphViewKeywords._keywords.Contains(identifier.Value))
                    {
                        var token = _tokens[identifier.FirstTokenIndex];
                        throw new SyntaxErrorException(token.Line, identifier.Value,
                            "System restricted Name cannot be used");
                    }
                    wMultiPartIdentifier.Identifiers.Add(ParseIdentifier(identifier));
                }
            }
            return wMultiPartIdentifier;
        }

        private WSchemaObjectName ParseSchemaObjectName(SchemaObjectName name)
        {
            if (name == null)
                return null;
            var wSchemaObjectName = new WSchemaObjectName
            {
                FirstTokenIndex = name.FirstTokenIndex,
                LastTokenIndex = name.LastTokenIndex,
            };
            if (name.Identifiers != null)
            {
                wSchemaObjectName.Identifiers = new List<Identifier>();
                foreach (var identifier in name.Identifiers)
                {
                    if (GraphViewKeywords._keywords.Contains(identifier.Value))
                    {
                        var token = _tokens[identifier.FirstTokenIndex];
                        throw new SyntaxErrorException(token.Line, identifier.Value,
                            "System restricted Name cannot be used");
                    }
                    wSchemaObjectName.Identifiers.Add(ParseIdentifier(identifier));
                }
            }
            return wSchemaObjectName;
        }

        private WCallTarget ParseCallTarget(CallTarget callTarget)
        {
            if (callTarget == null)
                return null;
            WCallTarget result;
            var tCallTarget = callTarget as MultiPartIdentifierCallTarget;
            if (tCallTarget != null)
            {
                result = new WMultiPartIdentifierCallTarget
                {
                    Identifiers = ParseMultiPartIdentifier(tCallTarget.MultiPartIdentifier)
                };
            }
            else
            {
                throw new NotImplementedException();
            }

            return result;
        }

        private WIdentifierOrValueExpression ParseIdentifierOrValueExpression(IdentifierOrValueExpression value)
        {
            if (value == null)
                return null;
            if (GraphViewKeywords._keywords.Contains(value.Identifier.Value))
            {
                var token = _tokens[value.FirstTokenIndex];
                throw new SyntaxErrorException(token.Line, value.Identifier.Value,
                    "System restricted Name cannot be used");
            }
            return new WIdentifierOrValueExpression
            {
                FirstTokenIndex = value.FirstTokenIndex,
                LastTokenIndex = value.LastTokenIndex,
                Identifier = ParseIdentifier(value.Identifier),
                ValueExpression = ParseScalarExpression(value.ValueExpression) as WValueExpression
            };
        }

        private WVariableReference ParseVariableReference(VariableReference value)
        {
            return  new WVariableReference()
            {
                Name = value.Name
            };
        }
    }

}
