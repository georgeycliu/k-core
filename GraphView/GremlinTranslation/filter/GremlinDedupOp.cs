using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace GraphView
{
    internal class GremlinDedupOp: GremlinTranslationOperator
    {
        public List<string> DedupLabels { get; set; }
        public GraphTraversal2 ByTraversal { get; set; }

        public GremlinDedupOp(params string[] dedupLabels)
        {
            this.DedupLabels = new List<string>(dedupLabels);
            this.ByTraversal = GraphTraversal2.__();
        }
    }

    internal class GremlinDedupGlobalOp : GremlinDedupOp
    {
        public GremlinDedupGlobalOp(params string[] dedupLabels): base(dedupLabels)
        {
        }

        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            inputContext.PivotVariable.DedupGlobal(inputContext, this.DedupLabels, this.ByTraversal);

            return inputContext;
        }

        public override void ModulateBy(GraphTraversal2 traversal)
        {
            this.ByTraversal = traversal;
        }
    }

    internal class GremlinDedupLocalOp : GremlinDedupOp
    {
        public GremlinDedupLocalOp(params string[] dedupLabels): base(dedupLabels)
        {
        }

        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            this.ByTraversal.GetStartOp().InheritedVariableFromParent(inputContext);
            GremlinToSqlContext dedupContext = this.ByTraversal.GetEndOp().GetContext();
            inputContext.PivotVariable.DedupLocal(inputContext, dedupContext);

            return inputContext;
        }
    }
}
