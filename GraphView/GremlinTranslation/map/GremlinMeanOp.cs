using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinMeanOp: GremlinTranslationOperator
    {
    }

    internal class GremlinMeanGlobalOp : GremlinMeanOp
    {
        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }
            inputContext.PivotVariable.MeanGlobal(inputContext);
            return inputContext;
        }
    }

    internal class GremlinMeanLocalOp : GremlinMeanOp
    {
        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }
            inputContext.PivotVariable.MeanLocal(inputContext);

            return inputContext;
        }
    }
}
