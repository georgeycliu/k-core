using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinRangeOp: GremlinTranslationOperator
    {
        public int Low { get; set; }
        public int High { get; set; }
        public bool IsReverse { get; set; }
    }

    internal class GremlinRangeGlobalOp : GremlinRangeOp
    {
        public GremlinRangeGlobalOp(int low, int high, bool isReverse = false)
        {
            this.Low = low;
            this.High = high;
            this.IsReverse = isReverse;
        }

        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            inputContext.PivotVariable.RangeGlobal(inputContext, this.Low, this.High, this.IsReverse);

            return inputContext;
        }
    }

    internal class GremlinRangeLocalOp : GremlinRangeOp
    {
        public GremlinRangeLocalOp(int low, int high, bool isReverse = false)
        {
            this.Low = low;
            this.High = high;
            this.IsReverse = isReverse;
        }

        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            inputContext.PivotVariable.RangeLocal(inputContext, this.Low, this.High, this.IsReverse);

            return inputContext;
        }
    }
}
