using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinMaxOp: GremlinTranslationOperator
    {
    }

    internal class GremlinMaxGlobalOp : GremlinMaxOp
    {

        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }
            inputContext.PivotVariable.MaxGlobal(inputContext);
            return inputContext;
        }
    }

    internal class GremlinMaxLocalOp : GremlinMaxOp
    {
        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            inputContext.PivotVariable.MaxLocal(inputContext);
            return inputContext;
        }
    }
}
