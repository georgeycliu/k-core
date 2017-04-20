using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinMinOp: GremlinTranslationOperator
    {
    }

    internal class GremlinMinGlobalOp : GremlinMinOp
    {
        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            inputContext.PivotVariable.MinGlobal(inputContext);

            return inputContext;
        }
    }

    internal class GremlinMinLocalOp : GremlinMinOp
    {
        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            inputContext.PivotVariable.MinLocal(inputContext);

            return inputContext;
        }
    }
}
