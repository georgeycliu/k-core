﻿using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinAndOp : GremlinTranslationOperator
    {
        public List<GraphTraversal2> AndTraversals { get; set; } = new List<GraphTraversal2>();

        public GremlinAndOp(params GraphTraversal2[] andTraversals)
        {
            AndTraversals = new List<GraphTraversal2>(andTraversals);
        }

        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = InputOperator.GetContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            List<GremlinToSqlContext> andContexts = new List<GremlinToSqlContext>();
            foreach (var andTraversal in AndTraversals)
            {
                andTraversal.GetStartOp().InheritedVariableFromParent(inputContext);
                andContexts.Add(andTraversal.GetEndOp().GetContext());
            }
            inputContext.PivotVariable.And(inputContext, andContexts);

            return inputContext;
        }
    }

    internal class GremlinAndInfixOp : GremlinAndOp
    {
        public GraphTraversal2 FirstTraversal => this.AndTraversals[0];
        public GraphTraversal2 SecondTraversal => this.AndTraversals[1];

        public GremlinAndInfixOp(params GraphTraversal2[] andTraversals) : base(andTraversals)
        {
        }
    }
}
