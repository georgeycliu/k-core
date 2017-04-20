using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinRangeVariable : GremlinTableVariable
    {
        public int Low { get; set; }
        public int High { get; set; }
        public bool IsReverse { get; set; }
        public GremlinVariable InputVaribale { get; set; }

        public GremlinRangeVariable(GremlinVariable inputVariable, int low, int high, bool isReverse): base(GremlinVariableType.Table)
        {
            this.InputVaribale = inputVariable;
            this.Low = low;
            this.High = high;
            this.IsReverse = isReverse;
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this };
            variableList.AddRange(this.InputVaribale.FetchAllVars());
            return variableList;
        }
    }

    internal class GremlinRangeGlobalVariable : GremlinRangeVariable
    {
        public GremlinRangeGlobalVariable(GremlinVariable inputVariable, int low, int high, bool isReverse)
            : base(inputVariable, low, high, isReverse)
        {
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();
            parameters.Add(this.InputVaribale.GetDefaultProjection().ToScalarExpression());
            parameters.Add(SqlUtil.GetValueExpr(this.Low));
            parameters.Add(SqlUtil.GetValueExpr(this.High));
            parameters.Add(SqlUtil.GetValueExpr(-1)); // global flag for compilation
            parameters.Add(this.IsReverse ? SqlUtil.GetValueExpr(1) : SqlUtil.GetValueExpr(-1));

            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.Range, parameters, GetVariableName());
            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }

    internal class GremlinRangeLocalVariable : GremlinRangeVariable
    {
        public GremlinRangeLocalVariable(GremlinVariable inputVariable, int low, int high, bool isReverse)
            : base(inputVariable, low, high, isReverse)
        {
        }

        internal override void Populate(string property)
        {
            this.InputVaribale.Populate(property);
            base.Populate(property);
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();
            parameters.Add(this.InputVaribale.GetDefaultProjection().ToScalarExpression());
            parameters.Add(SqlUtil.GetValueExpr(this.Low));
            parameters.Add(SqlUtil.GetValueExpr(this.High));
            parameters.Add(SqlUtil.GetValueExpr(1)); // local flag for compilation
            parameters.Add(this.IsReverse ? SqlUtil.GetValueExpr(1) : SqlUtil.GetValueExpr(-1));

            foreach (var property in this.ProjectedProperties)
            {
                parameters.Add(SqlUtil.GetValueExpr(property));
            }

            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.Range, parameters, GetVariableName());
            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }
}