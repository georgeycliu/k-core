using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace GraphView
{
    internal class GremlinSampleOp: GremlinTranslationOperator
    {
        public int AmountToSample { get; set; }

        public GremlinSampleOp(int amountToSample)
        {
            this.AmountToSample = amountToSample;
        }
    }

    internal class GremlinSampleGlobalOp : GremlinSampleOp
    {
        public GraphTraversal2 ProbabilityTraversal { get; set; }

        public GremlinSampleGlobalOp(int amountToSample): base(amountToSample)
        {
        }

        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            GremlinToSqlContext probabilityContext = null;
            if (this.ProbabilityTraversal != null)
            {
                this.ProbabilityTraversal.GetStartOp().InheritedVariableFromParent(inputContext);
                probabilityContext = this.ProbabilityTraversal.GetEndOp().GetContext();
            }

            inputContext.PivotVariable.SampleGlobal(inputContext, this.AmountToSample, probabilityContext);

            return inputContext;
        }

        public override void ModulateBy(GraphTraversal2 traversal)
        {
            this.ProbabilityTraversal = traversal;
        }
    }

    internal class GremlinSampleLocalOp : GremlinSampleOp
    {
        public GremlinSampleLocalOp(int amountToSample): base(amountToSample)
        {
        }

        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            inputContext.PivotVariable.SampleLocal(inputContext, this.AmountToSample);

            return inputContext;
        }
    }
}
