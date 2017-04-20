using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinOrOp : GremlinTranslationOperator
    {
        public List<GraphTraversal2> OrTraversals { get; set; } = new List<GraphTraversal2>();

        public GremlinOrOp(params GraphTraversal2[] orTraversals)
        {
            OrTraversals = new List<GraphTraversal2>(orTraversals);
        }

        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = InputOperator.GetContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            List<GremlinToSqlContext> orContexts = new List<GremlinToSqlContext>();
            foreach (var orTraversal in this.OrTraversals)
            {
                orTraversal.GetStartOp().InheritedVariableFromParent(inputContext);
                orContexts.Add(orTraversal.GetEndOp().GetContext());
            }

            inputContext.PivotVariable.Or(inputContext, orContexts);

            return inputContext;
        }
    }

    internal class GremlinOrInfixOp : GremlinOrOp
    {
        public GraphTraversal2 FirstTraversal => this.OrTraversals[0];
        public GraphTraversal2 SecondTraversal => this.OrTraversals[1];

        public GremlinOrInfixOp(params GraphTraversal2[] orTraversals) : base(orTraversals)
        {
        }
    }
}
